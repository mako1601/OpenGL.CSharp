using System.Numerics;
using System.Runtime.CompilerServices;
using Engine.Physics.Colliders;

namespace Engine.Physics.Utilities;

/// <summary>
/// Provides static helper methods for collision detection between various types of colliders.
/// </summary>
public static class CollisionHelper
{
    // static buffers to avoid memory allocations during collision checks
    private static readonly Vector3[] s_axesA = new Vector3[3];
    private static readonly Vector3[] s_axesB = new Vector3[3];
    private static readonly Vector3[] s_testAxes = new Vector3[15];

    // unit vectors to avoid creating new instances
    private static readonly Vector3 s_unitX = Vector3.UnitX;
    private static readonly Vector3 s_unitY = Vector3.UnitY;
    private static readonly Vector3 s_unitZ = Vector3.UnitZ;

    private const float EPSILON = 0.0001f;
    private const float HALF = 0.5f;

    /// <summary>
    /// Checks for collision between two oriented bounding boxes (OBB) using the Separating Axis Theorem (SAT).
    /// </summary>
    /// <param name="a">The first box collider.</param>
    /// <param name="b">The second box collider.</param>
    /// <param name="normal">The resulting collision normal (direction from a to b).</param>
    /// <param name="penetrationDepth">The penetration depth along the collision normal.</param>
    /// <returns>True if boxes are colliding, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BoxVsBox(BoxCollider a, BoxCollider b, out Vector3 normal, out float penetrationDepth)
    {
        normal = Vector3.Zero;
        penetrationDepth = float.MaxValue;

        // compute local axes directly into pre-allocated static buffers
        ComputeBoxAxes(a.Transform.Rotation, s_axesA);
        ComputeBoxAxes(b.Transform.Rotation, s_axesB);

        // pre-compute separation vector and box half-sizes
        var separation = b.Transform.Position - a.Transform.Position;
        var halfSizeA = a.Size * HALF;
        var halfSizeB = b.Size * HALF;

        // copy face normals (6 axes total) directly to static buffer
        s_testAxes[0] = s_axesA[0]; s_testAxes[1] = s_axesA[1]; s_testAxes[2] = s_axesA[2];
        s_testAxes[3] = s_axesB[0]; s_testAxes[4] = s_axesB[1]; s_testAxes[5] = s_axesB[2];

        // generate edge-edge cross product axes (9 axes) directly in static buffer
        int axisIndex = 6;
        for (int i = 0; i < 3; i++)
        {
            ref readonly var axisA = ref s_axesA[i];
            for (int j = 0; j < 3; j++)
            {
                ref readonly var axisB = ref s_axesB[j];
                Vector3 cross = Vector3.Cross(axisA, axisB);
                float lengthSq = cross.LengthSquared();

                // only use non-degenerate axes
                if (lengthSq > EPSILON)
                {
                    s_testAxes[axisIndex] = cross * (1.0f / MathF.Sqrt(lengthSq)); // fast normalization
                }
                else
                {
                    s_testAxes[axisIndex] = Vector3.Zero;
                }

                axisIndex++;
            }
        }

        // test all 15 axes for separation with early exit optimization
        for (int i = 0; i < 15; i++)
        {
            ref readonly var axis = ref s_testAxes[i];

            // skip degenerate axes
            if (axis.LengthSquared() < EPSILON) continue;

            // fast projection radius calculation using static buffers
            float projectionA = ComputeProjectionRadius(halfSizeA, s_axesA, axis);
            float projectionB = ComputeProjectionRadius(halfSizeB, s_axesB, axis);
            float distance = MathF.Abs(Vector3.Dot(separation, axis));

            float overlap = projectionA + projectionB - distance;

            // early exit - separating axis found, no collision
            if (overlap <= 0f) return false;

            // track minimum penetration for collision resolution
            if (overlap < penetrationDepth)
            {
                penetrationDepth = overlap;
                normal = Vector3.Dot(separation, axis) < 0 ? -axis : axis;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks for collision between a box and a sphere.
    /// </summary>
    /// <param name="box">The box collider.</param>
    /// <param name="sphere">The sphere collider.</param>
    /// <param name="normal">The resulting collision normal (direction from box to sphere).</param>
    /// <param name="penetrationDepth">The penetration depth along the collision normal.</param>
    /// <returns>True if box and sphere are colliding, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BoxVsSphere(BoxCollider box, SphereCollider sphere, out Vector3 normal, out float penetrationDepth)
    {
        normal = Vector3.Zero;
        penetrationDepth = 0f;

        Vector3 sphereCenter = sphere.Transform.Position;
        Vector3 boxCenter = box.Transform.Position;
        float sphereDiameter = sphere.Diameter;

        // transform sphere center to box's local space
        Vector3 relativePosition = sphereCenter - boxCenter;
        Vector3 localSphereCenter = Vector3.Transform(relativePosition, Quaternion.Conjugate(box.Transform.Rotation));

        Vector3 boxHalfSize = box.Size * HALF;

        // Find the closest point on the box to the sphere center
        var closestPoint = new Vector3(
            Math.Clamp(localSphereCenter.X, -boxHalfSize.X, boxHalfSize.X),
            Math.Clamp(localSphereCenter.Y, -boxHalfSize.Y, boxHalfSize.Y),
            Math.Clamp(localSphereCenter.Z, -boxHalfSize.Z, boxHalfSize.Z)
        );

        Vector3 localDirection = localSphereCenter - closestPoint;
        float distanceSq = localDirection.LengthSquared();

        // no collision if distance squared is greater than sphere diameter squared
        if (distanceSq >= sphereDiameter * sphereDiameter) return false;

        var distance = MathF.Sqrt(distanceSq);
        if (distance < EPSILON)
        {
            // sphere center is inside the box - find the closest face
            Vector3 localNormal = Vector3.Zero;
            float minDistance = float.MaxValue;

            // check distance to each face using pre-computed values
            float distanceX = boxHalfSize.X - MathF.Abs(localSphereCenter.X);
            float distanceY = boxHalfSize.Y - MathF.Abs(localSphereCenter.Y);
            float distanceZ = boxHalfSize.Z - MathF.Abs(localSphereCenter.Z);

            if (distanceX < minDistance)
            {
                minDistance = distanceX;
                localNormal = new Vector3(localSphereCenter.X > 0 ? 1f : -1f, 0f, 0f);
            }
            if (distanceY < minDistance)
            {
                minDistance = distanceY;
                localNormal = new Vector3(0f, localSphereCenter.Y > 0 ? 1f : -1f, 0f);
            }
            if (distanceZ < minDistance)
            {
                minDistance = distanceZ;
                localNormal = new Vector3(0f, 0f, localSphereCenter.Z > 0 ? 1f : -1f);
            }

            normal = Vector3.Transform(localNormal, box.Transform.Rotation);
            penetrationDepth = sphereDiameter + minDistance;
        }
        else
        {
            float invDistance = 1.0f / distance;
            Vector3 localNormal = localDirection * invDistance; // fast normalization
            normal = Vector3.Transform(localNormal, box.Transform.Rotation);
            penetrationDepth = sphereDiameter - distance;
        }

        return true;
    }

    /// <summary>
    /// Checks for collision between a box and a capsule.
    /// </summary>
    /// <param name="box">The box collider.</param>
    /// <param name="capsule">The capsule collider.</param>
    /// <param name="normal">The resulting collision normal (direction from box to capsule).</param>
    /// <param name="penetrationDepth">The penetration depth along the collision normal.</param>
    /// <returns>True if box and capsule are colliding, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BoxVsCapsule(
        BoxCollider box,
        CapsuleCollider capsule,
        out Vector3 normal,
        out float penetrationDepth
    )
    {
        normal = Vector3.Zero;
        penetrationDepth = 0f;

        // get the line segment representing the capsule's central axis
        Vector3 capsuleTop = capsule.TopCenter;
        Vector3 capsuleBottom = capsule.BottomCenter;
        float capsuleRadius = capsule.Radius;

        // transform capsule line segment to box's local space
        Vector3 boxCenter = box.Transform.Position;
        Quaternion invBoxRotation = Quaternion.Conjugate(box.Transform.Rotation);

        // transform capsule line segment to box's local space
        Vector3 localTop = Vector3.Transform(capsuleTop - boxCenter, invBoxRotation);
        Vector3 localBottom = Vector3.Transform(capsuleBottom - boxCenter, invBoxRotation);
        Vector3 boxHalfSize = box.Size * HALF;

        // find the closest point on the box to the capsule's line segment
        Vector3 closestPointOnLine = ClosestPointOnLineSegment(localTop, localBottom, Vector3.Zero);

        // clamp this point to the box surface
        var closestPointOnBox = new Vector3(
            Math.Clamp(closestPointOnLine.X, -boxHalfSize.X, boxHalfSize.X),
            Math.Clamp(closestPointOnLine.Y, -boxHalfSize.Y, boxHalfSize.Y),
            Math.Clamp(closestPointOnLine.Z, -boxHalfSize.Z, boxHalfSize.Z)
        );

        // find the closest point on the line segment to the closest point on the box
        Vector3 finalClosestOnLine = ClosestPointOnLineSegment(localTop, localBottom, closestPointOnBox);

        Vector3 localDirection = finalClosestOnLine - closestPointOnBox;
        float distanceSq = localDirection.LengthSquared();
        float capsuleRadiusSq = capsuleRadius * capsuleRadius;

        // no collision if distance squared is greater than capsule radius squared
        if (distanceSq >= capsuleRadiusSq) return false;

        float distance = MathF.Sqrt(distanceSq);
        if (distance < EPSILON)
        {
            // handle case where closest points overlap - find the closest face of the box
            Vector3 localNormal = Vector3.Zero;
            float minDistance = float.MaxValue;

            float distanceX = boxHalfSize.X - MathF.Abs(finalClosestOnLine.X);
            float distanceY = boxHalfSize.Y - MathF.Abs(finalClosestOnLine.Y);
            float distanceZ = boxHalfSize.Z - MathF.Abs(finalClosestOnLine.Z);

            if (distanceX < minDistance)
            {
                minDistance = distanceX;
                localNormal = new Vector3(finalClosestOnLine.X > 0 ? 1f : -1f, 0f, 0f);
            }
            if (distanceY < minDistance)
            {
                minDistance = distanceY;
                localNormal = new Vector3(0f, finalClosestOnLine.Y > 0 ? 1f : -1f, 0f);
            }
            if (distanceZ < minDistance)
            {
                minDistance = distanceZ;
                localNormal = new Vector3(0f, 0f, finalClosestOnLine.Z > 0 ? 1f : -1f);
            }

            normal = Vector3.Transform(localNormal, box.Transform.Rotation);
            penetrationDepth = capsuleRadius + minDistance;
        }
        else
        {
            float invDistance = 1.0f / distance;
            Vector3 localNormal = localDirection * invDistance; // fast normalization
            normal = Vector3.Transform(localNormal, box.Transform.Rotation);
            penetrationDepth = capsuleRadius - distance;
        }

        return true;
    }

    /// <summary>
    /// Checks for collision between two spheres.
    /// </summary>
    /// <param name="a">The first sphere collider.</param>
    /// <param name="b">The second sphere collider.</param>
    /// <param name="normal">The resulting collision normal (direction from a to b).</param>
    /// <param name="penetrationDepth">The penetration depth along the collision normal.</param>
    /// <returns>True if spheres are colliding, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SphereVsSphere(
        SphereCollider a,
        SphereCollider b,
        out Vector3 normal,
        out float penetrationDepth
    )
    {
        normal = Vector3.Zero;
        penetrationDepth = 0f;

        Vector3 centerA = a.Transform.Position;
        Vector3 centerB = b.Transform.Position;
        float diameterA = a.Diameter;
        float diameterB = b.Diameter;

        Vector3 direction = centerB - centerA;
        float distanceSq = direction.LengthSquared();
        float combinedDiameter = diameterA + diameterB;
        float combinedDiameterSq = combinedDiameter * combinedDiameter;

        // no collision if distance squared is greater than combined diameter squared
        if (distanceSq >= combinedDiameterSq) return false;

        // handle case where spheres are at the same position
        float distance = MathF.Sqrt(distanceSq);
        if (distance < EPSILON)
        {
            normal = s_unitX; // use cached unit vector
            penetrationDepth = combinedDiameter;
        }
        else
        {
            float invDistance = 1.0f / distance;
            normal = direction * invDistance; // fast normalization
            penetrationDepth = combinedDiameter - distance;
        }

        return true;
    }

    /// <summary>
    /// Checks for collision between a sphere and a capsule.
    /// </summary>
    /// <param name="sphere">The sphere collider.</param>
    /// <param name="capsule">The capsule collider.</param>
    /// <param name="normal">The resulting collision normal (direction from sphere to capsule).</param>
    /// <param name="penetrationDepth">The penetration depth along the collision normal.</param>
    /// <returns>True if sphere and capsule are colliding, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SphereVsCapsule(
        SphereCollider sphere,
        CapsuleCollider capsule,
        out Vector3 normal,
        out float penetrationDepth
    )
    {
        normal = Vector3.Zero;
        penetrationDepth = 0f;

        Vector3 sphereCenter = sphere.Transform.Position;
        Vector3 capsuleTop = capsule.TopCenter;
        Vector3 capsuleBottom = capsule.BottomCenter;

        // find closest point on capsule's central line segment to sphere center
        var closestPointOnCapsule = ClosestPointOnLineSegment(capsuleTop, capsuleBottom, sphereCenter);

        Vector3 direction = sphereCenter - closestPointOnCapsule;
        float distanceSq = direction.LengthSquared();
        float combinedRadius = sphere.Diameter + capsule.Radius;
        float combinedRadiusSq = combinedRadius * combinedRadius;

        // no collision if distance squared is greater than combined radius squared
        if (distanceSq >= combinedRadiusSq) return false;

        float distance = MathF.Sqrt(distanceSq);
        if (distance < EPSILON)
        {
            // handle case where sphere center is exactly on the capsule's axis
            Vector3 capsuleCenter = (capsuleTop + capsuleBottom) * HALF;
            Vector3 toSphere = sphereCenter - capsuleCenter;
            float toSphereLengthSq = toSphere.LengthSquared();

            if (toSphereLengthSq < EPSILON)
            {
                // use capsule's right direction as normal (perpendicular to up direction)
                Vector3 capsuleUp = capsule.UpDirection;
                normal = Vector3.Cross(capsuleUp, s_unitX);
                if (normal.LengthSquared() < EPSILON)
                {
                    normal = Vector3.Cross(capsuleUp, s_unitZ);
                }

                float normalLengthSq = normal.LengthSquared();
                if (normalLengthSq > EPSILON)
                {
                    normal *= 1.0f / MathF.Sqrt(normalLengthSq); // fast normalization
                }
                else
                {
                    normal = s_unitX;
                }
            }
            else
            {
                float invToSphereLength = 1.0f / MathF.Sqrt(toSphereLengthSq);
                normal = toSphere * invToSphereLength; // fast normalization
            }
            penetrationDepth = combinedRadius;
        }
        else
        {
            float invDistance = 1.0f / distance;
            normal = -direction * invDistance; // fast normalization
            penetrationDepth = combinedRadius - distance;
        }

        return true;
    }

    /// <summary>
    /// Checks for collision between two capsules.
    /// </summary>
    /// <param name="a">The first capsule collider.</param>
    /// <param name="b">The second capsule collider.</param>
    /// <param name="normal">The resulting collision normal (direction from a to b).</param>
    /// <param name="penetrationDepth">The penetration depth along the collision normal.</param>
    /// <returns>True if capsules are colliding, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CapsuleVsCapsule(
        CapsuleCollider a,
        CapsuleCollider b,
        out Vector3 normal,
        out float penetrationDepth
    )
    {
        normal = Vector3.Zero;
        penetrationDepth = 0f;

        // get line segments for both capsules
        Vector3 a1 = a.TopCenter;
        Vector3 a2 = a.BottomCenter;
        Vector3 b1 = b.TopCenter;
        Vector3 b2 = b.BottomCenter;

        // find closest points between the two line segments
        ClosestPointsBetweenLineSegments(a1, a2, b1, b2, out Vector3 closestA, out Vector3 closestB);

        Vector3 direction = closestB - closestA;
        float distanceSq = direction.LengthSquared();
        float combinedRadius = a.Radius + b.Radius;
        float combinedRadiusSq = combinedRadius * combinedRadius;

        // no collision if distance squared is greater than combined radius squared
        if (distanceSq >= combinedRadiusSq) return false;

        float distance = MathF.Sqrt(distanceSq);
        if (distance < EPSILON)
        {
            // handle case where line segments intersect or are very close
            Vector3 aUp = a.UpDirection;
            Vector3 bUp = b.UpDirection;
            Vector3 crossProduct = Vector3.Cross(aUp, bUp);
            float crossLengthSq = crossProduct.LengthSquared();

            if (crossLengthSq < EPSILON)
            {
                // capsules are parallel, use arbitrary perpendicular direction
                normal = Vector3.Cross(aUp, s_unitX);
                if (normal.LengthSquared() < EPSILON)
                {
                    normal = Vector3.Cross(aUp, s_unitZ);
                }

                float normalLengthSq = normal.LengthSquared();
                if (normalLengthSq > EPSILON)
                {
                    normal *= 1.0f / MathF.Sqrt(normalLengthSq); // fast normalization
                }
                else
                {
                    normal = s_unitX;
                }
            }
            else
            {
                float invCrossLength = 1f / MathF.Sqrt(crossLengthSq);
                normal = crossProduct * invCrossLength; // fast normalization
            }

            penetrationDepth = combinedRadius;
        }
        else
        {
            float invDistance = 1f / distance;
            normal = direction * invDistance; // fast normalization
            penetrationDepth = combinedRadius - distance;
        }

        return true;
    }

    /// <summary>
    /// Gets the local axes (X, Y, Z) of a box after applying rotation.
    /// </summary>
    /// <param name="rotation">The box rotation quaternion.</param>
    /// <param name="axesBuffer">Pre-allocated buffer to store the computed axes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ComputeBoxAxes(Quaternion rotation, Span<Vector3> axesBuffer)
    {
        axesBuffer[0] = Vector3.Transform(s_unitX, rotation);
        axesBuffer[1] = Vector3.Transform(s_unitY, rotation);
        axesBuffer[2] = Vector3.Transform(s_unitZ, rotation);
    }

    /// <summary>
    /// Computes the projection radius of a box onto a given axis.
    /// Used in SAT to test overlap along that axis.
    /// </summary>
    /// <param name="halfSize">Pre-computed half-size of the box.</param>
    /// <param name="axesBuffer">The local axes buffer of the box.</param>
    /// <param name="axis">The axis to project onto.</param>
    /// <returns>The projection radius.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ComputeProjectionRadius(
        in Vector3 halfSize,
        Span<Vector3> axesBuffer,
        in Vector3 axis
    )
        => MathF.Abs(Vector3.Dot(axesBuffer[0], axis) * halfSize.X) +
           MathF.Abs(Vector3.Dot(axesBuffer[1], axis) * halfSize.Y) +
           MathF.Abs(Vector3.Dot(axesBuffer[2], axis) * halfSize.Z);

    /// <summary>
    /// Finds the closest point on a line segment to a given point.
    /// </summary>
    /// <param name="lineStart">The start point of the line segment.</param>
    /// <param name="lineEnd">The end point of the line segment.</param>
    /// <param name="point">The point to find the closest point to.</param>
    /// <returns>The closest point on the line segment.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 ClosestPointOnLineSegment(
        in Vector3 lineStart,
        in Vector3 lineEnd,
        in Vector3 point
    )
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLengthSq = lineDirection.LengthSquared();

        if (lineLengthSq < EPSILON) return lineStart; // degenerate line segment

        Vector3 toPoint = point - lineStart;
        float projection = Vector3.Dot(toPoint, lineDirection) / lineLengthSq;

        projection = Math.Clamp(projection, 0f, 1f);

        return lineStart + lineDirection * projection;
    }

    /// <summary>
    /// Finds the closest points between two line segments.
    /// </summary>
    /// <param name="a1">Start point of first line segment.</param>
    /// <param name="a2">End point of first line segment.</param>
    /// <param name="b1">Start point of second line segment.</param>
    /// <param name="b2">End point of second line segment.</param>
    /// <param name="closestA">Closest point on first line segment.</param>
    /// <param name="closestB">Closest point on second line segment.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClosestPointsBetweenLineSegments(
        in Vector3 a1,
        in Vector3 a2,
        in Vector3 b1,
        in Vector3 b2,
        out Vector3 closestA,
        out Vector3 closestB
    )
    {
        Vector3 d1 = a2 - a1;
        Vector3 d2 = b2 - b1;
        Vector3 r = a1 - b1;

        float a = Vector3.Dot(d1, d1);
        float e = Vector3.Dot(d2, d2);
        float f = Vector3.Dot(d2, r);

        float s, t;

        if (a <= EPSILON && e <= EPSILON)
        {
            // both segments are points
            s = t = 0f;
        }
        else if (a <= EPSILON)
        {
            // first segment is a point
            s = 0f;
            t = Math.Clamp(f / e, 0f, 1f);
        }
        else if (e <= EPSILON)
        {
            // second segment is a point
            t = 0f;
            s = Math.Clamp(-Vector3.Dot(d1, r) / a, 0f, 1f);
        }
        else
        {
            // general case
            float c = Vector3.Dot(d1, r);
            float b = Vector3.Dot(d1, d2);
            float denom = a * e - b * b;

            if (MathF.Abs(denom) > EPSILON)
            {
                s = Math.Clamp((b * f - c * e) / denom, 0f, 1f);
            }
            else
            {
                s = 0f;
            }

            t = (b * s + f) / e;

            if (t < 0f)
            {
                t = 0f;
                s = Math.Clamp(-c / a, 0f, 1f);
            }
            else if (t > 1f)
            {
                t = 1f;
                s = Math.Clamp((b - c) / a, 0f, 1f);
            }
        }

        closestA = a1 + s * d1;
        closestB = b1 + t * d2;
    }
}

using System.Numerics;
using Engine.Physics.Colliders;

namespace Engine.Physics.Utilities;

/// <summary>
/// Provides static helper methods for collision detection between various types of colliders.
/// </summary>
public static class CollisionHelper
{
    /// <summary>
    /// Checks for collision between two oriented bounding boxes (OBB) using the Separating Axis Theorem (SAT).
    /// </summary>
    /// <param name="a">The first box collider.</param>
    /// <param name="b">The second box collider.</param>
    /// <param name="normal">The resulting collision normal (direction from a to b).</param>
    /// <param name="penetrationDepth">The penetration depth along the collision normal.</param>
    /// <returns>True if boxes are colliding, otherwise false.</returns>
    public static bool BoxVsBox(BoxCollider a, BoxCollider b, out Vector3 normal, out float penetrationDepth)
    {
        normal = Vector3.Zero;
        penetrationDepth = float.MaxValue;

        // local axes of each box (after rotation)
        Vector3[] axesA = GetBoxAxes(a.Transform.Rotation);
        Vector3[] axesB = GetBoxAxes(b.Transform.Rotation);

        // axes to test for separation (15 in total for SAT)
        var testAxes = new Vector3[15];
        Array.Copy(axesA, 0, testAxes, 0, 3); // 3 face normals from box A
        Array.Copy(axesB, 0, testAxes, 3, 3); // 3 face normals from box B

        // 9 axes from cross products of edges of both boxes
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                var cross = Vector3.Cross(axesA[i], axesB[j]);
                if (cross.LengthSquared() > 0.0001f) // avoid degenerate axes
                {
                    testAxes[6 + i * 3 + j] = Vector3.Normalize(cross);
                }
            }
        }

        Vector3 separation = b.Transform.Position - a.Transform.Position;

        // test all axes for overlap
        for (int i = 0; i < 15; i++)
        {
            Vector3 axis = testAxes[i];
            if (axis.LengthSquared() < 0.0001f) continue; // skip zero vectors

            float projectionA = GetProjectionRadius(a.Size, axesA, axis);
            float projectionB = GetProjectionRadius(b.Size, axesB, axis);
            float distance = MathF.Abs(Vector3.Dot(separation, axis));

            float overlap = projectionA + projectionB - distance;

            // separating axis found — no collision
            if (overlap <= 0f) return false;

            // track axis with least penetration
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
    public static bool BoxVsSphere(BoxCollider box, SphereCollider sphere, out Vector3 normal, out float penetrationDepth)
    {
        normal = Vector3.Zero;
        penetrationDepth = 0f;

        var sphereCenter = sphere.Transform.Position;
        var boxCenter = box.Transform.Position;

        // transform sphere center to box's local space
        var relativePosition = sphereCenter - boxCenter;
        var localSphereCenter = Vector3.Transform(relativePosition, Quaternion.Conjugate(box.Transform.Rotation));

        var boxHalfSize = box.Size * 0.5f;

        // find the closest point on the box to the sphere center
        var closestPoint = new Vector3(
            Math.Clamp(localSphereCenter.X, -boxHalfSize.X, boxHalfSize.X),
            Math.Clamp(localSphereCenter.Y, -boxHalfSize.Y, boxHalfSize.Y),
            Math.Clamp(localSphereCenter.Z, -boxHalfSize.Z, boxHalfSize.Z)
        );

        Vector3 localDirection = localSphereCenter - closestPoint;
        float distance = localDirection.Length();

        // no collision if distance is greater than sphere diameter
        if (distance >= sphere.Diameter) return false;

        if (distance < 0.0001f)
        {
            // sphere center is inside the box - find the closest face
            var localNormal = Vector3.Zero;
            var minDistance = float.MaxValue;

            // check distance to each face
            float[] distances = [
                boxHalfSize.X - MathF.Abs(localSphereCenter.X), // X faces
                boxHalfSize.Y - MathF.Abs(localSphereCenter.Y), // Y faces
                boxHalfSize.Z - MathF.Abs(localSphereCenter.Z)  // Z faces
            ];

            for (int i = 0; i < 3; i++)
            {
                if (distances[i] < minDistance)
                {
                    minDistance = distances[i];
                    localNormal = Vector3.Zero;
                    localNormal[i] = localSphereCenter[i] > 0 ? 1f : -1f;
                }
            }

            normal = Vector3.Transform(localNormal, box.Transform.Rotation);
            penetrationDepth = sphere.Diameter + minDistance;
        }
        else
        {
            var localNormal = Vector3.Normalize(localDirection);
            normal = Vector3.Transform(localNormal, box.Transform.Rotation);
            penetrationDepth = sphere.Diameter - distance;
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
    public static bool BoxVsCapsule(BoxCollider box, CapsuleCollider capsule, out Vector3 normal, out float penetrationDepth)
    {
        normal = Vector3.Zero;
        penetrationDepth = 0f;

        // get the line segment representing the capsule's central axis
        Vector3 capsuleTop = capsule.TopCenter;
        Vector3 capsuleBottom = capsule.BottomCenter;
        float capsuleRadius = capsule.Radius;

        // transform capsule line segment to box's local space
        var boxCenter = box.Transform.Position;
        var invBoxRotation = Quaternion.Conjugate(box.Transform.Rotation);

        Vector3 localTop = Vector3.Transform(capsuleTop - boxCenter, invBoxRotation);
        Vector3 localBottom = Vector3.Transform(capsuleBottom - boxCenter, invBoxRotation);
        Vector3 boxHalfSize = box.Size * 0.5f;

        // find the closest point on the box to the capsule's line segment
        var closestPointOnLine = ClosestPointOnLineSegment(localTop, localBottom, Vector3.Zero);

        // clamp this point to the box surface
        var closestPointOnBox = new Vector3(
            Math.Clamp(closestPointOnLine.X, -boxHalfSize.X, boxHalfSize.X),
            Math.Clamp(closestPointOnLine.Y, -boxHalfSize.Y, boxHalfSize.Y),
            Math.Clamp(closestPointOnLine.Z, -boxHalfSize.Z, boxHalfSize.Z)
        );

        // find the closest point on the line segment to the closest point on the box
        var finalClosestOnLine = ClosestPointOnLineSegment(localTop, localBottom, closestPointOnBox);

        Vector3 localDirection = finalClosestOnLine - closestPointOnBox;
        float distance = localDirection.Length();

        // no collision if distance is greater than capsule radius
        if (distance >= capsuleRadius) return false;

        if (distance < 0.0001f)
        {
            // handle case where closest points overlap
            // find the closest face of the box
            var localNormal = Vector3.Zero;
            float minDistance = float.MaxValue;

            float[] distances = [
                boxHalfSize.X - MathF.Abs(finalClosestOnLine.X),
                boxHalfSize.Y - MathF.Abs(finalClosestOnLine.Y),
                boxHalfSize.Z - MathF.Abs(finalClosestOnLine.Z)
            ];

            for (int i = 0; i < 3; i++)
            {
                if (distances[i] < minDistance)
                {
                    minDistance = distances[i];
                    localNormal = Vector3.Zero;
                    localNormal[i] = finalClosestOnLine[i] > 0 ? 1f : -1f;
                }
            }

            normal = Vector3.Transform(localNormal, box.Transform.Rotation);
            penetrationDepth = capsuleRadius + minDistance;
        }
        else
        {
            Vector3 localNormal = Vector3.Normalize(localDirection);
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
    public static bool SphereVsSphere(SphereCollider a, SphereCollider b, out Vector3 normal, out float penetrationDepth)
    {
        normal = Vector3.Zero;
        penetrationDepth = 0f;

        Vector3 centerA = a.Transform.Position;
        Vector3 centerB = b.Transform.Position;

        Vector3 direction = centerB - centerA;
        float distance = direction.Length();
        float combinedDiameter = a.Diameter + b.Diameter;

        // no collision if distance is greater than combined diameter
        if (distance >= combinedDiameter) return false;

        // handle case where spheres are at the same position
        if (distance < 0.0001f)
        {
            normal = Vector3.UnitX; // arbitrary direction
            penetrationDepth = combinedDiameter;
        }
        else
        {
            normal = Vector3.Normalize(direction);
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
    public static bool SphereVsCapsule(SphereCollider sphere, CapsuleCollider capsule, out Vector3 normal, out float penetrationDepth)
    {
        normal = Vector3.Zero;
        penetrationDepth = 0f;

        Vector3 sphereCenter = sphere.Transform.Position;
        Vector3 capsuleTop = capsule.TopCenter;
        Vector3 capsuleBottom = capsule.BottomCenter;

        // find closest point on capsule's central line segment to sphere center
        var closestPointOnCapsule = ClosestPointOnLineSegment(capsuleTop, capsuleBottom, sphereCenter);

        Vector3 direction = sphereCenter - closestPointOnCapsule;
        float distance = direction.Length();
        float combinedRadius = sphere.Diameter + capsule.Radius;

        // no collision if distance is greater than combined sphere diameter and capsule radius
        if (distance >= combinedRadius) return false;

        if (distance < 0.0001f)
        {
            // handle case where sphere center is exactly on the capsule's axis
            // find the vector from capsule center to sphere center
            Vector3 capsuleCenter = (capsuleTop + capsuleBottom) * 0.5f;
            Vector3 toSphere = sphereCenter - capsuleCenter;

            if (toSphere.LengthSquared() < 0.0001f)
            {
                // use capsule's right direction as normal (perpendicular to up direction)
                Vector3 capsuleUp = capsule.UpDirection;
                normal = Vector3.Cross(capsuleUp, Vector3.UnitX);
                if (normal.LengthSquared() < 0.0001f)
                {
                    normal = Vector3.Cross(capsuleUp, Vector3.UnitZ);
                }
                normal = Vector3.Normalize(normal);
            }
            else
            {
                normal = Vector3.Normalize(toSphere);
            }
            penetrationDepth = combinedRadius;
        }
        else
        {
            // normal should point from capsule to sphere (opposite of direction)
            normal = -Vector3.Normalize(direction);
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
    public static bool CapsuleVsCapsule(CapsuleCollider a, CapsuleCollider b, out Vector3 normal, out float penetrationDepth)
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
        float distance = direction.Length();
        float combinedRadius = a.Radius + b.Radius;

        // no collision if distance is greater than combined radii
        if (distance >= combinedRadius) return false;

        if (distance < 0.0001f)
        {
            // handle case where line segments intersect or are very close
            // use cross product of both capsules' up directions
            Vector3 crossProduct = Vector3.Cross(a.UpDirection, b.UpDirection);
            if (crossProduct.LengthSquared() < 0.0001f)
            {
                // capsules are parallel, use arbitrary perpendicular direction
                normal = Vector3.Cross(a.UpDirection, Vector3.UnitX);
                if (normal.LengthSquared() < 0.0001f)
                {
                    normal = Vector3.Cross(a.UpDirection, Vector3.UnitZ);
                }
                normal = Vector3.Normalize(normal);
            }
            else
            {
                normal = Vector3.Normalize(crossProduct);
            }
            penetrationDepth = combinedRadius;
        }
        else
        {
            normal = Vector3.Normalize(direction);
            penetrationDepth = combinedRadius - distance;
        }

        return true;
    }

    /// <summary>
    /// Gets the local axes (X, Y, Z) of a box after applying rotation.
    /// </summary>
    /// <param name="rotation">The box rotation (as a quaternion).</param>
    private static Vector3[] GetBoxAxes(Quaternion rotation)
        => [
            Vector3.Transform(Vector3.UnitX, rotation),
            Vector3.Transform(Vector3.UnitY, rotation),
            Vector3.Transform(Vector3.UnitZ, rotation)
        ];

    /// <summary>
    /// Computes the projection radius of a box onto a given axis.
    /// Used in SAT to test overlap along that axis.
    /// </summary>
    /// <param name="size">The size (extents) of the box.</param>
    /// <param name="axes">The local axes of the box (after rotation).</param>
    /// <param name="axis">The axis to project onto.</param>
    private static float GetProjectionRadius(Vector3 size, Vector3[] axes, Vector3 axis)
        => MathF.Abs(Vector3.Dot(axes[0] * size.X * 0.5f, axis)) +
           MathF.Abs(Vector3.Dot(axes[1] * size.Y * 0.5f, axis)) +
           MathF.Abs(Vector3.Dot(axes[2] * size.Z * 0.5f, axis));

    /// <summary>
    /// Finds the closest point on a line segment to a given point.
    /// </summary>
    /// <param name="lineStart">The start point of the line segment.</param>
    /// <param name="lineEnd">The end point of the line segment.</param>
    /// <param name="point">The point to find the closest point to.</param>
    /// <returns>The closest point on the line segment.</returns>
    private static Vector3 ClosestPointOnLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLength = lineDirection.Length();

        if (lineLength < 0.0001f) return lineStart; // degenerate line segment

        lineDirection /= lineLength;
        Vector3 toPoint = point - lineStart;
        float projection = Vector3.Dot(toPoint, lineDirection);

        // clamp projection to line segment bounds
        projection = Math.Clamp(projection, 0f, lineLength);

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
    private static void ClosestPointsBetweenLineSegments(
        Vector3 a1,
        Vector3 a2,
        Vector3 b1,
        Vector3 b2,
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

        if (a <= 0.0001f && e <= 0.0001f)
        {
            // both segments are points
            s = t = 0f;
        }
        else if (a <= 0.0001f)
        {
            // first segment is a point
            s = 0f;
            t = Math.Clamp(f / e, 0f, 1f);
        }
        else if (e <= 0.0001f)
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

            if (denom != 0f)
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

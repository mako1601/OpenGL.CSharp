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

        // no collision if distance is greater than sphere radius
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
    /// Not implemented.
    /// </summary>
    public static bool BoxVsCapsule(BoxCollider box, CapsuleCollider capsule)
        => throw new NotImplementedException();

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

        // no collision if distance is greater than combined radii
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
    /// Not implemented.
    /// </summary>
    public static bool SphereVsCapsule(SphereCollider sphere, CapsuleCollider capsule)
        => throw new NotImplementedException();

    /// <summary>
    /// Checks for collision between two capsules.
    /// Not implemented.
    /// </summary>
    public static bool CapsuleVsCapsule(CapsuleCollider a, CapsuleCollider b)
        => throw new NotImplementedException();

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
}

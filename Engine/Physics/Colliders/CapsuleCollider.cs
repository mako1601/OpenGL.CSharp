using System.Numerics;
using Engine.Physics.Utilities;

namespace Engine.Physics.Colliders;

/// <summary>
/// Represents a capsule-shaped collider used in the collision physics.
/// A capsule is essentially a cylinder with hemispherical caps on both ends.
/// </summary>
/// <param name="transform">The transform of the capsule collider.</param>
/// <param name="size">The size of the capsule collider.</param>
public class CapsuleCollider(Transform transform, Vector3 size) : Collider(transform, size)
{
    /// <summary>
    /// Gets the radius of the capsule (radius of the cylindrical body and hemispherical caps).
    /// Calculated as half of the smallest horizontal dimension (X or Z) of the Size.
    /// </summary>
    public float Radius => MathF.Min(Size.X, Size.Z) * 0.5f;

    /// <summary>
    /// Gets the height of the cylindrical portion of the capsule (excluding the hemispherical caps).
    /// Calculated as the Y dimension of Size minus two times the radius.
    /// If the result is negative, returns 0 (degenerate case where capsule becomes a sphere).
    /// </summary>
    public float CylinderHeight => MathF.Max(0f, Size.Y - 2f * Radius);

    /// <summary>
    /// Gets the total height of the capsule including the hemispherical caps.
    /// This is simply the Y dimension of the Size.
    /// </summary>
    public float TotalHeight => Size.Y;

    /// <summary>
    /// Gets the center position of the top hemispherical cap in world space.
    /// </summary>
    public Vector3 TopCenter
    {
        get
        {
            Vector3 localOffset = new(0f, CylinderHeight * 0.5f, 0f);
            Vector3 worldOffset = Vector3.Transform(localOffset, Transform.Rotation);
            return Transform.Position + worldOffset;
        }
    }

    /// <summary>
    /// Gets the center position of the bottom hemispherical cap in world space.
    /// </summary>
    public Vector3 BottomCenter
    {
        get
        {
            Vector3 localOffset = new(0f, -CylinderHeight * 0.5f, 0f);
            Vector3 worldOffset = Vector3.Transform(localOffset, Transform.Rotation);
            return Transform.Position + worldOffset;
        }
    }

    /// <summary>
    /// Gets the up direction of the capsule in world space (direction from bottom to top).
    /// </summary>
    public Vector3 UpDirection => Vector3.Transform(Vector3.UnitY, Transform.Rotation);
}

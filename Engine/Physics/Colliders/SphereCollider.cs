using System.Numerics;
using Engine.Physics.Utilities;

namespace Engine.Physics.Colliders;

/// <summary>
/// Represents a spherical collider used in the collision physics.
/// </summary>
/// <param name="transform">The transform of the sphere collider.</param>
/// <param name="size">The size of the sphere collider.</param>
public class SphereCollider(Transform transform, Vector3 size) : Collider(transform, size)
{
    /// <summary>
    /// Gets the diameter of the sphere collider,
    /// calculated from the maximum component of the transform's scale.
    /// </summary>
    public float Diameter => MathF.Max(MathF.Max(Size.X, Size.Y), Size.Z);

    /// <summary>
    /// Gets the radius of the sphere collider (half of the diameter).
    /// </summary>
    public float Radius => Diameter * 0.5f;
}

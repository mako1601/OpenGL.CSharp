using System.Numerics;
using Engine.Physics.Utilities;

namespace Engine.Physics.Colliders;

/// <summary>
/// Represents the base class for all colliders used in the collision physics.
/// </summary>
/// <param name="transform">The transform of the collider.</param>
/// <param name="size">The size of the collider.</param>
public abstract class Collider(Transform transform, Vector3 size)
{
    /// <summary>
    /// Gets or sets the size of the collider.
    /// The result of multiplying the base size by the transform's scale.
    /// </summary>
    public Vector3 Size { get; set; } = size * transform.Scale;

    /// <summary>
    /// Gets or sets the transform of the collider.
    /// </summary>
    public Transform Transform { get; set; } = transform;
}

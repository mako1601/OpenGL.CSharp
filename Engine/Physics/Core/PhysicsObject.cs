using System.Numerics;
using Engine.Physics.Colliders;
using Engine.Physics.Utilities;

namespace Engine.Physics.Core;

/// <summary>
/// Represents a physical object in the physics simulation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PhysicsObject"/> class.
/// </remarks>
/// <param name="transform">The transform of the physics object.</param>
/// <param name="rigidbody">The rigidbody of the physics object.</param>
/// <param name="collider">The collider of the physics object.</param>
public class PhysicsObject(Transform transform, Rigidbody rigidbody, Collider collider)
{
    /// <summary>
    /// Gets the transform of the physics object.
    /// </summary>
    public Transform Transform { get; } = transform;

    /// <summary>
    /// Gets the rigidbody of the physics object.
    /// </summary>
    public Rigidbody Rigidbody { get; } = rigidbody;

    /// <summary>
    /// Gets the collider of the physics object.
    /// </summary>
    public Collider Collider { get; } = collider;

    /// <summary>
    /// Gets the current color of the physics object (used for debugging or visualization collision).
    /// </summary>
    public Vector4 CurrentColor = new(1f);
}

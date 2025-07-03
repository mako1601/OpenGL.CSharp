using Engine.Physics.Utilities;

namespace Engine.Physics.Colliders;

/// <summary>
/// Represents a box-shaped collider used in the collision physics.
/// </summary>
/// <param name="transform">The transform of the box collider.</param>
public class BoxCollider(Transform transform) : Collider(transform) { }

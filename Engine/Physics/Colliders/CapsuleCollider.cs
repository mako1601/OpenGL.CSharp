using Engine.Physics.Utilities;

namespace Engine.Physics.Colliders;

/// <summary>
/// Represents a capsule-shaped collider used in the collision physics.
/// </summary>
/// <param name="transform">The transform of the capsule collider.</param>
public class CapsuleCollider(Transform transform) : Collider(transform) { }

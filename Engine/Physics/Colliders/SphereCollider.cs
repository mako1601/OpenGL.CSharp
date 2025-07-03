using Engine.Physics.Utilities;

namespace Engine.Physics.Colliders;

/// <summary>
/// Represents a spherical collider used in the collision physics.
/// </summary>
/// <param name="transform">The transform of the sphere collider.</param>
public class SphereCollider(Transform transform) : Collider(transform) { }

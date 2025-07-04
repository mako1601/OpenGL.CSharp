using System.Numerics;
using Engine.Physics.Utilities;

namespace Engine.Physics.Colliders;

/// <summary>
/// Represents a box-shaped collider used in the collision physics.
/// </summary>
/// <param name="transform">The transform of the box collider.</param>
/// <param name="size">The size of the box collider.</param>
public class BoxCollider(Transform transform, Vector3 size) : Collider(transform, size) { }

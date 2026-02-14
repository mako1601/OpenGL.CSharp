using System.Numerics;

namespace Engine.Physics.Colliders;

public abstract class Collider
{
    public PhysicsBody Body { get; internal set; } = null!;
    public Vector3 Offset { get; set; } = Vector3.Zero;

    public abstract Aabb GetAabb();
}

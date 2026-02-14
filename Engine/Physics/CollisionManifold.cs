using System.Numerics;

namespace Engine.Physics;

public readonly struct CollisionManifold(Vector3 normal, float penetration)
{
    public Vector3 Normal { get; } = normal;
    public float Penetration { get; } = penetration;
}

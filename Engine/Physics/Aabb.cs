using System.Numerics;

namespace Engine.Physics;

public readonly struct Aabb(Vector3 min, Vector3 max)
{
    public Vector3 Min { get; } = min;
    public Vector3 Max { get; } = max;
    public Vector3 Center => (Min + Max) * 0.5f;
    public Vector3 HalfExtents => (Max - Min) * 0.5f;
}

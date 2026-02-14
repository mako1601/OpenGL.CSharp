using System.Numerics;

namespace Engine.Physics.Colliders;

public sealed class BoxCollider : Collider
{
    private Vector3 _halfExtents;

    public BoxCollider(Vector3 halfExtents)
    {
        HalfExtents = halfExtents;
    }

    public Vector3 HalfExtents
    {
        get => _halfExtents;
        set
        {
            if (value.X <= 0f || value.Y <= 0f || value.Z <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Half extents must be > 0.");
            }

            _halfExtents = value;
        }
    }

    public override Aabb GetAabb()
    {
        Vector3 center = Body.Position + Offset;
        return new Aabb(center - HalfExtents, center + HalfExtents);
    }
}

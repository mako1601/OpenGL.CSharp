using System.Numerics;

namespace Engine.Physics.Colliders;

public sealed class CapsuleCollider : Collider
{
    private float _radius;
    private float _halfHeight;

    public CapsuleCollider(float radius, float halfHeight)
    {
        Radius = radius;
        HalfHeight = halfHeight;
    }

    public float Radius
    {
        get => _radius;
        set
        {
            if (value <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Radius must be > 0.");
            }

            _radius = value;
        }
    }

    public float HalfHeight
    {
        get => _halfHeight;
        set
        {
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Half height must be >= 0.");
            }

            _halfHeight = value;
        }
    }

    public Vector3 SegmentStart => Body.Position + Offset + new Vector3(0f, -HalfHeight, 0f);
    public Vector3 SegmentEnd => Body.Position + Offset + new Vector3(0f, HalfHeight, 0f);

    public override Aabb GetAabb()
    {
        Vector3 center = Body.Position + Offset;
        Vector3 extents = new(Radius, Radius + HalfHeight, Radius);
        return new Aabb(center - extents, center + extents);
    }
}

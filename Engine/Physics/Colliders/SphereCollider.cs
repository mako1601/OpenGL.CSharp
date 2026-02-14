using System.Numerics;

namespace Engine.Physics.Colliders;

public sealed class SphereCollider : Collider
{
    private float _radius;

    public SphereCollider(float radius)
    {
        Radius = radius;
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

    public override Aabb GetAabb()
    {
        Vector3 center = Body.Position + Offset;
        Vector3 extents = new(Radius);
        return new Aabb(center - extents, center + extents);
    }
}

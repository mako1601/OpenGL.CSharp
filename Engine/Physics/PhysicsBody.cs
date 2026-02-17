using System.Numerics;
using Engine.Physics.Colliders;

namespace Engine.Physics;

public sealed class PhysicsBody
{
    private float _mass = 1f;
    private float _restitution = 0.15f;
    private float _staticFriction = 0.6f;
    private float _dynamicFriction = 0.4f;

    public PhysicsBody(Collider collider, bool isStatic = false)
    {
        Collider = collider ?? throw new ArgumentNullException(nameof(collider));
        IsStatic = isStatic;
        Collider.Body = this;
    }

    public Vector3 Position { get; set; }
    public float Mass
    {
        get => _mass;
        set
        {
            if (!float.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Mass must be finite.");
            }

            if (!IsStatic && value <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Mass must be > 0 for dynamic bodies.");
            }

            _mass = value;
        }
    }
    public float InverseMass
    {
        get
        {
            if (IsStatic) return 0f;

            return Mass <= 0f ? 0f : 1f / Mass;
        }
    }
    public Vector3 Velocity { get; set; }
    public Vector3 Gravity { get; set; } = new(0f, -9.81f, 0f);
    public float Restitution
    {
        get => _restitution;
        set
        {
            if (!float.IsFinite(value) || value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Restitution must be finite and in [0, 1].");
            }

            _restitution = value;
        }
    }
    public float StaticFriction
    {
        get => _staticFriction;
        set
        {
            if (!float.IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Static friction must be finite and >= 0.");
            }

            if (_dynamicFriction > value)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Static friction must be >= dynamic friction.");
            }

            _staticFriction = value;
        }
    }
    public float DynamicFriction
    {
        get => _dynamicFriction;
        set
        {
            if (!float.IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Dynamic friction must be finite and >= 0.");
            }

            if (value > _staticFriction)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Dynamic friction must be <= static friction.");
            }

            _dynamicFriction = value;
        }
    }

    public bool IsStatic { get; }
    public bool IsGrounded { get; internal set; }

    public Collider Collider { get; }
    public uint CollisionLayer { get; set; } = CollisionLayers.Default;
    public uint CollisionMask { get; set; } = uint.MaxValue;

}

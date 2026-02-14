using System.Numerics;
using Engine.Physics.Colliders;

namespace Engine.Physics;

public sealed class PhysicsBody
{
    public PhysicsBody(Collider collider, bool isStatic = false)
    {
        Collider = collider ?? throw new ArgumentNullException(nameof(collider));
        IsStatic = isStatic;
        Collider.Body = this;
    }

    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public Vector3 Gravity { get; set; } = new(0f, -9.81f, 0f);
    public float Mass { get; set; } = 1f;
    public float Restitution { get; set; } = 0.15f;
    public float StaticFriction { get; set; } = 0.6f;
    public float DynamicFriction { get; set; } = 0.4f;
    public uint CollisionLayer { get; set; } = CollisionLayers.Default;
    public uint CollisionMask { get; set; } = uint.MaxValue;
    public bool IsStatic { get; }
    public Collider Collider { get; }
    public bool IsGrounded { get; internal set; }

    public float InverseMass
    {
        get
        {
            if (IsStatic) return 0f;

            return Mass <= 0f ? 0f : 1f / Mass;
        }
    }
}

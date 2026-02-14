using System.Numerics;
using Engine.Physics;

namespace Engine.Entities;

public abstract class Entity(PhysicsBody body)
{
    public PhysicsBody Body { get; } = body ?? throw new ArgumentNullException(nameof(body));

    public Vector3 Position
    {
        get => Body.Position;
        set => Body.Position = value;
    }
}

using System.Numerics;
using Engine.Physics.Colliders;

namespace Engine.Physics;

public sealed class PhysicsWorld
{
    private readonly List<PhysicsBody> _bodies = [];

    public IReadOnlyList<PhysicsBody> Bodies => _bodies;

    public PhysicsBody AddBody(PhysicsBody body)
    {
        ArgumentNullException.ThrowIfNull(body);

        _bodies.Add(body);
        return body;
    }

    public void Step(float dt)
    {
        if (dt <= 0f) return;

        foreach (var body in _bodies)
        {
            if (body.IsStatic) continue;

            body.IsGrounded = false;
            body.Velocity += body.Gravity * dt;
            body.Position += body.Velocity * dt;
        }

        for (int i = 0; i < _bodies.Count; i++)
        {
            var a = _bodies[i];
            for (int j = i + 1; j < _bodies.Count; j++)
            {
                var b = _bodies[j];

                if (a.IsStatic && b.IsStatic) continue;

                if (a.Collider is not BoxCollider boxA || b.Collider is not BoxCollider boxB)
                {
                    continue;
                }

                if (TryCollide(boxA, boxB, out var manifold))
                {
                    ResolveCollision(a, b, manifold);
                }
            }
        }
    }

    private static bool TryCollide(BoxCollider a, BoxCollider b, out CollisionManifold manifold)
    {
        Aabb aabbA = a.GetAabb();
        Aabb aabbB = b.GetAabb();

        Vector3 delta = aabbB.Center - aabbA.Center;
        Vector3 overlap = aabbA.HalfExtents + aabbB.HalfExtents - new Vector3(
            MathF.Abs(delta.X),
            MathF.Abs(delta.Y),
            MathF.Abs(delta.Z)
        );

        if (overlap.X <= 0f || overlap.Y <= 0f || overlap.Z <= 0f)
        {
            manifold = default;
            return false;
        }

        if (overlap.X < overlap.Y && overlap.X < overlap.Z)
        {
            float normalX = delta.X >= 0f ? 1f : -1f;
            manifold = new CollisionManifold(new Vector3(normalX, 0f, 0f), overlap.X);
            return true;
        }

        if (overlap.Y < overlap.Z)
        {
            float normalY = delta.Y >= 0f ? 1f : -1f;
            manifold = new CollisionManifold(new Vector3(0f, normalY, 0f), overlap.Y);
            return true;
        }

        float normalZ = delta.Z >= 0f ? 1f : -1f;
        manifold = new CollisionManifold(new Vector3(0f, 0f, normalZ), overlap.Z);
        return true;
    }

    private static void ResolveCollision(PhysicsBody a, PhysicsBody b, CollisionManifold manifold)
    {
        float invMassA = a.InverseMass;
        float invMassB = b.InverseMass;
        float invMassSum = invMassA + invMassB;

        if (invMassSum <= 0f) return;

        Vector3 relativeVelocity = b.Velocity - a.Velocity;
        float velocityAlongNormal = Vector3.Dot(relativeVelocity, manifold.Normal);

        if (velocityAlongNormal < 0f)
        {
            float restitution = MathF.Min(a.Restitution, b.Restitution);
            float impulseMagnitude = -(1f + restitution) * velocityAlongNormal / invMassSum;
            Vector3 impulse = impulseMagnitude * manifold.Normal;

            if (!a.IsStatic)
            {
                a.Velocity -= impulse * invMassA;
            }

            if (!b.IsStatic)
            {
                b.Velocity += impulse * invMassB;
            }
        }

        const float percent = 0.8f;
        const float slop = 0.0001f;
        float correctionMagnitude = MathF.Max(manifold.Penetration - slop, 0f) / invMassSum * percent;
        Vector3 correction = correctionMagnitude * manifold.Normal;

        if (!a.IsStatic)
        {
            a.Position -= correction * invMassA;
            if (manifold.Normal.Y < -0.5f)
            {
                a.IsGrounded = true;
            }
        }

        if (!b.IsStatic)
        {
            b.Position += correction * invMassB;
            if (manifold.Normal.Y > 0.5f)
            {
                b.IsGrounded = true;
            }
        }
    }
}

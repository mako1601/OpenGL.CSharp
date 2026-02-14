using System.Numerics;
using Engine.Physics.Colliders;

namespace Engine.Physics;

public sealed class PhysicsWorld
{
    private const int SolverIterations = 12;
    private const float PositionalCorrectionPercent = 0.95f;
    private const float PositionalCorrectionSlop = 0.0005f;

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

        for (int iteration = 0; iteration < SolverIterations; iteration++)
        {
            for (int i = 0; i < _bodies.Count; i++)
            {
                var a = _bodies[i];
                for (int j = i + 1; j < _bodies.Count; j++)
                {
                    var b = _bodies[j];

                    if (a.IsStatic && b.IsStatic) continue;

                    if (!CanBodiesCollide(a, b)) continue;

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

    private static bool CanBodiesCollide(PhysicsBody a, PhysicsBody b)
    {
        bool aWantsB = (a.CollisionMask & b.CollisionLayer) != 0u;
        bool bWantsA = (b.CollisionMask & a.CollisionLayer) != 0u;
        return aWantsB && bWantsA;
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

            Vector3 rvAfterNormal = b.Velocity - a.Velocity;
            Vector3 tangent = rvAfterNormal - Vector3.Dot(rvAfterNormal, manifold.Normal) * manifold.Normal;

            if (MathF.Abs(manifold.Normal.Y) < 0.5f)
            {
                tangent.Y = 0f;
            }

            if (tangent.LengthSquared() > 1e-8f)
            {
                tangent = Vector3.Normalize(tangent);

                float jt = -Vector3.Dot(rvAfterNormal, tangent) / invMassSum;
                float muS = MathF.Sqrt(a.StaticFriction * b.StaticFriction);
                float muD = MathF.Sqrt(a.DynamicFriction * b.DynamicFriction);

                Vector3 frictionImpulse = MathF.Abs(jt) < impulseMagnitude * muS
                    ? jt * tangent
                    : -impulseMagnitude * muD * tangent;

                if (!a.IsStatic)
                {
                    a.Velocity -= frictionImpulse * invMassA;
                }

                if (!b.IsStatic)
                {
                    b.Velocity += frictionImpulse * invMassB;
                }
            }
        }

        float correctionMagnitude = MathF.Max(manifold.Penetration - PositionalCorrectionSlop, 0f)
            / invMassSum
            * PositionalCorrectionPercent;
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

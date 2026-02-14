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

    public bool RemoveBody(PhysicsBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        return _bodies.Remove(body);
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

                    if (!TryCollide(a.Collider, b.Collider, out var manifold))
                    {
                        continue;
                    }

                    ResolveCollision(a, b, manifold);
                }
            }
        }
    }

    private static bool TryCollide(Collider a, Collider b, out CollisionManifold manifold)
    {
        if (a is BoxCollider boxA && b is BoxCollider boxB)
        {
            return TryCollide(boxA, boxB, out manifold);
        }

        if (a is SphereCollider sphereA && b is SphereCollider sphereB)
        {
            return TryCollide(sphereA, sphereB, out manifold);
        }

        if (a is BoxCollider box && b is SphereCollider sphere)
        {
            return TryCollide(box, sphere, out manifold);
        }

        if (a is SphereCollider sphereFirst && b is BoxCollider boxSecond)
        {
            bool collided = TryCollide(boxSecond, sphereFirst, out manifold);
            if (collided)
            {
                manifold = new CollisionManifold(-manifold.Normal, manifold.Penetration);
            }
            return collided;
        }

        manifold = default;
        return false;
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

    private static bool TryCollide(SphereCollider a, SphereCollider b, out CollisionManifold manifold)
    {
        Vector3 centerA = a.Body.Position + a.Offset;
        Vector3 centerB = b.Body.Position + b.Offset;
        Vector3 delta = centerB - centerA;

        float radiusSum = a.Radius + b.Radius;
        float distSq = delta.LengthSquared();
        float radiusSq = radiusSum * radiusSum;

        if (distSq >= radiusSq)
        {
            manifold = default;
            return false;
        }

        if (distSq <= 1e-10f)
        {
            manifold = new CollisionManifold(Vector3.UnitY, a.Radius);
            return true;
        }

        float dist = MathF.Sqrt(distSq);
        Vector3 normal = delta / dist;
        float penetration = radiusSum - dist;
        manifold = new CollisionManifold(normal, penetration);
        return true;
    }

    private static bool TryCollide(BoxCollider box, SphereCollider sphere, out CollisionManifold manifold)
    {
        Vector3 boxCenter = box.Body.Position + box.Offset;
        Vector3 sphereCenter = sphere.Body.Position + sphere.Offset;
        Vector3 local = sphereCenter - boxCenter;

        Vector3 clamped = new(
            Math.Clamp(local.X, -box.HalfExtents.X, box.HalfExtents.X),
            Math.Clamp(local.Y, -box.HalfExtents.Y, box.HalfExtents.Y),
            Math.Clamp(local.Z, -box.HalfExtents.Z, box.HalfExtents.Z)
        );

        Vector3 closest = boxCenter + clamped;
        Vector3 delta = sphereCenter - closest;
        float distSq = delta.LengthSquared();
        float radiusSq = sphere.Radius * sphere.Radius;

        if (distSq > radiusSq)
        {
            manifold = default;
            return false;
        }

        if (distSq > 1e-10f)
        {
            float dist = MathF.Sqrt(distSq);
            Vector3 normal = delta / dist;
            float penetration = sphere.Radius - dist;
            manifold = new CollisionManifold(normal, penetration);
            return true;
        }

        float dx = box.HalfExtents.X - MathF.Abs(local.X);
        float dy = box.HalfExtents.Y - MathF.Abs(local.Y);
        float dz = box.HalfExtents.Z - MathF.Abs(local.Z);

        if (dx <= dy && dx <= dz)
        {
            float sign = local.X >= 0f ? 1f : -1f;
            manifold = new CollisionManifold(new Vector3(sign, 0f, 0f), sphere.Radius + dx);
            return true;
        }

        if (dy <= dz)
        {
            float sign = local.Y >= 0f ? 1f : -1f;
            manifold = new CollisionManifold(new Vector3(0f, sign, 0f), sphere.Radius + dy);
            return true;
        }

        {
            float sign = local.Z >= 0f ? 1f : -1f;
            manifold = new CollisionManifold(new Vector3(0f, 0f, sign), sphere.Radius + dz);
            return true;
        }
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

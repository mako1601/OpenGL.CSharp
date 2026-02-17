using System.Numerics;
using Engine.Physics.Colliders;

namespace Engine.Physics;

public sealed class PhysicsWorld
{
    private const int SolverIterations = 12;
    private const float PositionalCorrectionPercent = 0.95f;
    private const float PositionalCorrectionSlop = 0.0005f;
    private const float Epsilon = 1e-10f;

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
            if (body.InverseMass <= 0f) continue;

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

        if (a is CapsuleCollider capsuleA && b is SphereCollider sphereCapsuleB)
        {
            return TryCollide(capsuleA, sphereCapsuleB, out manifold);
        }

        if (a is SphereCollider sphereC && b is CapsuleCollider capsuleD)
        {
            bool collided = TryCollide(capsuleD, sphereC, out manifold);
            if (collided)
            {
                manifold = new CollisionManifold(-manifold.Normal, manifold.Penetration);
            }
            return collided;
        }

        if (a is BoxCollider boxE && b is CapsuleCollider capsuleF)
        {
            return TryCollide(boxE, capsuleF, out manifold);
        }

        if (a is CapsuleCollider capsuleG && b is BoxCollider boxH)
        {
            bool collided = TryCollide(boxH, capsuleG, out manifold);
            if (collided)
            {
                manifold = new CollisionManifold(-manifold.Normal, manifold.Penetration);
            }
            return collided;
        }

        if (a is CapsuleCollider capsuleI && b is CapsuleCollider capsuleJ)
        {
            return TryCollide(capsuleI, capsuleJ, out manifold);
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

        if (distSq <= Epsilon)
        {
            manifold = new CollisionManifold(Vector3.UnitY, radiusSum);
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

        if (distSq > Epsilon)
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

    private static bool TryCollide(CapsuleCollider capsule, SphereCollider sphere, out CollisionManifold manifold)
    {
        Vector3 sphereCenter = sphere.Body.Position + sphere.Offset;
        Vector3 capsulePoint = ClosestPointOnSegment(capsule.SegmentStart, capsule.SegmentEnd, sphereCenter);
        Vector3 delta = sphereCenter - capsulePoint;

        float radiusSum = capsule.Radius + sphere.Radius;
        float distSq = delta.LengthSquared();
        float radiusSq = radiusSum * radiusSum;

        if (distSq >= radiusSq)
        {
            manifold = default;
            return false;
        }

        if (distSq <= Epsilon)
        {
            manifold = new CollisionManifold(Vector3.UnitY, radiusSum);
            return true;
        }

        float dist = MathF.Sqrt(distSq);
        Vector3 normal = delta / dist;
        float penetration = radiusSum - dist;
        manifold = new CollisionManifold(normal, penetration);
        return true;
    }

    private static bool TryCollide(BoxCollider box, CapsuleCollider capsule, out CollisionManifold manifold)
    {
        Aabb aabb = box.GetAabb();
        Vector3 segmentPoint = FindClosestPointOnSegmentToAabb(capsule.SegmentStart, capsule.SegmentEnd, aabb);
        Vector3 closestOnBox = ClosestPointOnAabb(segmentPoint, aabb);
        Vector3 delta = segmentPoint - closestOnBox;
        float distSq = delta.LengthSquared();
        float radiusSq = capsule.Radius * capsule.Radius;

        if (distSq > radiusSq)
        {
            manifold = default;
            return false;
        }

        if (distSq > Epsilon)
        {
            float dist = MathF.Sqrt(distSq);
            Vector3 normal = delta / dist;
            float penetration = capsule.Radius - dist;
            manifold = new CollisionManifold(normal, penetration);
            return true;
        }

        Vector3 boxCenter = aabb.Center;
        Vector3 local = segmentPoint - boxCenter;
        Vector3 halfExtents = aabb.HalfExtents;

        float dx = halfExtents.X - MathF.Abs(local.X);
        float dy = halfExtents.Y - MathF.Abs(local.Y);
        float dz = halfExtents.Z - MathF.Abs(local.Z);

        if (dx <= dy && dx <= dz)
        {
            float sign = local.X >= 0f ? 1f : -1f;
            manifold = new CollisionManifold(new Vector3(sign, 0f, 0f), capsule.Radius + dx);
            return true;
        }

        if (dy <= dz)
        {
            float sign = local.Y >= 0f ? 1f : -1f;
            manifold = new CollisionManifold(new Vector3(0f, sign, 0f), capsule.Radius + dy);
            return true;
        }

        {
            float sign = local.Z >= 0f ? 1f : -1f;
            manifold = new CollisionManifold(new Vector3(0f, 0f, sign), capsule.Radius + dz);
            return true;
        }
    }

    private static bool TryCollide(CapsuleCollider a, CapsuleCollider b, out CollisionManifold manifold)
    {
        ClosestPointsOnSegments(
            a.SegmentStart,
            a.SegmentEnd,
            b.SegmentStart,
            b.SegmentEnd,
            out Vector3 pointA,
            out Vector3 pointB
        );

        Vector3 delta = pointB - pointA;
        float radiusSum = a.Radius + b.Radius;
        float distSq = delta.LengthSquared();
        float radiusSq = radiusSum * radiusSum;

        if (distSq >= radiusSq)
        {
            manifold = default;
            return false;
        }

        if (distSq <= Epsilon)
        {
            manifold = new CollisionManifold(Vector3.UnitY, radiusSum);
            return true;
        }

        float dist = MathF.Sqrt(distSq);
        Vector3 normal = delta / dist;
        float penetration = radiusSum - dist;
        manifold = new CollisionManifold(normal, penetration);
        return true;
    }

    private static Vector3 ClosestPointOnSegment(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 ab = end - start;
        float abLenSq = ab.LengthSquared();
        if (abLenSq <= Epsilon)
        {
            return start;
        }

        float t = Vector3.Dot(point - start, ab) / abLenSq;
        t = Math.Clamp(t, 0f, 1f);
        return start + ab * t;
    }

    private static Vector3 ClosestPointOnAabb(Vector3 point, Aabb aabb)
    {
        return new Vector3(
            Math.Clamp(point.X, aabb.Min.X, aabb.Max.X),
            Math.Clamp(point.Y, aabb.Min.Y, aabb.Max.Y),
            Math.Clamp(point.Z, aabb.Min.Z, aabb.Max.Z)
        );
    }

    private static Vector3 FindClosestPointOnSegmentToAabb(Vector3 start, Vector3 end, Aabb aabb)
    {
        Vector3 direction = end - start;
        var breakpoints = new List<float>(8) { 0f, 1f };

        AddBreakpoint(aabb.Min.X, start.X, direction.X, breakpoints);
        AddBreakpoint(aabb.Max.X, start.X, direction.X, breakpoints);
        AddBreakpoint(aabb.Min.Y, start.Y, direction.Y, breakpoints);
        AddBreakpoint(aabb.Max.Y, start.Y, direction.Y, breakpoints);
        AddBreakpoint(aabb.Min.Z, start.Z, direction.Z, breakpoints);
        AddBreakpoint(aabb.Max.Z, start.Z, direction.Z, breakpoints);

        breakpoints.Sort();
        int write = 1;
        for (int i = 1; i < breakpoints.Count; i++)
        {
            if (breakpoints[i] - breakpoints[write - 1] > 1e-6f)
            {
                breakpoints[write++] = breakpoints[i];
            }
        }
        breakpoints.RemoveRange(write, breakpoints.Count - write);

        float bestT = 0f;
        float bestDistSq = float.MaxValue;

        for (int i = 0; i < breakpoints.Count - 1; i++)
        {
            float t0 = breakpoints[i];
            float t1 = breakpoints[i + 1];
            float midT = 0.5f * (t0 + t1);
            Vector3 pMid = start + direction * midT;

            float q2 = 0f;
            float q1 = 0f;

            AccumulateAxis(aabb.Min.X, aabb.Max.X, start.X, direction.X, pMid.X, ref q2, ref q1);
            AccumulateAxis(aabb.Min.Y, aabb.Max.Y, start.Y, direction.Y, pMid.Y, ref q2, ref q1);
            AccumulateAxis(aabb.Min.Z, aabb.Max.Z, start.Z, direction.Z, pMid.Z, ref q2, ref q1);

            EvaluateCandidate(t0, start, direction, aabb, ref bestDistSq, ref bestT);
            EvaluateCandidate(t1, start, direction, aabb, ref bestDistSq, ref bestT);

            if (q2 > Epsilon)
            {
                float tOpt = -q1 / (2f * q2);
                if (tOpt >= t0 && tOpt <= t1)
                {
                    EvaluateCandidate(tOpt, start, direction, aabb, ref bestDistSq, ref bestT);
                }
            }
        }

        return start + direction * bestT;
    }

    private static void AddBreakpoint(float bound, float start, float dir, List<float> breakpoints)
    {
        if (MathF.Abs(dir) <= Epsilon) return;

        float t = (bound - start) / dir;
        if (t > 0f && t < 1f)
        {
            breakpoints.Add(t);
        }
    }

    private static void AccumulateAxis(
        float min,
        float max,
        float start,
        float dir,
        float mid,
        ref float q2,
        ref float q1
    )
    {
        if (mid < min)
        {
            float c = start - min;
            q2 += dir * dir;
            q1 += 2f * dir * c;
            return;
        }

        if (mid > max)
        {
            float c = start - max;
            q2 += dir * dir;
            q1 += 2f * dir * c;
        }
    }

    private static void EvaluateCandidate(
        float t,
        Vector3 start,
        Vector3 direction,
        Aabb aabb,
        ref float bestDistSq,
        ref float bestT
    )
    {
        Vector3 point = start + direction * t;
        Vector3 clamped = ClosestPointOnAabb(point, aabb);
        float distSq = Vector3.DistanceSquared(point, clamped);
        if (distSq < bestDistSq)
        {
            bestDistSq = distSq;
            bestT = t;
        }
    }

    private static void ClosestPointsOnSegments(
        Vector3 p1,
        Vector3 q1,
        Vector3 p2,
        Vector3 q2,
        out Vector3 c1,
        out Vector3 c2
    )
    {
        const float eps = Epsilon;

        Vector3 d1 = q1 - p1;
        Vector3 d2 = q2 - p2;
        Vector3 r = p1 - p2;
        float a = Vector3.Dot(d1, d1);
        float e = Vector3.Dot(d2, d2);
        float f = Vector3.Dot(d2, r);

        float s;
        float t;

        if (a <= eps && e <= eps)
        {
            c1 = p1;
            c2 = p2;
            return;
        }

        if (a <= eps)
        {
            s = 0f;
            t = Math.Clamp(f / e, 0f, 1f);
        }
        else
        {
            float c = Vector3.Dot(d1, r);
            if (e <= eps)
            {
                t = 0f;
                s = Math.Clamp(-c / a, 0f, 1f);
            }
            else
            {
                float b = Vector3.Dot(d1, d2);
                float denom = a * e - b * b;

                s = denom > eps
                    ? Math.Clamp((b * f - c * e) / denom, 0f, 1f)
                    : 0f;

                t = (b * s + f) / e;
                if (t < 0f)
                {
                    t = 0f;
                    s = Math.Clamp(-c / a, 0f, 1f);
                }
                else if (t > 1f)
                {
                    t = 1f;
                    s = Math.Clamp((b - c) / a, 0f, 1f);
                }
            }
        }

        c1 = p1 + d1 * s;
        c2 = p2 + d2 * t;
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

            if (invMassA > 0f)
            {
                a.Velocity -= impulse * invMassA;
            }

            if (invMassB > 0f)
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

                if (invMassA > 0f)
                {
                    a.Velocity -= frictionImpulse * invMassA;
                }

                if (invMassB > 0f)
                {
                    b.Velocity += frictionImpulse * invMassB;
                }
            }
        }

        float correctionMagnitude = MathF.Max(manifold.Penetration - PositionalCorrectionSlop, 0f)
            / invMassSum
            * PositionalCorrectionPercent;
        Vector3 correction = correctionMagnitude * manifold.Normal;

        if (invMassA > 0f)
        {
            a.Position -= correction * invMassA;
            if (manifold.Normal.Y < -0.5f)
            {
                a.IsGrounded = true;
            }
        }

        if (invMassB > 0f)
        {
            b.Position += correction * invMassB;
            if (manifold.Normal.Y > 0.5f)
            {
                b.IsGrounded = true;
            }
        }
    }
}

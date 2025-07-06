using System.Numerics;
using System.Runtime.CompilerServices;
using Engine.Physics.Colliders;
using Engine.Physics.Utilities;

namespace Engine.Physics.Core;

/// <summary>
/// Represents the physics world where physical objects interact with each other.
/// </summary>
public class PhysicsWorld
{
    /// <summary>
    /// The maximum number of iterations used during collision resolution.
    /// </summary>
    private const int MAX_ITERATIONS = 10;

    private const float MAX_SIMULATION_DISTANCE = 50f;
    private const float MAX_SIMULATION_DISTANCE_SQ = MAX_SIMULATION_DISTANCE * MAX_SIMULATION_DISTANCE;

    // pre-allocated arrays to avoid allocations during collision detection
    private static readonly PhysicsObject[] s_dynamicObjects = new PhysicsObject[1024];
    private static readonly PhysicsObject[] s_staticObjects = new PhysicsObject[1024];

    private const float EPSILON = 0.0001f;
    private const float EPSILON_SQ = EPSILON * EPSILON;

    /// <summary>
    /// The list of all physics objects currently in the physics world.
    /// </summary>
    private readonly List<PhysicsObject> _objects = [];

    /// <summary>
    /// Adds a physics object to the physics world.
    /// </summary>
    /// <param name="obj">The physics object to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddObject(PhysicsObject obj) => _objects.Add(obj);

    /// <summary>
    /// Removes a physics object from the physics world.
    /// </summary>
    /// <param name="obj">The physics object to remove.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveObject(PhysicsObject obj) => _objects.Remove(obj);

    /// <summary>
    /// Updates the physics world with the given time step.
    /// Includes multiple substeps for increased stability and early filtering for static objects.
    /// </summary>
    /// <param name="deltaTime">The time step for updating the physics world.</param>
    /// <param name="cameraPosition">The camera position for distance culling.</param>
    public void Update(float deltaTime, Vector3 cameraPosition)
    {
        const int substeps = 4;
        float substepDelta = deltaTime / substeps;

        for (int i = 0; i < substeps; i++)
        {
            UpdateVelocities(substepDelta, cameraPosition);
            UpdatePositions(substepDelta, cameraPosition);
            ResolveCollisions(cameraPosition);
        }
    }

    /// <summary>
    /// Updates velocities of all physics objects based on forces, gravity, and damping.
    /// </summary>
    /// <param name="deltaTime">The time step for velocity integration.</param>
    /// <param name="cameraPosition">The camera position for distance culling.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateVelocities(float deltaTime, Vector3 cameraPosition)
    {
        foreach (var obj in _objects)
        {
            var rb = obj.Rigidbody;

            // skip static objects
            if (IsStaticObject(rb)) continue;

            // distance culling using squared distance to avoid sqrt
            var distanceSq = Vector3.DistanceSquared(obj.Transform.Position, cameraPosition);
            if (distanceSq > MAX_SIMULATION_DISTANCE_SQ) continue;

            // apply gravity if enabled
            if (rb.UseGravity)
            {
                rb.Velocity += rb.Gravity * deltaTime;
            }

            // apply linear and angular forces
            float inverseMassDelta = rb.InverseMass * deltaTime;
            rb.Velocity += rb.Force * inverseMassDelta;
            rb.AngularVelocity += rb.Torque * inverseMassDelta;

            // apply damping using fast power approximation
            float linearDamp = MathF.Pow(1f - rb.LinearDamping, deltaTime);
            float angularDamp = MathF.Pow(1f - rb.AngularDamping, deltaTime);
            rb.Velocity *= linearDamp;
            rb.AngularVelocity *= angularDamp;

            // avoid unnecessary updates for very small velocities
            if (rb.Velocity.LengthSquared() < 0.0001f)
            {
                rb.Velocity = Vector3.Zero;
            }

            if (rb.AngularVelocity.LengthSquared() < EPSILON_SQ)
            {
                rb.AngularVelocity = Vector3.Zero;
            }

            // reset forces and torques after integration
            rb.Force = Vector3.Zero;
            rb.Torque = Vector3.Zero;
        }
    }

    /// <summary>
    /// Updates positions and rotations of objects based on their velocities.
    /// </summary>
    /// <param name="deltaTime">The time step for position integration.</param>
    /// <param name="cameraPosition">The camera position for distance culling.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdatePositions(float deltaTime, Vector3 cameraPosition)
    {
        foreach (var obj in _objects)
        {
            var rb = obj.Rigidbody;

            // skip static objects
            if (IsStaticObject(rb)) continue;

            // distance culling
            var distanceSq = Vector3.DistanceSquared(obj.Transform.Position, cameraPosition);
            if (distanceSq > MAX_SIMULATION_DISTANCE_SQ) continue;

            // integrate position
            obj.Transform.Position += rb.Velocity * deltaTime;

            // integrate rotation only if there's significant angular velocity
            float angularSpeedSq = rb.AngularVelocity.LengthSquared();
            if (angularSpeedSq > EPSILON_SQ)
            {
                float angularSpeed = MathF.Sqrt(angularSpeedSq);
                var normalizedAngularVel = rb.AngularVelocity * (1.0f / angularSpeed); // fast normalization

                Quaternion deltaRotation = Quaternion.CreateFromAxisAngle(
                    normalizedAngularVel,
                    angularSpeed * deltaTime
                );
                obj.Transform.Rotation = Quaternion.Normalize(
                    deltaRotation * obj.Transform.Rotation
                );
            }
        }
    }

    /// <summary>
    /// Resolve collisions between all objects.
    /// </summary>
    /// <param name="cameraPosition">The camera position for distance culling.</param>
    private void ResolveCollisions(Vector3 cameraPosition)
    {
        // separate objects into dynamic and static using pre-allocated buffers
        int dynamicCount = 0;
        int staticCount = 0;

        foreach (var obj in _objects)
        {
            var distanceSq = Vector3.DistanceSquared(obj.Transform.Position, cameraPosition);
            if (distanceSq > MAX_SIMULATION_DISTANCE_SQ) continue;

            if (IsStaticObject(obj.Rigidbody))
            {
                if (staticCount < s_staticObjects.Length)
                {
                    s_staticObjects[staticCount++] = obj;
                }
            }
            else
            {
                if (dynamicCount < s_dynamicObjects.Length)
                {
                    s_dynamicObjects[dynamicCount++] = obj;
                }
            }
        }

        for (int iteration = 0; iteration < MAX_ITERATIONS; iteration++)
        {
            bool hadCollision = false;

            // Dynamic vs Dynamic collisions
            for (int i = 0; i < dynamicCount; i++)
            {
                var objA = s_dynamicObjects[i];
                for (int j = i + 1; j < dynamicCount; j++)
                {
                    var objB = s_dynamicObjects[j];

                    if (CheckCollision(objA, objB, out Vector3 normal, out float penetrationDepth))
                    {
                        ResolveCollision(objA, objB, normal, penetrationDepth);
                        hadCollision = true;
                        objA.CurrentColor = new Vector4(1f, 0f, 0f, 1f);
                        objB.CurrentColor = new Vector4(1f, 0f, 0f, 1f);
                    }
                }
            }

            // Dynamic vs Static collisions (only dynamic objects get affected)
            for (int i = 0; i < dynamicCount; i++)
            {
                var dynamicObj = s_dynamicObjects[i];
                for (int j = 0; j < staticCount; j++)
                {
                    var staticObj = s_staticObjects[j];

                    if (CheckCollision(dynamicObj, staticObj, out Vector3 normal, out float penetrationDepth))
                    {
                        // only resolve collision for the dynamic object
                        ResolveStaticCollision(dynamicObj, staticObj, normal, penetrationDepth);
                        hadCollision = true;
                        dynamicObj.CurrentColor = new Vector4(1f, 0f, 0f, 1f);
                    }
                }
            }

            // early exit if no collisions detected
            if (!hadCollision) break;
        }
    }

    /// <summary>
    /// Collision detection.
    /// </summary>
    /// <param name="a">The first physics object.</param>
    /// <param name="b">The second physics object.</param>
    /// <param name="normal">The resulting collision normal if a collision is detected.</param>
    /// <param name="penetrationDepth">The penetration depth of the collision.</param>
    /// <returns>True if a collision is detected; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CheckCollision(
        PhysicsObject a,
        PhysicsObject b,
        out Vector3 normal,
        out float penetrationDepth
    )
    {
        normal = Vector3.Zero;
        penetrationDepth = 0f;

        var colliderA = a.Collider;
        var colliderB = b.Collider;

        if (colliderA == null || colliderB == null) return false;

        return (colliderA, colliderB) switch
        {
            // box collisions
            (BoxCollider boxA, BoxCollider boxB)
                => CollisionHelper.BoxVsBox(boxA, boxB, out normal, out penetrationDepth),

            (BoxCollider box, SphereCollider sphere)
                => CollisionHelper.BoxVsSphere(box, sphere, out normal, out penetrationDepth),

            (BoxCollider box, CapsuleCollider capsule)
                => CollisionHelper.BoxVsCapsule(box, capsule, out normal, out penetrationDepth),

            // sphere collisions
            (SphereCollider sphereA, SphereCollider sphereB)
                => CollisionHelper.SphereVsSphere(sphereA, sphereB, out normal, out penetrationDepth),

            (SphereCollider sphere, BoxCollider box)
                => CollisionHelper.BoxVsSphere(box, sphere, out normal, out penetrationDepth) &&
                   ReverseNormalAndReturnTrue(ref normal),

            (SphereCollider sphere, CapsuleCollider capsule)
                => CollisionHelper.SphereVsCapsule(sphere, capsule, out normal, out penetrationDepth),

            // capsule collisions
            (CapsuleCollider capsuleA, CapsuleCollider capsuleB)
                => CollisionHelper.CapsuleVsCapsule(capsuleA, capsuleB, out normal, out penetrationDepth),

            (CapsuleCollider capsule, BoxCollider box)
                => CollisionHelper.BoxVsCapsule(box, capsule, out normal, out penetrationDepth) &&
                   ReverseNormalAndReturnTrue(ref normal),

            (CapsuleCollider capsule, SphereCollider sphere)
                => CollisionHelper.SphereVsCapsule(sphere, capsule, out normal, out penetrationDepth) &&
                   ReverseNormalAndReturnTrue(ref normal),

            _ => throw new NotImplementedException($"Collision detection not implemented for {colliderA.GetType().Name} vs {colliderB.GetType().Name}"),
        };
    }

    /// <summary>
    /// Resolves collision response between two dynamic objects.
    /// </summary>
    /// <param name="a">The first physics object involved in the collision.</param>
    /// <param name="b">The second physics object involved in the collision.</param>
    /// <param name="normal">The collision normal.</param>
    /// <param name="penetrationDepth">The penetration depth of the collision.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ResolveCollision(
        PhysicsObject a,
        PhysicsObject b,
        Vector3 normal,
        float penetrationDepth
    )
    {
        float invMassA = a.Rigidbody.InverseMass;
        float invMassB = b.Rigidbody.InverseMass;
        float totalInvMass = invMassA + invMassB;

        if (totalInvMass <= 0f) return;

        // positional correction to prevent sinking
        Vector3 separation = normal * (penetrationDepth / totalInvMass);
        a.Transform.Position -= separation * invMassA;
        b.Transform.Position += separation * invMassB;

        Vector3 relativeVelocity = b.Rigidbody.Velocity - a.Rigidbody.Velocity;
        float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal);

        // skip if objects are separating
        if (velocityAlongNormal > 0f) return;

        // calculate impulse scalar
        float restitution = MathF.Min(a.Rigidbody.Restitution, b.Rigidbody.Restitution);
        float impulseScalar = -(1 + restitution) * velocityAlongNormal / totalInvMass;

        Vector3 impulse = impulseScalar * normal;
        a.Rigidbody.Velocity -= impulse * invMassA;
        b.Rigidbody.Velocity += impulse * invMassB;

        // friction impulse
        Vector3 tangent = relativeVelocity - velocityAlongNormal * normal;
        float tangentLengthSq = tangent.LengthSquared();
        if (tangentLengthSq > EPSILON_SQ)
        {
            float invTangentLength = 1.0f / MathF.Sqrt(tangentLengthSq); // fast normalization
            tangent *= invTangentLength;

            float jt = -Vector3.Dot(relativeVelocity, tangent) / totalInvMass;
            float friction = MathF.Sqrt(a.Rigidbody.Friction * b.Rigidbody.Friction);
            jt = Math.Clamp(jt, -impulseScalar * friction, impulseScalar * friction);

            Vector3 frictionImpulse = jt * tangent;
            a.Rigidbody.Velocity -= frictionImpulse * invMassA;
            b.Rigidbody.Velocity += frictionImpulse * invMassB;
        }
    }

    /// <summary>
    /// Resolves collision response between a dynamic and static object.
    /// Only the dynamic object is affected by the collision.
    /// </summary>
    /// <param name="dynamicObj">The dynamic physics object.</param>
    /// <param name="staticObj">The static physics object.</param>
    /// <param name="normal">The collision normal.</param>
    /// <param name="penetrationDepth">The penetration depth of the collision.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ResolveStaticCollision(
        PhysicsObject dynamicObj,
        PhysicsObject staticObj,
        Vector3 normal,
        float penetrationDepth
    )
    {
        float invMass = dynamicObj.Rigidbody.InverseMass;
        if (invMass <= 0f) return;

        // move only the dynamic object out of penetration
        dynamicObj.Transform.Position -= normal * penetrationDepth;

        float velocityAlongNormal = Vector3.Dot(-dynamicObj.Rigidbody.Velocity, normal);

        // skip if object is moving away from static surface
        if (velocityAlongNormal > 0f) return;

        // apply impulse only to dynamic object
        float restitution = MathF.Min(dynamicObj.Rigidbody.Restitution, staticObj.Rigidbody.Restitution);
        float impulseScalar = -(1 + restitution) * velocityAlongNormal;
        dynamicObj.Rigidbody.Velocity -= impulseScalar * normal;

        // apply friction
        Vector3 tangent = dynamicObj.Rigidbody.Velocity - velocityAlongNormal * normal;
        float tangentLengthSq = tangent.LengthSquared();
        if (tangentLengthSq > EPSILON_SQ)
        {
            float invTangentLength = 1.0f / MathF.Sqrt(tangentLengthSq); // fast normalization
            tangent *= invTangentLength;

            float jt = -Vector3.Dot(dynamicObj.Rigidbody.Velocity, tangent);
            float friction = MathF.Sqrt(dynamicObj.Rigidbody.Friction * staticObj.Rigidbody.Friction);
            jt = Math.Clamp(jt, -impulseScalar * friction, impulseScalar * friction);

            dynamicObj.Rigidbody.Velocity += jt * tangent;
        }
    }

    /// <summary>
    /// Fast check if an object is static.
    /// Static objects have UseGravity = false and Mass = 0.
    /// </summary>
    /// <param name="rigidbody">The rigidbody to check.</param>
    /// <returns>True if the object is static, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsStaticObject(Rigidbody rigidbody)
        => !rigidbody.UseGravity && rigidbody.Mass <= 0f;

    /// <summary>
    /// Helper method to reverse the normal vector and return true.
    /// </summary>
    /// <param name="normal">The normal vector to reverse.</param>
    /// <returns>Always returns true.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ReverseNormalAndReturnTrue(ref Vector3 normal)
    {
        normal = -normal;
        return true;
    }
}

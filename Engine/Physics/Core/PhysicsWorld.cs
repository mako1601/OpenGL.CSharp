using System.Numerics;
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

    /// <summary>
    /// The list of all physics objects currently in the physics world.
    /// </summary>
    private readonly List<PhysicsObject> _objects = [];

    /// <summary>
    /// Adds a physics object to the physics world.
    /// </summary>
    /// <param name="obj">The physics object to add.</param>
    public void AddObject(PhysicsObject obj) => _objects.Add(obj);

    /// <summary>
    /// Removes a physics object from the physics world.
    /// </summary>
    /// <param name="obj">The physics object to remove.</param>
    public void RemoveObject(PhysicsObject obj) => _objects.Remove(obj);

    /// <summary>
    /// Updates the physics world with the given time step.
    /// Includes multiple substeps for increased stability.
    /// </summary>
    /// <param name="deltaTime">The time step for updating the physics world.</param>
    public void Update(float deltaTime, Vector3 cameraPosition)
    {
        int substeps = 4;
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
    private void UpdateVelocities(float deltaTime, Vector3 cameraPosition)
    {
        foreach (var obj in _objects)
        {
            if (Vector3.DistanceSquared(obj.Transform.Position, cameraPosition) > MAX_SIMULATION_DISTANCE * MAX_SIMULATION_DISTANCE) continue;

            var rb = obj.Rigidbody;
            if (rb.InverseMass <= 0f) continue;

            // apply gravity if enabled
            if (rb.UseGravity)
            {
                rb.Velocity += rb.Gravity * deltaTime;
            }

            // apply linear and angular forces
            float inverseMassDelta = rb.InverseMass * deltaTime;
            rb.Velocity += rb.Force * inverseMassDelta;
            rb.AngularVelocity += rb.Torque * inverseMassDelta;

            // apply damping
            float linearDamp = MathF.Pow(1f - rb.LinearDamping, deltaTime);
            float angularDamp = MathF.Pow(1f - rb.AngularDamping, deltaTime);
            rb.Velocity *= linearDamp;
            rb.AngularVelocity *= angularDamp;

            // avoid unnecessary updates for very small velocities
            if (rb.Velocity.LengthSquared() < 0.0001f)
            {
                rb.Velocity = Vector3.Zero;
            }

            if (rb.AngularVelocity.LengthSquared() < 0.0001f)
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
    private void UpdatePositions(float deltaTime, Vector3 cameraPosition)
    {
        foreach (var obj in _objects)
        {
            if (Vector3.DistanceSquared(obj.Transform.Position, cameraPosition) > MAX_SIMULATION_DISTANCE * MAX_SIMULATION_DISTANCE) continue;

            var rb = obj.Rigidbody;
            if (rb.InverseMass <= 0f) continue;

            // integrate position
            obj.Transform.Position += rb.Velocity * deltaTime;

            // integrate rotation
            float angularSpeed = rb.AngularVelocity.Length();
            if (angularSpeed > 0f)
            {
                Quaternion deltaRotation = Quaternion.CreateFromAxisAngle(
                    Vector3.Normalize(rb.AngularVelocity),
                    angularSpeed * deltaTime
                );
                obj.Transform.Rotation = Quaternion.Normalize(
                    deltaRotation * obj.Transform.Rotation
                );
            }
        }
    }

    /// <summary>
    /// Resolves collisions between all objects in the scene with sequential impulse resolution.
    /// </summary>
    private void ResolveCollisions(Vector3 cameraPosition)
    {
        for (int iteration = 0; iteration < MAX_ITERATIONS; iteration++)
        {
            bool hadCollision = false;

            for (int i = 0; i < _objects.Count; i++)
            {
                var objA = _objects[i];
                if (Vector3.DistanceSquared(objA.Transform.Position, cameraPosition) > MAX_SIMULATION_DISTANCE * MAX_SIMULATION_DISTANCE) continue;

                for (int j = i + 1; j < _objects.Count; j++)
                {
                    var objB = _objects[j];
                    if (Vector3.DistanceSquared(objB.Transform.Position, cameraPosition) > MAX_SIMULATION_DISTANCE * MAX_SIMULATION_DISTANCE) continue;

                    if (CheckCollision(objA, objB, out Vector3 normal, out float penetrationDepth))
                    {
                        ResolveCollision(objA, objB, normal, penetrationDepth);
                        hadCollision = true;
                        objA.CurrentColor = new Vector4(1f, 0f, 0f, 1f);
                        objB.CurrentColor = new Vector4(1f, 0f, 0f, 1f);
                    }
                }
            }

            // exit early if no collisions detected
            if (!hadCollision) break;
        }
    }

    /// <summary>
    /// Checks for a collision between two physics objects and outputs the collision normal and penetration depth.
    /// </summary>
    /// <param name="a">The first physics object.</param>
    /// <param name="b">The second physics object.</param>
    /// <param name="normal">The resulting collision normal if a collision is detected.</param>
    /// <param name="penetrationDepth">The penetration depth of the collision.</param>
    /// <returns>True if a collision is detected; otherwise, false.</returns>
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
    /// Resolves collision response between two physics objects using impulses.
    /// </summary>
    /// <param name="a">The first physics object involved in the collision.</param>
    /// <param name="b">The second physics object involved in the collision.</param>
    /// <param name="normal">The collision normal.</param>
    /// <param name="penetrationDepth">The penetration depth of the collision.</param>
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
        if (tangent.LengthSquared() > 0.0001f)
        {
            tangent = Vector3.Normalize(tangent);
            float jt = -Vector3.Dot(relativeVelocity, tangent) / totalInvMass;

            float friction = MathF.Sqrt(a.Rigidbody.Friction * b.Rigidbody.Friction);
            jt = Math.Clamp(jt, -impulseScalar * friction, impulseScalar * friction);

            Vector3 frictionImpulse = jt * tangent;
            a.Rigidbody.Velocity -= frictionImpulse * invMassA;
            b.Rigidbody.Velocity += frictionImpulse * invMassB;
        }
    }

    /// <summary>
    /// Helper method to reverse the normal vector and return true.
    /// </summary>
    /// <param name="normal">The normal vector to reverse.</param>
    /// <returns>Always returns true.</returns>
    private static bool ReverseNormalAndReturnTrue(ref Vector3 normal)
    {
        normal = -normal;
        return true;
    }
}

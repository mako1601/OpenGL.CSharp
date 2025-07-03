using System.Numerics;

namespace Engine.Physics.Core;

/// <summary>
/// Represents the physical properties of an object, such as mass, velocity, and forces.
/// </summary>
public class Rigidbody
{
    private float _mass = 1f;
    private float _restitution = 0.5f;
    private float _friction = 0.5f;
    private float _linearDamping = 0.01f;
    private float _angularDamping = 0.01f;

    /// <summary>
    /// Gets or sets the mass of the object. Must be non-negative.
    /// Default value is <c>1.0f</c>.
    /// </summary>
    public float Mass
    {
        get => _mass;
        set => _mass = value < 0f
            ? throw new ArgumentException($"Mass must be non-negative. Provided: {value}")
            : value;
    }

    /// <summary>
    /// Gets the inverse mass of the object. If mass is zero, inverse mass is zero.
    /// </summary>
    public float InverseMass => Mass > 0f ? 1f / Mass : 0f;

    /// <summary>
    /// Gets or sets the linear velocity of the object.
    /// </summary>
    public Vector3 Velocity { get; set; }

    /// <summary>
    /// Gets or sets the angular velocity of the object.
    /// </summary>
    public Vector3 AngularVelocity { get; set; }

    /// <summary>
    /// This is reset after each physics update.
    /// </summary>
    public Vector3 Force { get; internal set; }

    /// <summary>
    /// This is reset after each physics update.
    /// </summary>
    public Vector3 Torque { get; internal set; }

    /// <summary>
    /// Gets or sets the coefficient of restitution (bounciness) of the object.
    /// Should be between 0 (inelastic collision) and 1 (perfectly elastic collision).
    /// Default value is <c>0.5f</c>.
    /// </summary>
    public float Restitution
    {
        get => _restitution;
        set => _restitution = value < 0f
            ? 0f
            : value > 1f
                ? 1f
                : value;
    }

    /// <summary>
    /// Gets or sets the coefficient of friction of the object.
    /// Friction cannot be negative. Default value is <c>0.5f</c>.
    /// </summary>
    public float Friction
    {
        get => _friction;
        set => _friction = value < 0f
            ? 0f
            : value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the object is affected by gravity.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool UseGravity { get; set; } = true;

    /// <summary>
    /// Gets or sets the gravity vector to apply to the object.
    /// Default value is <c>(0, -9.81f, 0)</c>.
    /// </summary>
    public Vector3 Gravity { get; set; } = new Vector3(0, -9.81f, 0);

    /// <summary>
    /// Gets or sets the linear damping factor, which reduces linear velocity over time.
    /// Should be between 0 (no damping) and 1 (full damping). Default value is <c>0.01f</c>.
    /// </summary>
    public float LinearDamping
    {
        get => _linearDamping;
        set => _linearDamping = value < 0f
            ? 0f
            : value > 1f
                ? 1f
                : value;
    }

    /// <summary>
    /// Gets or sets the angular damping factor, which reduces angular velocity over time.
    /// Should be between 0 (no damping) and 1 (full damping). Default value is <c>0.01f</c>.
    /// </summary>
    public float AngularDamping
    {
        get => _angularDamping;
        set => _angularDamping = value < 0f
            ? 0f
            : value > 1f
                ? 1f
                : value;
    }

    /// <summary>
    /// Applies an instantaneous force to the rigidbody.
    /// </summary>
    /// <param name="force">The force vector to apply.</param>
    public void AddForce(Vector3 force)
    {
        Force += force;
    }

    /// <summary>
    /// Applies an instantaneous torque to the rigidbody.
    /// </summary>
    /// <param name="torque">The torque vector to apply.</param>
    public void AddTorque(Vector3 torque)
    {
        Torque += torque;
    }
}

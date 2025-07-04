using System.Numerics;

namespace Engine.Physics.Utilities;

/// <summary>
/// Represents the transformation of an object in space, including position, rotation, and scale.
/// </summary>
public class Transform
{
    private Vector3 _scale = Vector3.One;

    /// <summary>
    /// Gets or sets the scale of the object.
    /// The scale must be positive and finite in all dimensions; otherwise, an exception is thrown.
    /// </summary>
    public Vector3 Scale
    {
        get => _scale;
        set => _scale = value.X <= 0 || value.Y <= 0 || value.Z <= 0 ||
                        float.IsNaN(value.X) || float.IsInfinity(value.X) ||
                        float.IsNaN(value.Y) || float.IsInfinity(value.Y) ||
                        float.IsNaN(value.Z) || float.IsInfinity(value.Z)
            ? throw new ArgumentException($"Scale must be positive finite number in all dimensions. Provided: {value}")
            : value;
    }

    /// <summary>
    /// Gets or sets the position of the object in world space.
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// Gets or sets the rotation of the object represented by a quaternion.
    /// </summary>
    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    /// <summary>
    /// Gets the full world matrix representing the object's transformation,
    /// including scale, rotation, and translation.
    /// </summary>
    public Matrix4x4 WorldMatrix
        => Matrix4x4.CreateScale(Scale) *
           Matrix4x4.CreateFromQuaternion(Rotation) *
           Matrix4x4.CreateTranslation(Position);
}

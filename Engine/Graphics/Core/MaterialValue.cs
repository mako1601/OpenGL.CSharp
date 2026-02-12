using System.Numerics;
using Silk.NET.Maths;

namespace Engine.Graphics;

/// <summary>
/// Universal value container for a material uniform parameter.
/// Supports implicit conversions from common shader data types.
/// </summary>
public readonly record struct MaterialValue(object Value)
{
    public static implicit operator MaterialValue(bool value) => new(value);
    public static implicit operator MaterialValue(int value) => new(value);
    public static implicit operator MaterialValue(float value) => new(value);
    public static implicit operator MaterialValue(Vector2 value) => new(value);
    public static implicit operator MaterialValue(Vector3 value) => new(value);
    public static implicit operator MaterialValue(Vector4 value) => new(value);
    public static implicit operator MaterialValue(Matrix4x4 value) => new(value);
    public static implicit operator MaterialValue(Matrix4X4<float> value) => new(value);
}

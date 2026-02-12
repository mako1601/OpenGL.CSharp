using System.Numerics;

namespace Engine.Graphics;

/// <summary>
/// DTO describing a material loaded from JSON.
/// Contains shader paths, textures, uniform constants, semantics, and properties.
/// </summary>
public sealed class MaterialDefinition
{
    public ShaderDefinition Shader { get; set; }
    public List<TextureDefinition> Textures { get; set; }
    public Dictionary<string, bool> Bools { get; set; }
    public Dictionary<string, int> Ints { get; set; }
    public Dictionary<string, float> Floats { get; set; }
    public Dictionary<string, Vector2> Vector2s { get; set; }
    public Dictionary<string, Vector3> Vector3s { get; set; }
    public Dictionary<string, Vector4> Vector4s { get; set; }
    public Dictionary<string, Matrix4x4> Matrix4s { get; set; }
    public Dictionary<string, string> Semantics { get; set; }
    public Dictionary<string, string> Properties { get; set; }
}

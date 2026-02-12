namespace Engine.Graphics;

/// <summary>
/// DTO describing vertex and fragment shader paths in a JSON material.
/// </summary>
public sealed class ShaderDefinition
{
    public string Vertex { get; set; } = string.Empty;
    public string Fragment { get; set; } = string.Empty;
}

namespace Engine.Graphics;

/// <summary>
/// DTO describing a material texture slot in JSON:
/// sampler uniform name, texture file, and slot index.
/// </summary>
public sealed class TextureDefinition
{
    public string Uniform { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int Slot { get; set; }
}

using Silk.NET.OpenGL;

namespace Engine.Geometry;

/// <summary>
/// Configuration options for mesh primitive generation.
/// </summary>
public class MeshPrimitiveConfig
{
    /// <summary>
    /// Include normal vectors in the mesh (required for lighting).
    /// </summary>
    public bool HasNormals { get; set; } = true;

    /// <summary>
    /// Include UV (texture coordinates) in the mesh.
    /// </summary>
    public bool HasUV { get; set; } = true;

    /// <summary>
    /// Include tangent and bitangent vectors for normal mapping.
    /// </summary>
    public bool HasNormalMap { get; set; } = true;

    /// <summary>
    /// Stretch texture coordinates to fill the entire [0,1] range.
    /// If false, texture coordinates are scaled based on geometry dimensions.
    /// </summary>
    public bool StretchTexture { get; set; } = true;

    /// <summary>
    /// Primitive type for rendering (Triangles, Lines, etc.).
    /// </summary>
    public PrimitiveType PrimitiveType { get; set; } = PrimitiveType.Triangles;

    /// <summary>
    /// Number of slices for curved primitives (Sphere, Cylinder, Cone).
    /// </summary>
    public ushort Slices { get; set; } = 16;

    /// <summary>
    /// Number of stacks for curved primitives (Sphere, Cylinder, Cone).
    /// </summary>
    public ushort Stacks { get; set; } = 16;
}

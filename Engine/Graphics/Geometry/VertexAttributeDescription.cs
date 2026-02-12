using Silk.NET.OpenGL;

namespace Engine.Graphics;

/// <summary>
/// Description of a single vertex attribute (layout(location = X)).
/// </summary>
public readonly record struct VertexAttributeDescription(
    uint Index,
    int Count,
    VertexAttribPointerType Type,
    uint VertexSize,
    int Offset
);

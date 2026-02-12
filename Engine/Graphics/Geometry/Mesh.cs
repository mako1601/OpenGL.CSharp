using Engine.Geometry;
using Silk.NET.OpenGL;

namespace Engine.Graphics;

/// <summary>
/// Wrapper over VBO/EBO/VAO for drawing a primitive.
/// Contains geometry only and does not manage shaders or textures.
/// </summary>
public sealed class Mesh : IDisposable
{
    private readonly MeshPrimitive _primitive;
    private readonly BufferObject<float> _vbo;
    private readonly BufferObject<uint> _ebo;
    private readonly VertexArrayObject<float, uint> _vao;

    public int IndexCount => _primitive.Indices.Length;

    public Mesh(GL gl, MeshPrimitive primitive, params VertexAttributeDescription[] attributes)
    {
        _primitive = primitive ?? throw new ArgumentNullException(nameof(primitive));

        _ebo = new BufferObject<uint>(
            gl,
            _primitive.Indices,
            BufferTargetARB.ElementArrayBuffer,
            BufferUsageARB.StaticDraw
        );

        _vbo = new BufferObject<float>(
            gl,
            _primitive.Vertices,
            BufferTargetARB.ArrayBuffer,
            BufferUsageARB.StaticDraw
        );

        _vao = new VertexArrayObject<float, uint>(gl, _vbo, _ebo);

        foreach (var attr in attributes)
        {
            _vao.VertexAttributePointer(
                attr.Index,
                attr.Count,
                attr.Type,
                attr.VertexSize,
                attr.Offset
            );
        }

        _vao.Unbind();
    }

    public void Bind() => _vao.Bind();

    public void Unbind() => _vao.Unbind();

    public unsafe void Draw(GL gl, PrimitiveType primitiveType = PrimitiveType.Triangles) =>
        gl.DrawElements(primitiveType, (uint)IndexCount, DrawElementsType.UnsignedInt, null);

    public void Dispose()
    {
        _vbo?.Dispose();
        _ebo?.Dispose();
        _vao?.Dispose();
        GC.SuppressFinalize(this);
    }
}

using Silk.NET.OpenGL;

namespace Engine.Graphics;

public sealed class VertexArrayObject<TVertexType, TIndexType> : IDisposable
    where TVertexType : unmanaged
    where TIndexType : unmanaged
{
    private readonly uint _handle;
    private readonly GL _gl;

    public VertexArrayObject(GL gl, BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo)
    {
        _gl = gl;
        _handle = _gl.GenVertexArray();
        _gl.BindVertexArray(_handle);
        vbo?.Bind();
        ebo?.Bind();
    }

    public unsafe void VertexAttributePointer(
        uint index,
        int count,
        VertexAttribPointerType type,
        uint vertexSize,
        int offset
    )
    {
        _gl.VertexAttribPointer(
            index,
            count,
            type,
            false,
            vertexSize * (uint)sizeof(TVertexType),
            (void*)(offset * sizeof(TVertexType))
        );
        _gl.EnableVertexAttribArray(index);
    }

    public void Bind() => _gl.BindVertexArray(_handle);

    public void Unbind() => _gl.BindVertexArray(0);

    public void Dispose()
    {
        _gl.DeleteVertexArray(_handle);
        GC.SuppressFinalize(this);
    }
}

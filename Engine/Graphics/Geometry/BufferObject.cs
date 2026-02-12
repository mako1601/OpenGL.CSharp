using Silk.NET.OpenGL;

namespace Engine.Graphics;

public sealed class BufferObject<TDataType> : IDisposable
    where TDataType : unmanaged
{
    private readonly uint _handle;
    private readonly BufferTargetARB _bufferType;
    private readonly GL _gl;

    public unsafe BufferObject(
        GL gl,
        Span<TDataType> data,
        BufferTargetARB bufferType,
        BufferUsageARB bufferUsage
    )
    {
        _gl = gl;
        _bufferType = bufferType;
        _handle = _gl.GenBuffer();
        _gl.BindBuffer(_bufferType, _handle);
        fixed (void* d = data)
        {
            _gl.BufferData(bufferType, (nuint)(data.Length * sizeof(TDataType)), d, bufferUsage);
        }
    }

    public void Bind() => _gl.BindBuffer(_bufferType, _handle);

    public void Unbind() => _gl.BindBuffer(_bufferType, 0);

    public void Dispose()
    {
        _gl.DeleteBuffer(_handle);
        GC.SuppressFinalize(this);
    }
}

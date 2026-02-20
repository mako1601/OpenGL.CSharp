using Silk.NET.OpenGL;

namespace Engine.Graphics;

public sealed class DepthTexture : IDisposable
{
    private readonly uint _handle;
    private readonly GL _gl;

    public uint Handle => _handle;
    public uint Width { get; }
    public uint Height { get; }

    public unsafe DepthTexture(GL gl, uint width, uint height)
    {
        _gl = gl;
        Width = width;
        Height = height;

        _handle = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _handle);

        _gl.TexImage2D(
            TextureTarget.Texture2D,
            0,
            InternalFormat.DepthComponent,
            width,
            height,
            0,
            PixelFormat.DepthComponent,
            PixelType.Float,
            null
        );

        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToBorder);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToBorder);

        float[] borderColor = [1f, 1f, 1f, 1f];
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderColor);
    }

    public void Bind(uint textureSlot = 0)
    {
        _gl.ActiveTexture((TextureUnit)((uint)TextureUnit.Texture0 + textureSlot));
        _gl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    public void Dispose()
    {
        _gl.DeleteTexture(_handle);
        GC.SuppressFinalize(this);
    }
}

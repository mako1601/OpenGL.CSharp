using Silk.NET.OpenGL;

namespace Engine.Graphics;

public sealed class DepthTextureArray : IDisposable
{
    private readonly GL _gl;
    private readonly uint _handle;

    public uint Handle => _handle;
    public uint Width { get; }
    public uint Height { get; }
    public int Layers { get; }

    public unsafe DepthTextureArray(GL gl, uint width, uint height, int layers)
    {
        if (layers <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(layers), "Layers must be > 0.");
        }

        _gl = gl;
        Width = width;
        Height = height;
        Layers = layers;

        _handle = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2DArray, _handle);
        _gl.TexImage3D(
            TextureTarget.Texture2DArray,
            0,
            InternalFormat.DepthComponent32f,
            width,
            height,
            (uint)layers,
            0,
            PixelFormat.DepthComponent,
            PixelType.Float,
            null
        );

        _gl.TexParameter(
            TextureTarget.Texture2DArray,
            TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Nearest
        );
        _gl.TexParameter(
            TextureTarget.Texture2DArray,
            TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Nearest
        );
        _gl.TexParameter(
            TextureTarget.Texture2DArray,
            TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToBorder
        );
        _gl.TexParameter(
            TextureTarget.Texture2DArray,
            TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToBorder
        );
        _gl.TexParameter(
            TextureTarget.Texture2DArray,
            TextureParameterName.TextureBorderColor,
            [1f, 1f, 1f, 1f]
        );
    }

    public void Bind(uint textureSlot = 0)
    {
        _gl.ActiveTexture((TextureUnit)((uint)TextureUnit.Texture0 + textureSlot));
        _gl.BindTexture(TextureTarget.Texture2DArray, _handle);
    }

    public void Dispose()
    {
        _gl.DeleteTexture(_handle);
        GC.SuppressFinalize(this);
    }
}

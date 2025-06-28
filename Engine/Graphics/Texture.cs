using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Graphics;

public class Texture : IDisposable
{
    private readonly uint _handle;
    private readonly GL _gl;

    public uint TextureSlot { get; private set; }

    public unsafe Texture(
        GL gl,
        string textureFilename,
        bool isGenerateMipmap = true,
        bool isPixelFiltering = true,
        uint textureSlot = 0
    )
    {
        if (textureSlot > 31)
        {
            throw new ArgumentException("textureSlot cannot be larger than 31");
        }

        TextureSlot = textureSlot;
        _gl = gl;
        _handle = _gl.GenTexture();

        try
        {
            Bind((uint)TextureUnit.Texture0 + textureSlot);

            string fullPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Textures", textureFilename);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Texture file not found.", fullPath);
            }

            using var image = Image.Load<Rgba32>(fullPath);
            gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba8,
                (uint)image.Width,
                (uint)image.Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                null
            );

            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    fixed (void* data = accessor.GetRowSpan(y))
                    {
                        gl.TexSubImage2D(
                            TextureTarget.Texture2D,
                            0,
                            0,
                            y,
                            (uint)accessor.Width,
                            1,
                            PixelFormat.Rgba,
                            PixelType.UnsignedByte,
                            data
                        );
                    }
                }
            });

            SetParameters(isGenerateMipmap, isPixelFiltering);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to load texture file '{textureFilename}': {ex.Message}");

            _gl.DeleteTexture(_handle);
            _handle = 0;

            throw;
        }
    }

    public unsafe Texture(
        GL gl,
        Span<byte> data,
        uint width,
        uint height,
        bool isGenerateMipmap = true,
        bool isPixelFiltering = true,
        uint textureSlot = 0
    )
    {
        if (textureSlot > 31)
        {
            throw new ArgumentException("textureSlot cannot be larger than 31");
        }

        TextureSlot = textureSlot;

        _gl = gl;
        _handle = _gl.GenTexture();
        Bind(textureSlot);

        fixed (void* d = &data[0])
        {
            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                (int)InternalFormat.Rgba,
                width,
                height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                d
            );

            SetParameters(isGenerateMipmap, isPixelFiltering);
        }
    }

    private void SetParameters(bool isGenerateMipmap = true, bool isPixelFiltering = true)
    {
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.Repeat
        );
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.Repeat
        );
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            isGenerateMipmap
                ? (int)TextureMinFilter.NearestMipmapLinear
                : (int)TextureMinFilter.Nearest
        );
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            isPixelFiltering ? (int)TextureMagFilter.Nearest : (int)TextureMagFilter.Linear
        );

        if (isGenerateMipmap)
        {
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);
            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }
    }

    public void Bind(uint textureSlot = 0)
    {
        _gl.ActiveTexture((TextureUnit)((uint)TextureUnit.Texture0 + textureSlot));
        _gl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    public void Dispose() => _gl.DeleteTexture(_handle);
}

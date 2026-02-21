using Silk.NET.OpenGL;

namespace Engine.Graphics;

public sealed class FrameBuffer : IDisposable
{
    private readonly uint _handle;
    private readonly GL _gl;

    public FrameBuffer(GL gl)
    {
        _gl = gl;
        _handle = _gl.GenFramebuffer();
    }

    public void Bind(FramebufferTarget target = FramebufferTarget.Framebuffer) =>
        _gl.BindFramebuffer(target, _handle);

    public void AttachTexture2D(
        FramebufferAttachment attachment,
        TextureTarget textureTarget,
        uint textureHandle,
        int level = 0,
        FramebufferTarget target = FramebufferTarget.Framebuffer
    ) =>
        _gl.FramebufferTexture2D(target, attachment, textureTarget, textureHandle, level);

    public void AttachTextureLayer(
        FramebufferAttachment attachment,
        uint textureHandle,
        int layer,
        int level = 0,
        FramebufferTarget target = FramebufferTarget.Framebuffer
    ) =>
        _gl.FramebufferTextureLayer(target, attachment, textureHandle, level, layer);

    public void SetDrawReadBuffers(DrawBufferMode drawBufferMode, ReadBufferMode readBufferMode)
    {
        _gl.DrawBuffer(drawBufferMode);
        _gl.ReadBuffer(readBufferMode);
    }

    public void Validate(FramebufferTarget target = FramebufferTarget.Framebuffer)
    {
        FramebufferStatus status = (FramebufferStatus)_gl.CheckFramebufferStatus(target);
        if (status != FramebufferStatus.Complete)
        {
            throw new InvalidOperationException(
                $"Framebuffer is incomplete. Status: {status}"
            );
        }
    }

    public static void Unbind(GL gl, FramebufferTarget target = FramebufferTarget.Framebuffer) =>
        gl.BindFramebuffer(target, 0);

    public void Dispose()
    {
        _gl.DeleteFramebuffer(_handle);
        GC.SuppressFinalize(this);
    }
}

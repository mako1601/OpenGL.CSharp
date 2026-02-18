using Silk.NET.OpenGL;

namespace Shadows;

public sealed class Scene : IDisposable
{
    public Scene(GL gl)
    {
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);
        gl.FrontFace(FrontFaceDirection.Ccw);
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    public void Draw(GL gl)
    {
        gl.ClearColor(0.05f, 0.06f, 0.08f, 1f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

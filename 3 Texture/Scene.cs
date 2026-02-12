using System.Numerics;
using Engine.Geometry;
using Engine.Graphics;
using Silk.NET.OpenGL;

namespace Texture;

public sealed class Scene : IDisposable
{
    private readonly MeshPrimitive _cube = Cube.Create(Vector3.One, false, true, false, false);
    private readonly Mesh _cubeMesh;
    private readonly ShaderProgram _shader;
    private readonly Engine.Graphics.Texture _texture;

    public Scene(GL gl)
    {
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);
        gl.FrontFace(FrontFaceDirection.Ccw);
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.Blend);
        // gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _cubeMesh = new Mesh(
            gl,
            _cube,
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 5, 0),
            new VertexAttributeDescription(1, 2, VertexAttribPointerType.Float, 5, 3)
        );

        _shader = new ShaderProgram(gl, "texture.glslv", "texture.glslf");
        _texture = new Engine.Graphics.Texture(gl, "brickwall.jpg");
    }

    public void Draw(GL gl, Engine.Camera camera)
    {
        gl.ClearColor(System.Drawing.Color.Wheat);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _cubeMesh.Bind();
        _shader.Use();
        _texture.Bind();

        _shader.SetInt("uTexture", (int)_texture.TextureSlot);
        _shader.SetMatrix4("uModel", Matrix4x4.Identity);
        _shader.SetMatrix4("uView", camera.GetViewMatrix());
        _shader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
        _cubeMesh.Draw(gl);
    }

    public void Dispose()
    {
        _texture?.Dispose();
        _cubeMesh?.Dispose();
        _shader?.Dispose();
        GC.SuppressFinalize(this);
    }
}

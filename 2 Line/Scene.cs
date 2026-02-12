using System.Numerics;
using Engine.Geometry;
using Engine.Graphics;
using Silk.NET.OpenGL;

namespace Line;

public sealed class Scene : IDisposable
{
    private readonly BufferObject<float> _vbo;
    private readonly BufferObject<uint> _ebo;
    private readonly VertexArrayObject<float, uint> _vao;
    private readonly ShaderProgram _shader;
    private readonly MeshPrimitive _cube = Cube.Create(Vector3.One, false, false, false, false, PrimitiveType.Lines);

    public Scene(GL gl)
    {
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);
        gl.FrontFace(FrontFaceDirection.Ccw);
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.Blend);
        // gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        gl.LineWidth(2f);

        _ebo = new BufferObject<uint>(gl, _cube.Indices, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw);
        _vbo = new BufferObject<float>(gl, _cube.Vertices, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        _vao = new VertexArrayObject<float, uint>(gl, _vbo, _ebo);
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);

        _shader = new ShaderProgram(gl, "line.glslv", "line.glslf");
    }

    public unsafe void Draw(GL gl, Engine.Camera camera)
    {
        gl.ClearColor(System.Drawing.Color.Wheat);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _vao.Bind();
        _shader.Use();

        _shader.SetVector4("uColor", 1.0f, 0.5f, 0.8f, 0.5f);
        _shader.SetMatrix4("uModel", Matrix4x4.Identity);
        _shader.SetMatrix4("uView", camera.GetViewMatrix());
        _shader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
        gl.DrawElements(PrimitiveType.Lines, (uint)_cube.Indices.Length, DrawElementsType.UnsignedInt, null);
    }

    public void Dispose()
    {
        _vbo?.Dispose();
        _ebo?.Dispose();
        _vao?.Dispose();
        _shader?.Dispose();
        GC.SuppressFinalize(this);
    }
}

using System.Numerics;
using Engine;
using Engine.Geometry;
using Engine.Graphics;
using Silk.NET.OpenGL;

namespace Color;

public sealed class Scene : IDisposable
{
    private readonly Mesh _cubeMesh;
    private readonly Material _cubeMaterial;
    private readonly MaterialContext _materialContext = new();

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
            Cube.Create(
                Vector3.One,
                new MeshPrimitiveConfig
                {
                    HasNormals = false,
                    HasUV = false,
                    HasNormalMap = false
                }
            ),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 3, 0)
        );

        _cubeMaterial = MaterialLoader.Load(gl, "FlatColor3");
    }

    public void Draw(GL gl, Camera camera)
    {
        gl.ClearColor(System.Drawing.Color.Wheat);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _materialContext.Set("Color", new Vector3(1.0f, 0.5f, 0.2f));
        _materialContext.Set("Model", Matrix4x4.Identity);
        _materialContext.Set("View", camera.GetViewMatrix());
        _materialContext.Set("Projection", camera.GetProjectionMatrix());

        _cubeMesh.Bind();
        _cubeMaterial.Apply(_materialContext);
        _cubeMesh.Draw(gl);
        _cubeMesh.Unbind();
    }

    public void Dispose()
    {
        _cubeMesh?.Dispose();
        _cubeMaterial?.Dispose();
        GC.SuppressFinalize(this);
    }
}

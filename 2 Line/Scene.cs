using System.Numerics;
using Engine.Geometry;
using Engine.Graphics;
using Silk.NET.OpenGL;

namespace Line;

public sealed class Scene : IDisposable
{
    private readonly MeshPrimitive _cube = Cube.Create(
        Vector3.One,
        new MeshPrimitiveConfig 
        {
            HasNormals = false,
            HasUV = false,
            HasNormalMap = false,
            StretchTexture = false,
            PrimitiveType = PrimitiveType.Lines
        }
    );
    private readonly Mesh _cubeMesh;
    private readonly Material _lineMaterial;
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
        gl.LineWidth(2f);

        _cubeMesh = new Mesh(
            gl,
            _cube,
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 3, 0)
        );

        _lineMaterial = MaterialLoader.Load(gl, "LineScene");
    }

    public void Draw(GL gl, Engine.Camera camera)
    {
        gl.ClearColor(System.Drawing.Color.Wheat);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _materialContext.Set("View", camera.GetViewMatrix());
        _materialContext.Set("Projection", camera.GetProjectionMatrix());

        _cubeMesh.Bind();
        _lineMaterial.Apply(_materialContext);
        _cubeMesh.Draw(gl, PrimitiveType.Lines);
        _cubeMesh.Unbind();
    }

    public void Dispose()
    {
        _cubeMesh?.Dispose();
        _lineMaterial?.Dispose();
        GC.SuppressFinalize(this);
    }
}

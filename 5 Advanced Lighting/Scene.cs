using System.Numerics;
using Engine.Geometry;
using Engine.Graphics;
using Silk.NET.OpenGL;

namespace AdvancedLighting;

public sealed class Scene : IDisposable
{
    private float _simulatedTime = 0f;
    private Matrix4x4 _lightModel = Matrix4x4.Identity;

    private readonly MeshPrimitive _sphere = Sphere.Create(
        new Vector3(0.04f),
        new MeshPrimitiveConfig
        {
            HasNormals = false,
            HasUV = false,
            HasNormalMap = false
        }
    );
    private readonly MeshPrimitive _plane = Engine.Geometry.Plane.Create(
        new Vector2(11f),
        new MeshPrimitiveConfig
        {
            HasNormalMap = false,
            StretchTexture = false
        }
    );

    private readonly Mesh _lightMesh;
    private readonly Mesh _planeMesh;

    private readonly Material _lightMaterial;
    private readonly Material _planeMaterial;

    private readonly MaterialContext _materialContext = new();
    private readonly MaterialPropertyBlock _lightBlock = new();

    public bool IsPaused { get; set; } = false;

    public Material PlaneMaterial => _planeMaterial;

    public Scene(GL gl)
    {
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);
        gl.FrontFace(FrontFaceDirection.Ccw);
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.Blend);
        // gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _lightMesh = new Mesh(
            gl,
            _sphere,
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 3, 0)
        );

        _planeMesh = new Mesh(
            gl,
            _plane,
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 8, 0),
            new VertexAttributeDescription(1, 3, VertexAttribPointerType.Float, 8, 3),
            new VertexAttributeDescription(2, 2, VertexAttribPointerType.Float, 8, 6)
        );

        _lightMaterial = MaterialLoader.Load(gl, "FlatColor");
        _planeMaterial = MaterialLoader.Load(gl, "AdvancedLightingPlane");
    }

    public void Update(float deltaTime)
    {
        if (!IsPaused)
        {
            _simulatedTime += deltaTime;
        }
    }

    public void Draw(GL gl, Engine.Camera camera, double time)
    {
        gl.ClearColor(System.Drawing.Color.Black);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _lightModel = Matrix4x4.Identity;
        _lightModel *= Matrix4x4.CreateTranslation(0f, 2.1f + 2f * MathF.Cos(_simulatedTime), 0f);
        _lightModel *= Matrix4x4.CreateRotationY(_simulatedTime, new Vector3(0.5f));

        _materialContext.Set("View", camera.GetViewMatrix());
        _materialContext.Set("Projection", camera.GetProjectionMatrix());
        _materialContext.Set("CameraPosition", camera.Position);
        _materialContext.Set("LightPosition", new Vector3(_lightModel.M41, _lightModel.M42, _lightModel.M43));

        _lightMesh.Bind();
        _lightBlock.Set("uModel", _lightModel);
        _lightMaterial.SetProperty("Color", new Vector3(1f));
        _lightMaterial.Apply(_materialContext, _lightBlock);
        _lightMesh.Draw(gl);
        _lightMesh.Unbind();

        _planeMesh.Bind();
        _planeMaterial.Apply(_materialContext);
        _planeMesh.Draw(gl);
        _planeMesh.Unbind();
    }

    public void Dispose()
    {
        _lightMesh?.Dispose();
        _planeMesh?.Dispose();
        _lightMaterial?.Dispose();
        _planeMaterial?.Dispose();
        GC.SuppressFinalize(this);
    }
}

using System.Numerics;
using Engine.Geometry;
using Engine.Graphics;
using Silk.NET.OpenGL;

namespace BasicLighting;

public sealed class Scene : IDisposable
{
    private readonly LightSettings[] _lights =
    [
        new(new Vector3(0f, 1f, -4.125f), 0.05f, 0.2f, 0.1f),
        new(new Vector3(0f, 1f, -1.375f), 0.25f, 0.4f, 0.3f),
        new(new Vector3(0f, 1f,  1.375f), 0.5f, 0.6f, 0.5f),
        new(new Vector3(0f, 1f,  4.125f), 0.75f, 0.8f, 0.7f),
    ];

    private readonly Mesh _lightMesh;
    private readonly Mesh _planeMesh;

    private readonly Material _lightMaterial;
    private readonly Material _planeMaterial;

    private readonly MaterialContext _materialContext = new();
    private readonly MaterialPropertyBlock _lightBlock = new();

    public Material PlaneMaterial => _planeMaterial;
    public IReadOnlyList<LightSettings> Lights => _lights;

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
            Sphere.Create(
                new Vector3(0.04f),
                new MeshPrimitiveConfig
                {
                    HasNormals = false,
                    HasUV = false,
                    HasNormalMap = false
                }
            ),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 3, 0)
        );

        _planeMesh = new Mesh(
            gl,
            Engine.Geometry.Plane.Create(
                new Vector2(11f),
                new MeshPrimitiveConfig
                {
                    HasNormalMap = false,
                    StretchTexture = false
                }
            ),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 8, 0),
            new VertexAttributeDescription(1, 3, VertexAttribPointerType.Float, 8, 3),
            new VertexAttributeDescription(2, 2, VertexAttribPointerType.Float, 8, 6)
        );

        _lightMaterial = MaterialLoader.Load(gl, "FlatColor");
        _planeMaterial = MaterialLoader.Load(gl, "BasicLightingPlane");
    }

    public void Draw(GL gl, Engine.Camera camera)
    {
        gl.ClearColor(System.Drawing.Color.Black);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _materialContext.Set("View", camera.GetViewMatrix());
        _materialContext.Set("Projection", camera.GetProjectionMatrix());
        _materialContext.Set("CameraPosition", camera.Position);

        _planeMaterial.Set("uLightCount", _lights.Length);

        for (int i = 0; i < _lights.Length; i++)
        {
            var light = _lights[i];
            _planeMaterial.Set($"uLight[{i}].position", light.Position);
            _planeMaterial.Set($"uLight[{i}].ambient", new Vector3(light.Ambient));
            _planeMaterial.Set($"uLight[{i}].diffuse", new Vector3(light.Diffuse));
            _planeMaterial.Set($"uLight[{i}].specular", new Vector3(light.Specular));
        }

        _planeMesh.Bind();
        _planeMaterial.Apply(_materialContext);
        _planeMesh.Draw(gl);
        _planeMesh.Unbind();

        _lightMesh.Bind();
        for (int i = 0; i < _lights.Length; i++)
        {
            var light = _lights[i];
            _lightBlock.Set("uModel", Matrix4x4.CreateTranslation(light.Position));
            _lightMaterial.SetProperty("Color", new Vector3(light.Specular));
            _lightMaterial.Apply(_materialContext, _lightBlock);
            _lightMesh.Draw(gl);
        }
        _lightMesh.Unbind();
    }

    public void Dispose()
    {
        _lightMesh?.Dispose();
        _planeMesh?.Dispose();
        _lightMaterial?.Dispose();
        _planeMaterial?.Dispose();
        GC.SuppressFinalize(this);
    }

    public sealed class LightSettings(Vector3 position, float ambient, float diffuse, float specular)
    {
        public Vector3 Position { get; set; } = position;
        public float Ambient { get; set; } = ambient;
        public float Diffuse { get; set; } = diffuse;
        public float Specular { get; set; } = specular;
    }
}

using System.Numerics;
using Engine;
using Engine.Geometry;
using Engine.Graphics;
using Silk.NET.OpenGL;

namespace NormalMapping;

public sealed class Scene : IDisposable
{
    private readonly Mesh _lightMesh;
    private readonly Mesh _planeMesh;

    private readonly Material _lightMaterial;
    private readonly Material _planeMaterial;

    private readonly MaterialContext _materialContext = new();
    private readonly MaterialPropertyBlock _lightBlock = new();

    public Material LightMaterial => _lightMaterial;
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
            Sphere.Create(
                new Vector3(0.3f),
                new MeshPrimitiveConfig
                {
                    Slices = 8,
                    Stacks = 8,
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
                    StretchTexture = false
                }
            ),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 14, 0),
            new VertexAttributeDescription(1, 3, VertexAttribPointerType.Float, 14, 3),
            new VertexAttributeDescription(2, 2, VertexAttribPointerType.Float, 14, 6),
            new VertexAttributeDescription(3, 3, VertexAttribPointerType.Float, 14, 8),
            new VertexAttributeDescription(4, 3, VertexAttribPointerType.Float, 14, 11)
        );

        _lightMaterial = MaterialLoader.Load(gl, "FlatColor3");
        _planeMaterial = MaterialLoader.Load(gl, "BrickWallNormal");
        _lightMaterial.Set("uColor", new Vector3(1.0f, 0.8f, 0.45f));
        _planeMaterial.Set("uLight.color", new Vector3(1.0f, 0.8f, 0.45f));
    }

    public void Draw(GL gl, Camera camera, double time)
    {
        gl.ClearColor(System.Drawing.Color.Black);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        float angle = 0.5f * (float)time;
        const float radius = 1.5f;
        const float height = 2.5f;
        float bounce = 2f * MathF.Sin(2f * angle);
        var platformCenter = Vector3.Zero;
        float x = platformCenter.X + radius * MathF.Cos(angle);
        float z = platformCenter.Z + radius * MathF.Sin(angle);
        var lightPos = new Vector3(x, platformCenter.Y + height + bounce, z);

        Matrix4x4 lightModel = Matrix4x4.Identity;
        lightModel *= Matrix4x4.CreateScale(0.2f);
        lightModel *= Matrix4x4.CreateTranslation(lightPos);

        _materialContext.Set("View", camera.GetViewMatrix());
        _materialContext.Set("Projection", camera.GetProjectionMatrix());
        _materialContext.Set("CameraPosition", camera.Position);
        _materialContext.Set("LightPosition", lightPos);

        _lightMesh.Bind();
        _lightBlock.Set("uModel", lightModel);
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

using System.Numerics;
using Engine;
using Engine.Geometry;
using Engine.Graphics;
using Silk.NET.OpenGL;

namespace Shadows;

public sealed class Scene : IDisposable
{
    private const uint ShadowWidth = 2048;
    private const uint ShadowHeight = 2048;
    private const float MinShadowNearPlane = 0.1f;
    private const float MinShadowRange = 1f;
    private const float ShadowBoundsMargin = 1f;

    private readonly Mesh _planeMesh;
    private readonly Mesh _cubeMesh;
    private readonly Mesh _sphereMesh;
    private readonly Mesh _cylinderMesh;
    private readonly Mesh _coneMesh;
    private readonly Mesh _torusMesh;
    private readonly Mesh _capsuleMesh;
    private readonly Mesh _frustumMesh;
    private readonly Mesh _frustumLineMesh;
    private readonly Mesh _quadMesh;

    private readonly Material _sceneMaterial;
    private readonly Material _depthMaterial;
    private readonly Material _debugQuadMaterial;
    private readonly Material _debugFrustumMaterial;

    private readonly MaterialContext _materialContext = new();
    private readonly MaterialPropertyBlock _sceneBlock = new();
    private readonly MaterialPropertyBlock _depthBlock = new();
    private readonly MaterialPropertyBlock _debugBlock = new();
    private readonly MaterialPropertyBlock _debugFrustumBlock = new();

    private float _angle;

    private readonly FrameBuffer _depthMapFbo;
    private readonly DepthTexture _depthMap;

    public Vector3 LightPos { get; set; } = new Vector3(-4.5f, 8f, 2.5f);
    public bool ShowDepthMapDebug { get; set; } = false;

    public Scene(GL gl)
    {
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);
        gl.FrontFace(FrontFaceDirection.Ccw);
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        var curvedPrimitiveConfig = new MeshPrimitiveConfig
        {
            HasNormals = true,
            HasNormalMap = false,
            StretchTexture = false,
            Slices = 128,
            Stacks = 128
        };

        _planeMesh = new Mesh(
            gl,
            Engine.Geometry.Plane.Create(new Vector2(5), curvedPrimitiveConfig),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 8, 0),
            new VertexAttributeDescription(1, 3, VertexAttribPointerType.Float, 8, 3),
            new VertexAttributeDescription(2, 2, VertexAttribPointerType.Float, 8, 6)
        );

        _cubeMesh = new Mesh(
            gl,
            Cube.Create(new Vector3(0.5f), curvedPrimitiveConfig),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 8, 0),
            new VertexAttributeDescription(1, 3, VertexAttribPointerType.Float, 8, 3),
            new VertexAttributeDescription(2, 2, VertexAttribPointerType.Float, 8, 6)
        );

        _sphereMesh = new Mesh(
            gl,
            Sphere.Create(new Vector3(0.33f), curvedPrimitiveConfig),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 8, 0),
            new VertexAttributeDescription(1, 3, VertexAttribPointerType.Float, 8, 3),
            new VertexAttributeDescription(2, 2, VertexAttribPointerType.Float, 8, 6)
        );

        _cylinderMesh = new Mesh(
            gl,
            Cylinder.Create(new Vector3(0.5f, 1.0f, 0.5f), curvedPrimitiveConfig),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 8, 0),
            new VertexAttributeDescription(1, 3, VertexAttribPointerType.Float, 8, 3),
            new VertexAttributeDescription(2, 2, VertexAttribPointerType.Float, 8, 6)
        );

        _coneMesh = new Mesh(
            gl,
            Cone.Create(new Vector3(0.7f, 1.0f, 0.7f), curvedPrimitiveConfig),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 8, 0),
            new VertexAttributeDescription(1, 3, VertexAttribPointerType.Float, 8, 3),
            new VertexAttributeDescription(2, 2, VertexAttribPointerType.Float, 8, 6)
        );

        _torusMesh = new Mesh(
            gl,
            Torus.Create(new Vector3(0.7f, 0.26f, 0.7f), curvedPrimitiveConfig),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 8, 0),
            new VertexAttributeDescription(1, 3, VertexAttribPointerType.Float, 8, 3),
            new VertexAttributeDescription(2, 2, VertexAttribPointerType.Float, 8, 6)
        );

        _capsuleMesh = new Mesh(
            gl,
            Capsule.Create(new Vector3(0.2f, 0.6f, 0.2f), curvedPrimitiveConfig),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 8, 0),
            new VertexAttributeDescription(1, 3, VertexAttribPointerType.Float, 8, 3),
            new VertexAttributeDescription(2, 2, VertexAttribPointerType.Float, 8, 6)
        );

        _frustumMesh = new Mesh(
            gl,
            Cube.Create(
                new Vector3(2f),
                new MeshPrimitiveConfig
                {
                    HasNormals = false,
                    HasUV = false,
                    HasNormalMap = false
                }
            ),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 3, 0)
        );

        _frustumLineMesh = new Mesh(
            gl,
            Cube.Create(
                new Vector3(2f),
                new MeshPrimitiveConfig
                {
                    HasNormals = false,
                    HasUV = false,
                    HasNormalMap = false,
                    PrimitiveType = PrimitiveType.Lines
                }
            ),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 3, 0)
        );

        _quadMesh = MeshFactory.CreateFullscreenQuad(gl);

        _sceneMaterial = MaterialLoader.Load(gl, "ShadowMappingScene");
        _depthMaterial = MaterialLoader.Load(gl, "ShadowMappingDepth");
        _debugQuadMaterial = MaterialLoader.Load(gl, "ShadowMappingDebugQuad");
        _debugFrustumMaterial = MaterialLoader.Load(gl, "FlatColor4");

        _depthMapFbo = new FrameBuffer(gl);
        _depthMap = new DepthTexture(gl, ShadowWidth, ShadowHeight);
        _depthMapFbo.Bind();
        _depthMapFbo.AttachTexture2D(
            FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D,
            _depthMap.Handle
        );
        _depthMapFbo.SetDrawReadBuffers(DrawBufferMode.None, ReadBufferMode.None);
        _depthMapFbo.Validate();
        FrameBuffer.Unbind(gl);
    }

    public void Draw(GL gl, Camera camera, float dt)
    {
        _angle += dt;

        Matrix4x4 lightView = Matrix4x4.CreateLookAt(LightPos, Vector3.Zero, Vector3.UnitY);
        GetShadowProjectionBounds(lightView, out float halfSize, out float nearPlane, out float farPlane);
        Matrix4x4 lightProjection = Matrix4x4.CreateOrthographicOffCenter(
            -halfSize, halfSize,
            -halfSize, halfSize,
            nearPlane, farPlane
        );
        Matrix4x4 lightSpaceMatrix = lightView * lightProjection;
        _materialContext.Set("Projection", camera.GetProjectionMatrix());
        _materialContext.Set("View", camera.GetViewMatrix());
        _materialContext.Set("ViewPosition", camera.Position);
        _materialContext.Set("LightPosition", LightPos);
        _materialContext.Set("LightSpaceMatrix", lightSpaceMatrix);

        Span<int> viewport = stackalloc int[4];
        gl.GetInteger(GetPName.Viewport, viewport);
        uint viewportWidth = (uint)viewport[2];
        uint viewportHeight = (uint)viewport[3];

        gl.Viewport(0, 0, ShadowWidth, ShadowHeight);
        _depthMapFbo.Bind();
        gl.Clear(ClearBufferMask.DepthBufferBit);

        gl.Enable(EnableCap.PolygonOffsetFill);
        gl.PolygonOffset(0.6f, 1.0f);
        RenderScene(gl, _depthMaterial, _depthBlock);
        gl.Disable(EnableCap.PolygonOffsetFill);

        FrameBuffer.Unbind(gl);

        gl.Viewport(0, 0, viewportWidth, viewportHeight);
        gl.ClearColor(0.53f, 0.81f, 0.92f, 1f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _depthMap.Bind(1);
        RenderScene(gl, _sceneMaterial, _sceneBlock);

        if (!ShowDepthMapDebug) return;

        if (Matrix4x4.Invert(lightSpaceMatrix, out Matrix4x4 frustumModel))
        {
            _debugFrustumBlock.Set("uModel", frustumModel);
            _debugFrustumBlock.Set("uColor", new Vector4(1f, 0.78f, 0.19f, 0.14f));
            gl.DepthMask(false);
            gl.Disable(EnableCap.CullFace);

            _frustumMesh.Bind();
            _debugFrustumMaterial.Apply(_materialContext, _debugFrustumBlock);
            _frustumMesh.Draw(gl);
            _debugFrustumBlock.Set("uColor", new Vector4(1f, 0.92f, 0.35f, 0.95f));

            _frustumLineMesh.Bind();
            _debugFrustumMaterial.Apply(_materialContext, _debugFrustumBlock);
            _frustumLineMesh.Draw(gl, PrimitiveType.Lines);

            _frustumLineMesh.Unbind();
            _frustumMesh.Unbind();

            gl.Enable(EnableCap.CullFace);
            gl.DepthMask(true);
        }

        uint debugWidth = Math.Max(220u, viewportWidth / 4u);
        uint debugHeight = Math.Max(140u, viewportHeight / 4u);

        gl.Disable(EnableCap.DepthTest);
        gl.Viewport(0, 0, debugWidth, debugHeight);

        _depthMap.Bind(0);
        _debugBlock.Set("uNearPlane", nearPlane);
        _debugBlock.Set("uFarPlane", farPlane);

        _quadMesh.Bind();
        _debugQuadMaterial.Apply(_materialContext, _debugBlock);
        _quadMesh.Draw(gl);
        _quadMesh.Unbind();

        gl.Viewport(0, 0, viewportWidth, viewportHeight);
        gl.Enable(EnableCap.DepthTest);
    }

    public void Dispose()
    {
        _planeMesh.Dispose();
        _cubeMesh.Dispose();
        _sphereMesh.Dispose();
        _cylinderMesh.Dispose();
        _coneMesh.Dispose();
        _torusMesh.Dispose();
        _capsuleMesh.Dispose();
        _frustumMesh.Dispose();
        _frustumLineMesh.Dispose();
        _quadMesh.Dispose();
        _sceneMaterial.Dispose();
        _depthMaterial.Dispose();
        _debugQuadMaterial.Dispose();
        _debugFrustumMaterial.Dispose();
        _depthMap.Dispose();
        _depthMapFbo.Dispose();
        GC.SuppressFinalize(this);
    }

    private void RenderScene(GL gl, Material material, MaterialPropertyBlock block)
    {
        block.Set("uModel", Matrix4x4.Identity);
        _planeMesh.Bind();
        material.Apply(_materialContext, block);
        _planeMesh.Draw(gl);
        _planeMesh.Unbind();

        block.Set("uModel",
            Matrix4x4.CreateFromYawPitchRoll(_angle * 0.6f, _angle * 0.6f, 0f) *
            Matrix4x4.CreateTranslation(-0.8f, 1f, 0f));
        _cubeMesh.Bind();
        material.Apply(_materialContext, block);
        _cubeMesh.Draw(gl);
        _cubeMesh.Unbind();

        block.Set("uModel", Matrix4x4.CreateTranslation(1.6f, 0.25f, -1.6f));
        _cubeMesh.Bind();
        material.Apply(_materialContext, block);
        _cubeMesh.Draw(gl);
        _cubeMesh.Unbind();

        block.Set("uModel", Matrix4x4.CreateTranslation(-1.55f, 0.45f, -1.35f));
        _sphereMesh.Bind();
        material.Apply(_materialContext, block);
        _sphereMesh.Draw(gl);
        _sphereMesh.Unbind();

        block.Set("uModel",
            Matrix4x4.CreateRotationZ(MathF.PI * 0.5f) *
            Matrix4x4.CreateTranslation(-1.4f, 0.6f, 1.35f));
        _cylinderMesh.Bind();
        material.Apply(_materialContext, block);
        _cylinderMesh.Draw(gl);
        _cylinderMesh.Unbind();

        block.Set("uModel", Matrix4x4.CreateTranslation(0.85f, 0.5f, 1.45f));
        _coneMesh.Bind();
        material.Apply(_materialContext, block);
        _coneMesh.Draw(gl);
        _coneMesh.Unbind();

        block.Set("uModel",
            Matrix4x4.CreateFromYawPitchRoll(45f, 45f, 45f) *
            Matrix4x4.CreateTranslation(1.25f, 0.95f, -0.15f));
        _torusMesh.Bind();
        material.Apply(_materialContext, block);
        _torusMesh.Draw(gl);
        _torusMesh.Unbind();

        block.Set("uModel", Matrix4x4.CreateTranslation(-1.73f, 0.3f, -0.1f));
        _capsuleMesh.Bind();
        material.Apply(_materialContext, block);
        _capsuleMesh.Draw(gl);
        _capsuleMesh.Unbind();
    }

    private static void GetShadowProjectionBounds(
        Matrix4x4 lightView,
        out float halfSize,
        out float nearPlane,
        out float farPlane
    )
    {
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float minZ = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        float maxZ = float.NegativeInfinity;

        void IncludePoint(Vector3 point)
        {
            Vector3 p = Vector3.Transform(point, lightView);
            minX = MathF.Min(minX, p.X);
            minY = MathF.Min(minY, p.Y);
            minZ = MathF.Min(minZ, p.Z);
            maxX = MathF.Max(maxX, p.X);
            maxY = MathF.Max(maxY, p.Y);
            maxZ = MathF.Max(maxZ, p.Z);
        }

        void IncludeSphere(Vector3 center, float radius)
        {
            Vector3 c = Vector3.Transform(center, lightView);
            minX = MathF.Min(minX, c.X - radius);
            minY = MathF.Min(minY, c.Y - radius);
            minZ = MathF.Min(minZ, c.Z - radius);
            maxX = MathF.Max(maxX, c.X + radius);
            maxY = MathF.Max(maxY, c.Y + radius);
            maxZ = MathF.Max(maxZ, c.Z + radius);
        }

        const float planeHalfExtent = 2.5f;
        IncludePoint(new Vector3(-planeHalfExtent, 0f, -planeHalfExtent));
        IncludePoint(new Vector3(-planeHalfExtent, 0f, planeHalfExtent));
        IncludePoint(new Vector3(planeHalfExtent, 0f, -planeHalfExtent));
        IncludePoint(new Vector3(planeHalfExtent, 0f, planeHalfExtent));

        const float unitCubeRadius = 0.8660254f; // sqrt(3) * 0.5
        IncludeSphere(new Vector3(0f, 1f, 0f), unitCubeRadius * 0.4f);
        IncludeSphere(new Vector3(1.6f, 0.25f, -1.6f), unitCubeRadius * 0.5f);
        IncludeSphere(new Vector3(-1.55f, 0.45f, -1.35f), 0.45f);
        IncludeSphere(new Vector3(-1.9f, 0.6f, 1.35f), 0.7f);
        IncludeSphere(new Vector3(0.85f, 0.5f, 1.45f), 0.6f);
        IncludeSphere(new Vector3(0.25f, 0.35f, -0.15f), 0.55f);
        IncludeSphere(new Vector3(-0.25f, 0.6f, 1.15f), 0.65f);

        float radiusX = MathF.Max(MathF.Abs(minX), MathF.Abs(maxX));
        float radiusY = MathF.Max(MathF.Abs(minY), MathF.Abs(maxY));
        halfSize = MathF.Max(radiusX, radiusY) + ShadowBoundsMargin;

        nearPlane = MathF.Max(MinShadowNearPlane, -maxZ - ShadowBoundsMargin);
        farPlane = MathF.Max(nearPlane + MinShadowRange, -minZ + ShadowBoundsMargin);
    }
}

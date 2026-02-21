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
    private const int CascadeCount = 4;
    private const float CascadeSplitLambda = 0.6f;
    private const float CsmCameraNear = 0.1f;
    private const float CsmCameraFar = 30;
    private const float ShadowBoundsMargin = 1f;
    private const int CascadeTestRowCount = 29;
    private const float CascadeTestRowSpacing = 1f;
    private const float CascadeTestRowX = -3.0f;
    private const float CascadeTestRowY = 0.25f;
    private const float CascadeTestRowStartZ = -14f;

    private static readonly Vector4[] CascadeDebugColors =
    [
        new(1f, 0.35f, 0.1f, 0.20f),
        new(1f, 0.75f, 0.1f, 0.16f),
        new(0.25f, 0.9f, 0.2f, 0.12f),
        new(0.2f, 0.65f, 1f, 0.10f)
    ];

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
    private readonly Matrix4x4[] _cascadeFrustumModels = new Matrix4x4[CascadeCount];

    private float _angle;
    private readonly CascadedShadowMap _csm;
    private bool _hasCascadeData;
    private bool _wasFreezeCsm;
    private Matrix4x4 _frozenCascadeView = Matrix4x4.Identity;

    public bool ShowDepthMapDebug { get; set; } = false;
    public bool FreezeCsm { get; set; } = false;
    public bool ShowCascadeColors { get; set; } = false;
    public float SunAzimuthDeg { get; set; } = 140f;
    public float SunElevationDeg { get; set; } = 66f;
    public int DebugCascadeIndex { get; set; } = 0;

    public static int MaxCascadeIndex => CascadeCount - 1;

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
            Engine.Geometry.Plane.Create(new Vector2(7f, 30f), curvedPrimitiveConfig),
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

        _csm = new CascadedShadowMap(gl, ShadowWidth, CascadeCount);
    }

    public void Draw(GL gl, Camera camera, float dt)
    {
        _angle += dt;
        Matrix4x4 currentView = camera.GetViewMatrix();

        Span<int> windowViewport = stackalloc int[4];
        gl.GetInteger(GetPName.Viewport, windowViewport);
        uint viewportWidth = (uint)windowViewport[2];
        uint viewportHeight = (uint)windowViewport[3];

        bool shouldUpdateCascades = !FreezeCsm || !_hasCascadeData || (FreezeCsm && !_wasFreezeCsm);
        if (shouldUpdateCascades)
        {
            Vector3 lightDir = GetLightDirection(SunAzimuthDeg, SunElevationDeg);
            float[] cascadeSplits = BuildCascadeSplits(CsmCameraNear, CsmCameraFar, CascadeCount, CascadeSplitLambda);

            float previousSplit = CsmCameraNear;
            for (int cascadeIndex = 0; cascadeIndex < CascadeCount; cascadeIndex++)
            {
                float splitDistance = cascadeSplits[cascadeIndex];
                Matrix4x4 lightSpaceMatrix = CalculateCascadeLightSpaceMatrix(
                    camera,
                    previousSplit,
                    splitDistance,
                    lightDir
                );

                _csm.SetCascadeData(cascadeIndex, lightSpaceMatrix, splitDistance);

                if (Matrix4x4.Invert(lightSpaceMatrix, out Matrix4x4 frustumModel))
                {
                    _cascadeFrustumModels[cascadeIndex] = frustumModel;
                }
                else
                {
                    _cascadeFrustumModels[cascadeIndex] = Matrix4x4.Identity;
                }

                previousSplit = splitDistance;
            }

            _frozenCascadeView = currentView;
            _hasCascadeData = true;
        }

        for (int cascadeIndex = 0; cascadeIndex < CascadeCount; cascadeIndex++)
        {
            _materialContext.Set("LightSpaceMatrix", _csm.LightSpaceMatrices[cascadeIndex]);
            _csm.BindCascadeForWriting(cascadeIndex);
            _csm.ClearDepth();
            gl.Enable(EnableCap.PolygonOffsetFill);
            gl.PolygonOffset(0.6f, 1.0f);
            RenderScene(gl, _depthMaterial, _depthBlock);
            gl.Disable(EnableCap.PolygonOffsetFill);
        }
        _csm.Unbind();

        gl.Viewport(0, 0, viewportWidth, viewportHeight);
        gl.ClearColor(0.53f, 0.81f, 0.92f, 1f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _materialContext.Set("Projection", camera.GetProjectionMatrix());
        _materialContext.Set("View", currentView);
        _materialContext.Set("CascadeView", FreezeCsm ? _frozenCascadeView : currentView);
        _materialContext.Set("ViewPosition", camera.Position);
        _materialContext.Set("LightDirection", GetLightDirection(SunAzimuthDeg, SunElevationDeg));

        _sceneMaterial.Set("uCascadeCount", CascadeCount);
        _sceneMaterial.Set("uShowCascadeColors", ShowCascadeColors ? 1 : 0);
        for (int i = 0; i < CascadeCount; i++)
        {
            _sceneMaterial.Set($"uLightSpaceMatrices[{i}]", _csm.LightSpaceMatrices[i]);
            _sceneMaterial.Set($"uCascadeSplits[{i}]", _csm.CascadeSplits[i]);
        }

        _csm.BindForReading(1);
        RenderScene(gl, _sceneMaterial, _sceneBlock);

        if (!ShowDepthMapDebug)
        {
            _wasFreezeCsm = FreezeCsm;
            return;
        }

        DrawCascadeFrustums(gl);
        DrawDepthMapOverlay(gl, viewportWidth, viewportHeight);
        _wasFreezeCsm = FreezeCsm;
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
        _csm.Dispose();
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

        for (int i = 0; i < CascadeTestRowCount; i++)
        {
            float z = CascadeTestRowStartZ + i * CascadeTestRowSpacing;
            block.Set("uModel", Matrix4x4.CreateTranslation(CascadeTestRowX, CascadeTestRowY, z));
            _cubeMesh.Bind();
            material.Apply(_materialContext, block);
            _cubeMesh.Draw(gl);
            _cubeMesh.Unbind();
        }
    }

    private Matrix4x4 CalculateCascadeLightSpaceMatrix(
        Camera camera,
        float cascadeNear,
        float cascadeFar,
        Vector3 lightDirection
    )
    {
        Span<Vector3> corners = stackalloc Vector3[8];
        GetFrustumCornersWorldSpace(camera, cascadeNear, cascadeFar, corners);

        Vector3 center = Vector3.Zero;
        for (int i = 0; i < corners.Length; i++)
        {
            center += corners[i];
        }
        center /= corners.Length;

        float radius = 0f;
        for (int i = 0; i < corners.Length; i++)
        {
            float distance = Vector3.Distance(center, corners[i]);
            radius = MathF.Max(radius, distance);
        }
        radius += ShadowBoundsMargin;

        Vector3 lightPosition = center - lightDirection * (radius * 2f);
        Matrix4x4 lightView = Matrix4x4.CreateLookAt(lightPosition, center, GetStableUpVector(lightDirection));

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float minZ = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        float maxZ = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 ls = Vector3.Transform(corners[i], lightView);
            minX = MathF.Min(minX, ls.X);
            minY = MathF.Min(minY, ls.Y);
            minZ = MathF.Min(minZ, ls.Z);
            maxX = MathF.Max(maxX, ls.X);
            maxY = MathF.Max(maxY, ls.Y);
            maxZ = MathF.Max(maxZ, ls.Z);
        }

        float nearPlane = MathF.Max(0.1f, -maxZ - ShadowBoundsMargin);
        float farPlane = MathF.Max(nearPlane + 1f, -minZ + ShadowBoundsMargin);

        Matrix4x4 lightProjection = Matrix4x4.CreateOrthographicOffCenter(
            minX - ShadowBoundsMargin,
            maxX + ShadowBoundsMargin,
            minY - ShadowBoundsMargin,
            maxY + ShadowBoundsMargin,
            nearPlane,
            farPlane
        );

        return lightView * lightProjection;
    }

    private static void GetFrustumCornersWorldSpace(
        Camera camera,
        float nearPlane,
        float farPlane,
        Span<Vector3> corners
    )
    {
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 180f * camera.Zoom,
            camera.AspectRatio,
            nearPlane,
            farPlane
        );
        Matrix4x4 view = camera.GetViewMatrix();

        if (!Matrix4x4.Invert(view * projection, out Matrix4x4 inverseViewProjection))
        {
            throw new InvalidOperationException("Failed to invert view-projection matrix.");
        }

        int index = 0;
        for (int z = -1; z <= 1; z += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int x = -1; x <= 1; x += 2)
                {
                    Vector4 corner = Vector4.Transform(new Vector4(x, y, z, 1f), inverseViewProjection);
                    corners[index++] = new Vector3(corner.X, corner.Y, corner.Z) / corner.W;
                }
            }
        }
    }

    private static float[] BuildCascadeSplits(float nearPlane, float farPlane, int cascadeCount, float lambda)
    {
        var splits = new float[cascadeCount];
        float clipRange = farPlane - nearPlane;

        for (int i = 0; i < cascadeCount; i++)
        {
            float p = (i + 1f) / cascadeCount;
            float log = nearPlane * MathF.Pow(farPlane / nearPlane, p);
            float linear = nearPlane + clipRange * p;
            splits[i] = linear + lambda * (log - linear);
        }

        return splits;
    }

    private void DrawCascadeFrustums(GL gl)
    {
        gl.DepthMask(false);
        gl.Disable(EnableCap.CullFace);

        for (int i = 0; i < CascadeCount; i++)
        {
            _debugFrustumBlock.Set("uModel", _cascadeFrustumModels[i]);
            _debugFrustumBlock.Set("uColor", CascadeDebugColors[i % CascadeDebugColors.Length]);
            _frustumMesh.Bind();
            _debugFrustumMaterial.Apply(_materialContext, _debugFrustumBlock);
            _frustumMesh.Draw(gl);
            _frustumMesh.Unbind();

            _debugFrustumBlock.Set("uColor", new Vector4(1f, 1f, 1f, 0.95f));
            _frustumLineMesh.Bind();
            _debugFrustumMaterial.Apply(_materialContext, _debugFrustumBlock);
            _frustumLineMesh.Draw(gl, PrimitiveType.Lines);
            _frustumLineMesh.Unbind();
        }

        gl.Enable(EnableCap.CullFace);
        gl.DepthMask(true);
    }

    private void DrawDepthMapOverlay(GL gl, uint viewportWidth, uint viewportHeight)
    {
        int debugCascade = Math.Clamp(DebugCascadeIndex, 0, CascadeCount - 1);
        uint debugWidth = Math.Max(220u, viewportWidth / 4u);
        uint debugHeight = Math.Max(140u, viewportHeight / 4u);

        gl.Disable(EnableCap.DepthTest);
        gl.Viewport(0, 0, debugWidth, debugHeight);

        _csm.BindForReading(0);
        _debugBlock.Set("uCascadeLayer", debugCascade);
        _debugBlock.Set("uNearPlane", CsmCameraNear);
        _debugBlock.Set("uFarPlane", _csm.CascadeSplits[debugCascade]);

        _quadMesh.Bind();
        _debugQuadMaterial.Apply(_materialContext, _debugBlock);
        _quadMesh.Draw(gl);
        _quadMesh.Unbind();

        gl.Viewport(0, 0, viewportWidth, viewportHeight);
        gl.Enable(EnableCap.DepthTest);
    }

    private static Vector3 GetLightDirection(float azimuthDeg, float elevationDeg)
    {
        float az = MathF.PI / 180f * azimuthDeg;
        float el = MathF.PI / 180f * elevationDeg;
        float cosEl = MathF.Cos(el);
        Vector3 sunDirection = new(
            cosEl * MathF.Cos(az),
            MathF.Sin(el),
            cosEl * MathF.Sin(az)
        );

        return Vector3.Normalize(-sunDirection);
    }

    private static Vector3 GetStableUpVector(Vector3 direction)
    {
        float upAlignment = MathF.Abs(Vector3.Dot(Vector3.Normalize(direction), Vector3.UnitY));
        return upAlignment > 0.999f ? Vector3.UnitZ : Vector3.UnitY;
    }
}

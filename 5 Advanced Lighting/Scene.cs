using System.Numerics;
using Engine.Geometry;
using Engine.Graphics;
using Silk.NET.OpenGL;

namespace AdvancedLighting;

public class Scene : IDisposable
{
    public static float Shininess { get; set; } = 64f;
    public static float Gamma { get; set; } = 2.2f;
    public static bool UseBlinnPhong { get; set; } = true;
    public static bool IsPaused { get; set; } = false;

    private readonly BufferObject<float> _lightvbo;
    private readonly BufferObject<uint> _lightebo;
    private readonly VertexArrayObject<float, uint> _lightvao;

    private readonly BufferObject<float> _vbo;
    private readonly BufferObject<uint> _ebo;
    private readonly VertexArrayObject<float, uint> _vao;

    private readonly ShaderProgram _shader;
    private readonly ShaderProgram _lightingShader;

    private readonly Engine.Graphics.Texture _texture;

    private Matrix4x4 _lightModel = Matrix4x4.Identity;
    private readonly MeshPrimitive _sphere =
        Sphere.Create(new Vector3(0.04f, 0.04f, 0.04f), 16, 16, false, false, false);
    private readonly MeshPrimitive _plane =
        Engine.Geometry.Plane.Create(new Vector2(11f, 11f), normalMap: false, stretchTexture: false);

    private float _simulatedTime = 0f;

    public Scene(GL gl)
    {
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);
        gl.FrontFace(FrontFaceDirection.Ccw);
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.Blend);
        // gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _lightebo = new BufferObject<uint>(
            gl,
            _sphere.Indices,
            BufferTargetARB.ElementArrayBuffer,
            BufferUsageARB.StaticDraw
        );
        _lightvbo = new BufferObject<float>(
            gl,
            _sphere.Vertices,
            BufferTargetARB.ArrayBuffer,
            BufferUsageARB.StaticDraw
        );
        _lightvao = new VertexArrayObject<float, uint>(gl, _lightvbo, _lightebo);

        _lightvao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);
        _lightvao.Unbind();

        _ebo = new BufferObject<uint>(
            gl,
            _plane.Indices,
            BufferTargetARB.ElementArrayBuffer,
            BufferUsageARB.StaticDraw
        );
        _vbo = new BufferObject<float>(
            gl,
            _plane.Vertices,
            BufferTargetARB.ArrayBuffer,
            BufferUsageARB.StaticDraw
        );
        _vao = new VertexArrayObject<float, uint>(gl, _vbo, _ebo);

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 8, 0);
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 8, 3);
        _vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, 8, 6);

        _shader = new ShaderProgram(gl, "main_shader.vert", "main_shader.frag");
        _lightingShader = new ShaderProgram(gl, "main_shader.vert", "lighting.frag");
        _texture = new Engine.Graphics.Texture(gl, "brickwall.jpg");
    }

    public void Update(float deltaTime)
    {
        if (!IsPaused)
        {
            _simulatedTime += deltaTime;
        }
    }

    public unsafe void Draw(GL gl, Engine.Camera camera, double time)
    {
        gl.ClearColor(System.Drawing.Color.Black);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _lightvao.Bind();
        _shader.Use();

        _lightModel = Matrix4x4.Identity;
        _lightModel *= Matrix4x4.CreateTranslation(0f, 2.1f + 2f * MathF.Cos(_simulatedTime), 0f);
        _lightModel *= Matrix4x4.CreateRotationY(_simulatedTime, new Vector3(0.5f, 0.5f, 0.5f));

        _shader.SetMatrix4("uModel", _lightModel);
        _shader.SetMatrix4("uView", camera.GetViewMatrix());
        _shader.SetMatrix4("uProjection", camera.GetProjectionMatrix());

        gl.DrawElements(
            PrimitiveType.Triangles,
            (uint)_sphere.Indices.Length,
            DrawElementsType.UnsignedInt,
            null
        );

        _lightvao.Unbind();

        _vao.Bind();
        _lightingShader.Use();
        _texture.Bind();

        _lightingShader.SetMatrix4("uModel", Matrix4x4.Identity);
        _lightingShader.SetMatrix4("uView", camera.GetViewMatrix());
        _lightingShader.SetMatrix4("uProjection", camera.GetProjectionMatrix());

        _lightingShader.SetVector3("uViewPosition", camera.Position);

        _lightingShader.SetInt("uMaterial.diffuse", 0);
        _lightingShader.SetFloat("uMaterial.shininess", Shininess);
        _lightingShader.SetFloat("uGamma", Gamma);
        _lightingShader.SetBool("uBlinnPhong", UseBlinnPhong);

        _lightingShader.SetVector3("uLight.position", _lightModel.M41, _lightModel.M42, _lightModel.M43);

        gl.DrawElements(
            PrimitiveType.Triangles,
            (uint)_plane.Indices.Length,
            DrawElementsType.UnsignedInt,
            null
        );
    }

    public void Dispose()
    {
        _texture?.Dispose();

        _lightvbo?.Dispose();
        _lightebo?.Dispose();
        _lightvao?.Dispose();
        _shader?.Dispose();

        _vbo?.Dispose();
        _ebo?.Dispose();
        _vao?.Dispose();
        _lightingShader?.Dispose();
    }
}

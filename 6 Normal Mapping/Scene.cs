using System.Numerics;
using Engine.Geometry;
using Engine.Graphics;
using Silk.NET.OpenGL;

namespace NormalMapping;

public class Scene : IDisposable
{
    public static float Shininess { get; set; } = 32f;
    public static float Ambient { get; set; } = 0.3f;
    public static float Specular { get; set; } = 0.4f;

    private readonly BufferObject<float> _lightvbo;
    private readonly BufferObject<uint> _lightebo;
    private readonly VertexArrayObject<float, uint> _lightvao;

    private readonly BufferObject<float> _vbo;
    private readonly BufferObject<uint> _ebo;
    private readonly VertexArrayObject<float, uint> _vao;

    private readonly ShaderProgram _shader;
    private readonly ShaderProgram _lightingShader;

    private readonly Engine.Graphics.Texture _diffuseTexture;
    private readonly Engine.Graphics.Texture _normalTexture;

    private readonly MeshPrimitive _sphere =
        Sphere.Create(new Vector3(0.3f, 0.3f, 0.3f), 8, 8, false, false, false);
    private readonly MeshPrimitive _plane =
        Engine.Geometry.Plane.Create(new Vector2(11, 11), stretchTexture: false);

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

        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 14, 0);
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 14, 3);
        _vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, 14, 6);
        _vao.VertexAttributePointer(3, 3, VertexAttribPointerType.Float, 14, 8);
        _vao.VertexAttributePointer(4, 3, VertexAttribPointerType.Float, 14, 11);

        _shader = new ShaderProgram(gl, "main_shader.vert", "main_shader.frag");
        _lightingShader = new ShaderProgram(gl, "normal.vert", "normal.frag");
        _diffuseTexture = new Engine.Graphics.Texture(gl, "brickwall.jpg");
        _normalTexture = new Engine.Graphics.Texture(gl, "brickwall_normal.jpg");
    }

    public unsafe void Draw(GL gl, Engine.Camera camera, double time)
    {
        gl.ClearColor(System.Drawing.Color.Black);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        float angle = 0.5f * (float)time;
        var radius = 1.5f;
        var height = 2.5f;
        float bounce = 2f * MathF.Sin(2f * angle);
        var platformCenter = Vector3.Zero;
        float x = platformCenter.X + radius * MathF.Cos(angle);
        float z = platformCenter.Z + radius * MathF.Sin(angle);
        var lightPos = new Vector3(x, platformCenter.Y + height + bounce, z);

        Matrix4x4 lightModel = Matrix4x4.Identity;
        lightModel *= Matrix4x4.CreateScale(0.2f);
        lightModel *= Matrix4x4.CreateTranslation(lightPos);

        _lightvao.Bind();
        _shader.Use();

        _shader.SetMatrix4("uModel", lightModel);
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
        _diffuseTexture.Bind(0);
        _normalTexture.Bind(1);

        _lightingShader.SetMatrix4("uModel", Matrix4x4.Identity);
        _lightingShader.SetMatrix4("uView", camera.GetViewMatrix());
        _lightingShader.SetMatrix4("uProjection", camera.GetProjectionMatrix());

        _lightingShader.SetVector3("uViewPosition", camera.Position);
        _lightingShader.SetVector3("uLightPosition", lightPos);

        _lightingShader.SetInt("uMaterial.diffuse", 0);
        _lightingShader.SetInt("uMaterial.normal", 1);
        _lightingShader.SetFloat("uMaterial.shininess", Shininess);

        _lightingShader.SetVector3("uLight.ambient", Ambient);
        _lightingShader.SetVector3("uLight.specular", Specular);

        gl.DrawElements(
            PrimitiveType.Triangles,
            (uint)_plane.Indices.Length,
            DrawElementsType.UnsignedInt,
            null
        );
    }

    public void Dispose()
    {
        _diffuseTexture?.Dispose();
        _normalTexture?.Dispose();

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

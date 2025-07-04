using System.Numerics;
using Engine.Geometry;
using Engine.Graphics;
using Engine.Physics.Colliders;
using Engine.Physics.Core;
using Engine.Physics.Utilities;
using Silk.NET.OpenGL;

namespace Collision;

public class Scene : IDisposable
{
    private const float FixedTimeStep = 1f / 64f;
    private float _accumulatedTime = 0f;
    private readonly Random _random = new();

    private readonly BufferObject<float> _lightVbo;
    private readonly BufferObject<uint> _lightEbo;
    private readonly VertexArrayObject<float, uint> _lightVao;

    private readonly BufferObject<float> _vbo;
    private readonly BufferObject<uint> _ebo;
    private readonly VertexArrayObject<float, uint> _vao;

    private readonly BufferObject<float> _colliderVbo;
    private readonly BufferObject<uint> _colliderEbo;
    private readonly VertexArrayObject<float, uint> _colliderVao;

    private readonly ShaderProgram _shader;
    private readonly ShaderProgram _lightingShader;

    private readonly Engine.Graphics.Texture _diffuseTexture;
    private readonly Engine.Graphics.Texture _normalTexture;


    private readonly MeshPrimitive _sphere;
    private readonly MeshPrimitive _plane;
    private readonly Vector3 _sphereSize = new(1f, 1f, 1f);
    private readonly Vector3 _planeSize = new(2f, 0.001f, 2f);
    private readonly List<PhysicsObject> _cubeFaces = [];

    public static float Shininess { get; set; } = 32f;
    public static float Ambient { get; set; } = 0.5f;
    public static float Specular { get; set; } = 0.0f;
    public List<PhysicsObject> Objects = [];
    public PhysicsWorld PhysicsWorld = new();

    public Scene(GL gl)
    {
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);
        gl.FrontFace(FrontFaceDirection.Ccw);
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.Blend);
        gl.LineWidth(2f);
        // gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _sphere = Sphere.Create(_sphereSize, 64, 64, true, false, false);
        _lightEbo = new BufferObject<uint>(gl, _sphere.Indices, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw);
        _lightVbo = new BufferObject<float>(gl, _sphere.Vertices, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        _lightVao = new VertexArrayObject<float, uint>(gl, _lightVbo, _lightEbo);
        _lightVao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 6, 0);
        _lightVao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 6, 3);
        _lightVao.Unbind();

        var colliderMesh = Cube.Create(_sphereSize, false, false, false, false, PrimitiveType.Lines);
        _colliderVbo = new BufferObject<float>(gl, colliderMesh.Vertices, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        _colliderEbo = new BufferObject<uint>(gl, colliderMesh.Indices, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw);
        _colliderVao = new VertexArrayObject<float, uint>(gl, _colliderVbo, _colliderEbo);
        _colliderVao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);
        _colliderVao.Unbind();

        _plane = Cube.Create(_planeSize, stretchTexture: true);
        _ebo = new BufferObject<uint>(gl, _plane.Indices, BufferTargetARB.ElementArrayBuffer, BufferUsageARB.StaticDraw);
        _vbo = new BufferObject<float>(gl, _plane.Vertices, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        _vao = new VertexArrayObject<float, uint>(gl, _vbo, _ebo);
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 14, 0);
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 14, 3);
        _vao.VertexAttributePointer(2, 2, VertexAttribPointerType.Float, 14, 6);
        _vao.VertexAttributePointer(3, 3, VertexAttribPointerType.Float, 14, 8);
        _vao.VertexAttributePointer(4, 3, VertexAttribPointerType.Float, 14, 11);

        _shader = new ShaderProgram(gl, "main_shader.glslv", "main_shader.glslf");
        _lightingShader = new ShaderProgram(gl, "normal.glslv", "normal.glslf");
        _diffuseTexture = new Engine.Graphics.Texture(gl, "brickwall.jpg");
        _normalTexture = new Engine.Graphics.Texture(gl, "brickwall_normal.jpg");

        CreateCubeFaces(_planeSize);
        // CreateObjects(100);
    }

    public unsafe void Draw(GL gl, Engine.Camera camera, float deltaTime)
    {
        gl.ClearColor(System.Drawing.Color.Black);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        UpdatePhysics(deltaTime, camera.Position);

        // _colliderVao.Bind();
        // _colliderShader.Use();

        // // collisdes
        // foreach (var obj in _objects)
        // {
        //     _colliderShader.SetMatrix4("uModel", obj.Transform.WorldMatrix);
        //     _colliderShader.SetMatrix4("uView", camera.GetViewMatrix());
        //     _colliderShader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
        //     _colliderShader.SetVector4("uColor", 0f, 0f, 0f, 1f);
        //     gl.DrawElements(PrimitiveType.Lines, 24, DrawElementsType.UnsignedInt, null);
        // }

        // _colliderVao.Unbind();

        _vao.Bind();
        _lightingShader.Use();
        _diffuseTexture.Bind(0);
        _normalTexture.Bind(1);

        _lightingShader.SetVector3("uLightPosition", 0f);
        _lightingShader.SetVector3("uViewPosition", camera.Position);

        _lightingShader.SetInt("uMaterial.diffuse", 0);
        _lightingShader.SetInt("uMaterial.normal", 1);

        _lightingShader.SetFloat("uMaterial.shininess", Shininess);
        _lightingShader.SetVector3("uLight.ambient", Ambient);
        _lightingShader.SetVector3("uLight.specular", Specular);

        // cube faces
        foreach (var face in _cubeFaces)
        {
            _lightingShader.SetMatrix4("uModel", face.Transform.WorldMatrix);
            _lightingShader.SetMatrix4("uView", camera.GetViewMatrix());
            _lightingShader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
            gl.DrawElements(PrimitiveType.Triangles, (uint)_plane.Indices.Length, DrawElementsType.UnsignedInt, null);
        }

        _lightVao.Bind();
        _shader.Use();

        // objects
        foreach (var obj in Objects)
        {
            // outline
            gl.CullFace(TriangleFace.Front);
            Vector3 outlineScale = obj.Transform.Scale * 1.02f;
            Matrix4x4 outlineMatrix =
                Matrix4x4.CreateScale(outlineScale) *
                Matrix4x4.CreateFromQuaternion(obj.Transform.Rotation) *
                Matrix4x4.CreateTranslation(obj.Transform.Position);

            _shader.SetVector4("uColor", new Vector4(0f, 0f, 0f, 1f));
            _shader.SetMatrix4("uModel", outlineMatrix);
            _shader.SetMatrix4("uView", camera.GetViewMatrix());
            _shader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
            gl.DrawElements(PrimitiveType.Triangles, (uint)_sphere.Indices.Length, DrawElementsType.UnsignedInt, null);

            // object
            gl.CullFace(TriangleFace.Back);
            obj.CurrentColor = Vector4.Lerp(obj.CurrentColor, new Vector4(1f), deltaTime);
            _shader.SetVector4("uColor", obj.CurrentColor);
            _shader.SetMatrix4("uModel", obj.Transform.WorldMatrix);
            _shader.SetMatrix4("uView", camera.GetViewMatrix());
            _shader.SetMatrix4("uProjection", camera.GetProjectionMatrix());
            gl.DrawElements(PrimitiveType.Triangles, (uint)_sphere.Indices.Length, DrawElementsType.UnsignedInt, null);
        }

        _lightVao.Unbind();
    }

    public void Dispose()
    {
        _diffuseTexture?.Dispose();
        _normalTexture?.Dispose();

        _lightVbo?.Dispose();
        _lightEbo?.Dispose();
        _lightVao?.Dispose();
        _shader?.Dispose();

        _colliderVbo?.Dispose();
        _colliderEbo?.Dispose();
        _colliderVao?.Dispose();

        _vbo?.Dispose();
        _ebo?.Dispose();
        _vao?.Dispose();
        _lightingShader?.Dispose();
    }

    private void CreateCubeFaces(Vector3 size)
    {
        for (int i = 1; i < 6; i++)
        {
            var transform = new Transform();

            switch (i)
            {
                case 0: // Y+
                    transform.Position = new Vector3(0, size.X / 2, 0);
                    break;
                case 1: // Y-
                    transform.Position = new Vector3(0, -size.X / 2, 0);
                    break;
                case 2: // Z+
                    transform.Position = new Vector3(0, 0, size.X / 2);
                    transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2);
                    break;
                case 3: // Z-
                    transform.Position = new Vector3(0, 0, -size.X / 2);
                    transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2);
                    break;
                case 4: // X+
                    transform.Position = new Vector3(size.X / 2, 0, 0);
                    transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2) *
                                         Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
                    break;
                case 5: // X-
                    transform.Position = new Vector3(-size.X / 2, 0, 0);
                    transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -MathF.PI / 2) *
                                         Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
                    break;
            }

            var faceObject = new PhysicsObject(
                transform,
                new Rigidbody
                {
                    Mass = 0f,
                    UseGravity = false,
                    Restitution = 0.7f,
                    Friction = 0f
                },
                new BoxCollider(transform, size)
            );

            _cubeFaces.Add(faceObject);
            PhysicsWorld.AddObject(faceObject);
        }
    }

    private void CreateObjects(uint amount)
    {
        for (int i = 0; i < amount; i++)
        {
            var transform = new Transform
            {
                Position = new Vector3(
                    _random.NextSingle() * 4f - 2f,
                    _random.NextSingle() * 4f - 2f,
                    _random.NextSingle() * 4f - 2f
                ),
                Rotation = Quaternion.CreateFromYawPitchRoll(MathF.PI / _random.Next(1, 10), MathF.PI / _random.Next(1, 10), MathF.PI / _random.Next(1, 10)),
                Scale = new Vector3(0.1f)
            };

            var rigidbody = new Rigidbody
            {
                Mass = 0.05f,
                UseGravity = true,
                // Gravity = Vector3.Zero,
                AngularVelocity = new Vector3(MathF.PI / _random.Next(1, 10), MathF.PI / _random.Next(1, 10), MathF.PI / _random.Next(1, 10)),
                // Restitution = 1f,
                // Friction = 0f,
                // AngularDamping = 0f,
                // LinearDamping = 0f,
            };

            var collider = new SphereCollider(transform, new Vector3(1f));

            var obj = new PhysicsObject(transform, rigidbody, collider);

            Objects.Add(obj);
            PhysicsWorld.AddObject(obj);
            Objects[i].Rigidbody.AddForce(new Vector3(_random.NextSingle() * 500f - 250f, _random.NextSingle() * 500f - 250f, _random.NextSingle() * 500f - 250f));
        }
    }

    private void UpdatePhysics(float deltaTime, Vector3 cameraPosition)

    {
        _accumulatedTime += deltaTime;
        while (_accumulatedTime >= FixedTimeStep)
        {
            PhysicsWorld.Update(FixedTimeStep, cameraPosition);
            _accumulatedTime -= FixedTimeStep;
        }
    }
}
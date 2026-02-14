using System.Numerics;
using Engine;
using Engine.Entities;
using Engine.Geometry;
using Engine.Graphics;
using Engine.Physics;
using Engine.Physics.Colliders;
using Silk.NET.OpenGL;

namespace Physics;

public sealed class Scene : IDisposable
{
    private const float ArenaSize = 6f;
    private const float WallHeight = 0.75f;
    private const float CubeSize = 0.5f;
    private const float FixedTimeStep = 1f / 120f;
    private const float MaxVisualSpeed = 8f;
    private const float MaxFrameDt = 0.1f;
    private const int MaxPhysicsStepsPerFrame = 12;

    private readonly Mesh _planeMesh;
    private readonly Mesh _cubeMesh;
    private readonly Mesh _colliderMesh;
    private readonly Material _flatMaterial;
    private readonly MaterialContext _materialContext = new();
    private readonly MaterialPropertyBlock _drawBlock = new();
    private readonly Matrix4x4[] _planeModels;
    private readonly Vector3[] _planeColors;

    private readonly PhysicsWorld _physicsWorld = new();
    private readonly List<PhysicsBody> _cubeBodies = [];
    private readonly Player _player;
    private float _accumulatedTime;

    public bool ShowColliders { get; set; } = false;
    public Player Player => _player;

    public Scene(GL gl)
    {
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);
        gl.FrontFace(FrontFaceDirection.Ccw);
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        gl.LineWidth(2f);

        _planeMesh = new Mesh(
            gl,
            Engine.Geometry.Plane.Create(
                new Vector2(1f),
                new MeshPrimitiveConfig
                {
                    HasNormals = false,
                    HasUV = false,
                    HasNormalMap = false
                }
            ),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 3, 0)
        );

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

        _colliderMesh = new Mesh(
            gl,
            Cube.Create(
                Vector3.One,
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

        _flatMaterial = MaterialLoader.Load(gl, "FlatColor");

        _planeModels = CreateArenaModels();
        _planeColors =
        [
            new Vector3(0.78f, 0.82f, 0.90f), // floor
            new Vector3(0.29f, 0.88f, 0.88f), // north wall
            new Vector3(0.88f, 0.29f, 0.29f), // south wall
            new Vector3(0.95f, 0.94f, 0.41f), // east wall
            new Vector3(0.53f, 0.88f, 0.29f), // west wall
        ];

        CreateArenaColliders();
        CreateCubes();
        _player = CreatePlayer();
    }

    public void Update(float dt, in PlayerInput input, Camera camera)
    {
        dt = Math.Clamp(dt, 0f, MaxFrameDt);
        _accumulatedTime += dt;

        int steps = 0;
        while (_accumulatedTime >= FixedTimeStep && steps < MaxPhysicsStepsPerFrame)
        {
            _player.ApplyInput(input, camera.Front, camera.Right);
            _physicsWorld.Step(FixedTimeStep);
            _accumulatedTime -= FixedTimeStep;
            steps++;
        }

        if (steps == MaxPhysicsStepsPerFrame && _accumulatedTime > FixedTimeStep)
        {
            _accumulatedTime = FixedTimeStep;
        }
    }

    public void Draw(GL gl, Camera camera)
    {
        gl.ClearColor(0.05f, 0.06f, 0.08f, 1f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _materialContext.Set("View", camera.GetViewMatrix());
        _materialContext.Set("Projection", camera.GetProjectionMatrix());

        _planeMesh.Bind();
        for (int i = 0; i < _planeModels.Length; i++)
        {
            _drawBlock.Set("uModel", _planeModels[i]);
            _flatMaterial.SetProperty("Color", _planeColors[i]);
            _flatMaterial.Apply(_materialContext, _drawBlock);
            _planeMesh.Draw(gl);
        }
        _planeMesh.Unbind();

        _cubeMesh.Bind();
        foreach (var cubeBody in _cubeBodies)
        {
            DrawCube(gl, cubeBody, ColorFromSpeed(cubeBody.Velocity.Length()));
        }

        DrawCube(gl, _player.Body, new Vector3(0.95f, 0.95f, 0.20f));
        _cubeMesh.Unbind();

        if (!ShowColliders) return;

        _colliderMesh.Bind();
        foreach (var body in _physicsWorld.Bodies)
        {
            if (body.Collider is not BoxCollider boxCollider) continue;

            Matrix4x4 model =
                Matrix4x4.CreateScale(boxCollider.HalfExtents * 2f) *
                Matrix4x4.CreateTranslation(body.Position + boxCollider.Offset);

            Vector3 debugColor = body.IsStatic
                ? new Vector3(0.15f, 1.00f, 0.35f)
                : new Vector3(1.00f, 1.00f, 1.00f);

            _drawBlock.Set("uModel", model);
            _flatMaterial.SetProperty("Color", debugColor);
            _flatMaterial.Apply(_materialContext, _drawBlock);
            _colliderMesh.Draw(gl, PrimitiveType.Lines);
        }
        _colliderMesh.Unbind();
    }

    public void Dispose()
    {
        _planeMesh?.Dispose();
        _cubeMesh?.Dispose();
        _colliderMesh?.Dispose();
        _flatMaterial?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void DrawCube(GL gl, PhysicsBody body, Vector3 color)
    {
        Matrix4x4 model =
            Matrix4x4.CreateScale(CubeSize) *
            Matrix4x4.CreateTranslation(body.Position);

        _drawBlock.Set("uModel", model);
        _flatMaterial.SetProperty("Color", color);
        _flatMaterial.Apply(_materialContext, _drawBlock);
        _cubeMesh.Draw(gl);
    }

    private static Vector3 ColorFromSpeed(float speed)
    {
        return Vector3.Lerp(
            new Vector3(0.10f, 0.25f, 1.00f),
            new Vector3(1.00f, 0.10f, 0.10f),
            Math.Clamp(speed / MaxVisualSpeed, 0f, 1f)
        );
    }

    private void CreateArenaColliders()
    {
        float half = ArenaSize * 0.5f;
        const float floorHalfHeight = 0.10f;
        const float wallHalfThickness = 0.10f;

        _physicsWorld.AddBody(
            new PhysicsBody(
                new BoxCollider(new Vector3(half, floorHalfHeight, half)),
                isStatic: true
            )
            {
                Position = new Vector3(0f, -floorHalfHeight, 0f)
            }
        );

        _physicsWorld.AddBody(
            new PhysicsBody(
                new BoxCollider(new Vector3(half, WallHeight * 0.5f, wallHalfThickness)),
                isStatic: true
            )
            {
                Position = new Vector3(0f, WallHeight * 0.5f, half + wallHalfThickness)
            }
        );

        _physicsWorld.AddBody(
            new PhysicsBody(
                new BoxCollider(new Vector3(half, WallHeight * 0.5f, wallHalfThickness)),
                isStatic: true
            )
            {
                Position = new Vector3(0f, WallHeight * 0.5f, -half - wallHalfThickness)
            }
        );

        _physicsWorld.AddBody(
            new PhysicsBody(
                new BoxCollider(new Vector3(wallHalfThickness, WallHeight * 0.5f, half)),
                isStatic: true
            )
            {
                Position = new Vector3(half + wallHalfThickness, WallHeight * 0.5f, 0f)
            }
        );

        _physicsWorld.AddBody(
            new PhysicsBody(
                new BoxCollider(new Vector3(wallHalfThickness, WallHeight * 0.5f, half)),
                isStatic: true
            )
            {
                Position = new Vector3(-half - wallHalfThickness, WallHeight * 0.5f, 0f)
            }
        );
    }

    private void CreateCubes()
    {
        Vector3 halfExtents = new(CubeSize * 0.5f);

        for (int i = 0; i < 4; i++)
        {
            float x = -1.8f + i * 1.2f;
            var body = new PhysicsBody(new BoxCollider(halfExtents))
            {
                Position = new Vector3(x, 1.35f, 0f),
                Velocity = new Vector3(1.6f - i * 1.0f, 0f, 0.45f * (i - 1.5f)),
                Mass = 1f,
                Restitution = 0.45f
            };

            _physicsWorld.AddBody(body);
            _cubeBodies.Add(body);
        }
    }

    private Player CreatePlayer()
    {
        var body = new PhysicsBody(new BoxCollider(new Vector3(CubeSize * 0.5f)))
        {
            Position = new Vector3(0f, 1.35f, -2f),
            Mass = 1f,
            Restitution = 0.05f
        };

        _physicsWorld.AddBody(body);
        return new Player(body)
        {
            MoveSpeed = 4.5f,
            JumpSpeed = 5.5f
        };
    }

    private static Matrix4x4[] CreateArenaModels()
    {
        float halfSize = ArenaSize * 0.5f;
        float halfHeight = WallHeight * 0.5f;

        var floor = Matrix4x4.CreateScale(ArenaSize, 1f, ArenaSize);

        var northWall = Matrix4x4.CreateScale(ArenaSize, 1f, WallHeight)
            * Matrix4x4.CreateRotationX(-MathF.PI * 0.5f)
            * Matrix4x4.CreateTranslation(0f, halfHeight, halfSize);

        var southWall = Matrix4x4.CreateScale(ArenaSize, 1f, WallHeight)
            * Matrix4x4.CreateRotationX(MathF.PI * 0.5f)
            * Matrix4x4.CreateTranslation(0f, halfHeight, -halfSize);

        var eastWall = Matrix4x4.CreateScale(WallHeight, 1f, ArenaSize)
            * Matrix4x4.CreateRotationZ(MathF.PI * 0.5f)
            * Matrix4x4.CreateTranslation(halfSize, halfHeight, 0f);

        var westWall = Matrix4x4.CreateScale(WallHeight, 1f, ArenaSize)
            * Matrix4x4.CreateRotationZ(-MathF.PI * 0.5f)
            * Matrix4x4.CreateTranslation(-halfSize, halfHeight, 0f);

        return [floor, northWall, southWall, eastWall, westWall];
    }
}

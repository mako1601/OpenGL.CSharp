using System.Numerics;
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
    private const float SphereRadius = 0.25f;
    private const float CapsuleRadius = 0.25f;
    private const float CapsuleHalfHeight = 0.30f;
    private const float CapsuleHeight = (CapsuleHalfHeight + CapsuleRadius) * 2f;
    private const float FixedTimeStep = 1f / 120f;
    private const float MaxVisualSpeed = 8f;
    private const float MaxFrameDt = 0.1f;
    private const int MaxPhysicsStepsPerFrame = 12;

    private readonly Mesh _planeMesh;
    private readonly Mesh _cubeMesh;
    private readonly Mesh _sphereMesh;
    private readonly Mesh _capsuleMesh;
    private readonly Mesh _colliderMesh;
    private readonly Material _flatMaterial;
    private readonly MaterialContext _materialContext = new();
    private readonly MaterialPropertyBlock _drawBlock = new();
    private readonly Matrix4x4[] _planeModels;
    private readonly Vector3[] _planeColors;
    private readonly Vector3 _playerColors = new(1f, 0.7f, 0f);

    private readonly PhysicsWorld _physicsWorld = new();
    private readonly List<PhysicsBody> _cubeBodies = [];
    private readonly List<PhysicsBody> _sphereBodies = [];
    private readonly List<PhysicsBody> _capsuleBodies = [];
    private Player _player;
    private readonly FollowCameraComponent _followCamera;
    private ColliderType _playerShape = ColliderType.Box;
    private float _accumulatedTime;

    public bool ShowColliders { get; set; } = false;
    public Player Player => _player;
    public FollowCameraComponent FollowCamera => _followCamera;
    public ColliderType CurrentPlayerShape => _playerShape;

    public Scene(GL gl, float aspectRatio)
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

        _sphereMesh = new Mesh(
            gl,
            Sphere.Create(
                new Vector3(SphereRadius),
                new MeshPrimitiveConfig
                {
                    HasNormals = false,
                    HasUV = false,
                    HasNormalMap = false
                }
            ),
            new VertexAttributeDescription(0, 3, VertexAttribPointerType.Float, 3, 0)
        );

        _capsuleMesh = new Mesh(
            gl,
            Capsule.Create(
                new Vector3(CapsuleRadius * 2f, CapsuleHeight, CapsuleRadius * 2f),
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
        CreateSpheres();
        CreateCapsules();
        _player = CreatePlayer();
        _followCamera = new FollowCameraComponent(_player, aspectRatio)
        {
            HeightOffset = 0.35f
        };
        _followCamera.SnapToTarget();
    }

    public void Update(float dt, in PlayerInput input, float lookDeltaX, float lookDeltaY, float zoomDelta)
    {
        dt = Math.Clamp(dt, 0f, MaxFrameDt);

        _followCamera.Rotate(lookDeltaX, lookDeltaY, _followCamera.Camera.Sensitivity);
        _followCamera.AddZoom(zoomDelta);
        _followCamera.Update(dt);

        _accumulatedTime += dt;
        int steps = 0;
        while (_accumulatedTime >= FixedTimeStep && steps < MaxPhysicsStepsPerFrame)
        {
            _player.ApplyInput(input, _followCamera.Camera.Front, _followCamera.Camera.Right);
            _physicsWorld.Step(FixedTimeStep);
            _accumulatedTime -= FixedTimeStep;
            steps++;
        }

        if (steps == MaxPhysicsStepsPerFrame && _accumulatedTime > FixedTimeStep)
        {
            _accumulatedTime = FixedTimeStep;
        }

        _followCamera.UpdateCameraTransform();
    }

    public void Draw(GL gl)
    {
        gl.ClearColor(0.05f, 0.06f, 0.08f, 1f);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _materialContext.Set("View", _followCamera.Camera.GetViewMatrix());
        _materialContext.Set("Projection", _followCamera.Camera.GetProjectionMatrix());

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

        if (_player.Body.Collider is BoxCollider)
        {
            DrawCube(gl, _player.Body, _playerColors);
        }
        _cubeMesh.Unbind();

        _sphereMesh.Bind();
        foreach (var sphereBody in _sphereBodies)
        {
            DrawSphere(gl, sphereBody, ColorFromSpeed(sphereBody.Velocity.Length()));
        }

        if (_player.Body.Collider is SphereCollider)
        {
            DrawSphere(gl, _player.Body, _playerColors);
        }
        _sphereMesh.Unbind();

        _capsuleMesh.Bind();
        foreach (var capsuleBody in _capsuleBodies)
        {
            if (capsuleBody.Collider is CapsuleCollider capsuleCollider)
            {
                DrawCapsule(gl, capsuleBody, capsuleCollider, ColorFromSpeed(capsuleBody.Velocity.Length()));
            }
        }

        if (_player.Body.Collider is CapsuleCollider playerCapsuleCollider)
        {
            DrawCapsule(gl, _player.Body, playerCapsuleCollider, _playerColors);
        }
        _capsuleMesh.Unbind();

        if (!ShowColliders) return;

        foreach (var body in _physicsWorld.Bodies)
        {
            Vector3 debugColor = body.IsStatic
                ? new Vector3(0.15f, 1.00f, 0.35f)
                : new Vector3(1.00f, 1.00f, 1.00f);

            if (body.Collider is BoxCollider boxCollider)
            {
                Matrix4x4 model =
                    Matrix4x4.CreateScale(boxCollider.HalfExtents * 2f) *
                    Matrix4x4.CreateTranslation(body.Position + boxCollider.Offset);

                _colliderMesh.Bind();
                _drawBlock.Set("uModel", model);
                _flatMaterial.SetProperty("Color", debugColor);
                _flatMaterial.Apply(_materialContext, _drawBlock);
                _colliderMesh.Draw(gl, PrimitiveType.Lines);
                _colliderMesh.Unbind();
            }
            else if (body.Collider is SphereCollider sphereCollider)
            {
                Matrix4x4 model =
                    Matrix4x4.CreateScale(sphereCollider.Radius / SphereRadius) *
                    Matrix4x4.CreateTranslation(body.Position + sphereCollider.Offset);

                _drawBlock.Set("uModel", model);
                _flatMaterial.SetProperty("Color", debugColor);
                _flatMaterial.Apply(_materialContext, _drawBlock);

                _sphereMesh.Bind();
                gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
                _sphereMesh.Draw(gl);
                gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
                _sphereMesh.Unbind();
            }
            else if (body.Collider is CapsuleCollider capsuleCollider)
            {
                float radiusScale = capsuleCollider.Radius / CapsuleRadius;
                float heightScale = (capsuleCollider.HalfHeight + capsuleCollider.Radius) / (CapsuleHalfHeight + CapsuleRadius);
                Matrix4x4 model =
                    Matrix4x4.CreateScale(radiusScale, heightScale, radiusScale) *
                    Matrix4x4.CreateTranslation(body.Position + capsuleCollider.Offset);

                _drawBlock.Set("uModel", model);
                _flatMaterial.SetProperty("Color", debugColor);
                _flatMaterial.Apply(_materialContext, _drawBlock);

                _capsuleMesh.Bind();
                gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
                _capsuleMesh.Draw(gl);
                gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
                _capsuleMesh.Unbind();
            }
        }
    }

    public void Dispose()
    {
        _planeMesh?.Dispose();
        _cubeMesh?.Dispose();
        _sphereMesh?.Dispose();
        _capsuleMesh?.Dispose();
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

    private void DrawSphere(GL gl, PhysicsBody body, Vector3 color)
    {
        Matrix4x4 model = Matrix4x4.CreateTranslation(body.Position);

        _drawBlock.Set("uModel", model);
        _flatMaterial.SetProperty("Color", color);
        _flatMaterial.Apply(_materialContext, _drawBlock);
        _sphereMesh.Draw(gl);
    }

    private void DrawCapsule(GL gl, PhysicsBody body, CapsuleCollider collider, Vector3 color)
    {
        float radiusScale = collider.Radius / CapsuleRadius;
        float heightScale = (collider.HalfHeight + collider.Radius) / (CapsuleHalfHeight + CapsuleRadius);
        Matrix4x4 model =
            Matrix4x4.CreateScale(radiusScale, heightScale, radiusScale) *
            Matrix4x4.CreateTranslation(body.Position + collider.Offset);

        _drawBlock.Set("uModel", model);
        _flatMaterial.SetProperty("Color", color);
        _flatMaterial.Apply(_materialContext, _drawBlock);
        _capsuleMesh.Draw(gl);
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
            new PhysicsBody(new BoxCollider(new Vector3(half, floorHalfHeight, half)), isStatic: true)
            {
                Position = new Vector3(0f, -floorHalfHeight, 0f),
                CollisionLayer = CollisionLayers.StaticWorld,
                CollisionMask = CollisionLayers.DynamicBody | CollisionLayers.Player
            }
        );

        _physicsWorld.AddBody(
            new PhysicsBody(new BoxCollider(new Vector3(half, WallHeight * 0.5f, wallHalfThickness)), isStatic: true)
            {
                Position = new Vector3(0f, WallHeight * 0.5f, half + wallHalfThickness),
                CollisionLayer = CollisionLayers.StaticWorld,
                CollisionMask = CollisionLayers.DynamicBody | CollisionLayers.Player
            }
        );

        _physicsWorld.AddBody(
            new PhysicsBody(new BoxCollider(new Vector3(half, WallHeight * 0.5f, wallHalfThickness)), isStatic: true)
            {
                Position = new Vector3(0f, WallHeight * 0.5f, -half - wallHalfThickness),
                CollisionLayer = CollisionLayers.StaticWorld,
                CollisionMask = CollisionLayers.DynamicBody | CollisionLayers.Player
            }
        );

        _physicsWorld.AddBody(
            new PhysicsBody(new BoxCollider(new Vector3(wallHalfThickness, WallHeight * 0.5f, half)), isStatic: true)
            {
                Position = new Vector3(half + wallHalfThickness, WallHeight * 0.5f, 0f),
                CollisionLayer = CollisionLayers.StaticWorld,
                CollisionMask = CollisionLayers.DynamicBody | CollisionLayers.Player
            }
        );

        _physicsWorld.AddBody(
            new PhysicsBody(new BoxCollider(new Vector3(wallHalfThickness, WallHeight * 0.5f, half)), isStatic: true)
            {
                Position = new Vector3(-half - wallHalfThickness, WallHeight * 0.5f, 0f),
                CollisionLayer = CollisionLayers.StaticWorld,
                CollisionMask = CollisionLayers.DynamicBody | CollisionLayers.Player
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
                Restitution = 0.45f,
                CollisionLayer = CollisionLayers.DynamicBody,
                CollisionMask = CollisionLayers.StaticWorld | CollisionLayers.DynamicBody | CollisionLayers.Player
            };

            _physicsWorld.AddBody(body);
            _cubeBodies.Add(body);
        }
    }

    private void CreateSpheres()
    {
        for (int i = 0; i < 4; i++)
        {
            float x = -1.8f + i * 1.2f;
            var body = new PhysicsBody(new SphereCollider(SphereRadius))
            {
                Position = new Vector3(x, 2.25f, 1.6f),
                Velocity = new Vector3(1.1f - i * 0.7f, 0f, -0.35f * (i - 1.5f)),
                Mass = 1f,
                Restitution = 0.55f,
                CollisionLayer = CollisionLayers.DynamicBody,
                CollisionMask = CollisionLayers.StaticWorld | CollisionLayers.DynamicBody | CollisionLayers.Player
            };

            _physicsWorld.AddBody(body);
            _sphereBodies.Add(body);
        }
    }

    private void CreateCapsules()
    {
        for (int i = 0; i < 3; i++)
        {
            float x = -1.2f + i * 1.2f;
            var body = new PhysicsBody(new CapsuleCollider(CapsuleRadius, CapsuleHalfHeight))
            {
                Position = new Vector3(x, 3.0f, -1.2f),
                Velocity = new Vector3(0.45f * (1f - i), 0f, 0.55f * (i - 1f)),
                Mass = 1f,
                Restitution = 0.25f,
                CollisionLayer = CollisionLayers.DynamicBody,
                CollisionMask = CollisionLayers.StaticWorld | CollisionLayers.DynamicBody | CollisionLayers.Player
            };

            _physicsWorld.AddBody(body);
            _capsuleBodies.Add(body);
        }
    }

    private Player CreatePlayer()
    {
        var body = CreatePlayerBody(_playerShape);
        _physicsWorld.AddBody(body);

        return new Player(body)
        {
            MoveSpeed = 4.5f,
            JumpSpeed = 5.5f
        };
    }

    public void SetPlayerShape(ColliderType shape)
    {
        if (_playerShape == shape) return;

        PhysicsBody oldBody = _player.Body;
        var newBody = CreatePlayerBody(shape);
        newBody.Position = oldBody.Position;
        newBody.Velocity = oldBody.Velocity;
        newBody.Gravity = oldBody.Gravity;
        newBody.Mass = oldBody.Mass;
        newBody.Restitution = oldBody.Restitution;
        newBody.StaticFriction = oldBody.StaticFriction;
        newBody.DynamicFriction = oldBody.DynamicFriction;
        newBody.CollisionLayer = oldBody.CollisionLayer;
        newBody.CollisionMask = oldBody.CollisionMask;

        _physicsWorld.RemoveBody(oldBody);
        _physicsWorld.AddBody(newBody);

        _player = new Player(newBody)
        {
            MoveSpeed = _player.MoveSpeed,
            JumpSpeed = _player.JumpSpeed
        };
        _playerShape = shape;
        _followCamera.SetTarget(_player);
    }

    private static PhysicsBody CreatePlayerBody(ColliderType shape)
    {
        Collider collider = shape switch
        {
            ColliderType.Sphere => new SphereCollider(SphereRadius),
            ColliderType.Capsule => new CapsuleCollider(CapsuleRadius, CapsuleHalfHeight),
            _ => new BoxCollider(new Vector3(CubeSize * 0.5f))
        };

        return new PhysicsBody(collider)
        {
            Position = new Vector3(0f, 1.35f, -2f),
            Mass = 1f,
            Restitution = 0.05f,
            CollisionLayer = CollisionLayers.Player,
            CollisionMask = CollisionLayers.StaticWorld | CollisionLayers.DynamicBody
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

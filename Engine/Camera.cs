using System.Numerics;
using Silk.NET.Maths;

namespace Engine;

public class Camera
{
    private Vector3 _position;
    private float _speed;
    private float _zoom;
    private float _sensitivity;
    private float _aspectRatio;

    private float _pitch;
    private float _yaw;

    private Vector3 _front;
    private Vector3 _up;
    private Vector3 _right;

    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }

    public float Speed
    {
        get => _speed;
        set => _speed = value < 0f
            ? throw new ArgumentOutOfRangeException(nameof(Speed), "Speed cannot be negative.")
            : value;
    }

    public float Zoom
    {
        get => _zoom;
        private set => _zoom = value is < 0f or > 180f
            ? throw new ArgumentOutOfRangeException(nameof(Zoom), "Zoom must be between 0 and 180 degrees.")
            : value;
    }

    public float Sensitivity
    {
        get => _sensitivity;
        private set => _sensitivity = value < 0f
            ? throw new ArgumentOutOfRangeException(nameof(Sensitivity), "Sensitivity cannot be negative.")
            : value;
    }

    public float AspectRatio
    {
        get => _aspectRatio;
        private set => _aspectRatio = value <= 0f
            ? throw new ArgumentOutOfRangeException(nameof(AspectRatio), "AspectRatio must be positive.")
            : value;
    }

    public Vector3 Front => _front;
    public Vector3 Up => _up;
    public Vector3 Right => _right;

    public float Yaw
    {
        get => _yaw;
        set => _yaw = value;
    }

    public float Pitch
    {
        get => _pitch;
        set => _pitch = value < -89.999f
            ? _pitch = -89.999f
            : value > 89.999f
                ? _pitch = 89.999f
                : _pitch = value;
    }

    public Camera(
        Vector3 position,
        float speed = 4f,
        float zoom = 90f,
        float sensitivity = 1f,
        float aspectRatio = 1.6f,
        float pitch = 0f,
        float yaw = -90f
    )
    {
        _position = position;
        Speed = speed;
        Zoom = zoom;
        Sensitivity = sensitivity;
        AspectRatio = aspectRatio;
        _pitch = pitch;
        _yaw = yaw;

        UpdateVectors();
    }

    public void UpdateVectors()
    {
        float radPitch = MathF.PI / 180f * _pitch;
        float radYaw = MathF.PI / 180f * _yaw;

        Vector3 front = new(
            MathF.Cos(radPitch) * MathF.Cos(radYaw),
            MathF.Sin(radPitch),
            MathF.Cos(radPitch) * MathF.Sin(radYaw)
        );

        _front = Vector3.Normalize(front);
        _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
    }

    public Matrix4x4 GetViewMatrix()
        => Matrix4x4.CreateLookAt(_position, _position + _front, _up);

    public Matrix4x4 GetProjectionMatrix() =>
        Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 180f * _zoom,
            _aspectRatio,
            0.001f,
            100f
        );

    public void ChangeAspectRatio(Vector2D<int> newWindowSize)
    {
        if (newWindowSize.X <= 0 || newWindowSize.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newWindowSize), "Window dimensions must be positive non-zero values.");
        }

        AspectRatio = (float)newWindowSize.X / newWindowSize.Y;
    }

    public void ChangeZoom(float deltaScroll) =>
        _zoom = System.Math.Clamp(_zoom - deltaScroll, 60f, 120f);
}

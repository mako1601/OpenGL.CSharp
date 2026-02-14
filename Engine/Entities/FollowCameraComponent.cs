using System.Numerics;

namespace Engine.Entities;

public sealed class FollowCameraComponent
{
    private readonly Player _target;

    public FollowCameraComponent(Player target, float aspectRatio)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        Camera = new Camera(Vector3.Zero, aspectRatio: aspectRatio, pitch: -35f, yaw: 90f);
        SnapToTarget();
    }

    public Camera Camera { get; }
    public float Distance { get; private set; } = 4.5f;
    public float DistanceTarget { get; private set; } = 4.5f;
    public float HeightOffset { get; set; } = 0.35f;
    public float ZoomSmoothness { get; set; } = 14f;
    public float MinDistance { get; set; } = 0.1f;
    public float MaxDistance { get; set; } = 10f;

    public void Rotate(float deltaX, float deltaY, float sensitivity)
    {
        Camera.Yaw += deltaX * sensitivity / 8f;
        Camera.Pitch = Math.Clamp(Camera.Pitch - deltaY * sensitivity / 8f, -89.999f, 89.999f);
    }

    public void AddZoom(float scrollY, float zoomStep = 0.35f)
    {
        DistanceTarget = Math.Clamp(DistanceTarget - scrollY * zoomStep, MinDistance, MaxDistance);
    }

    public void SnapToTarget()
    {
        Distance = DistanceTarget;
        UpdateCameraTransform();
    }

    public void Update(float dt)
    {
        float zoomBlend = dt <= 0f ? 1f : 1f - MathF.Exp(-ZoomSmoothness * dt);
        Distance += (DistanceTarget - Distance) * zoomBlend;
        UpdateCameraTransform();
    }

    public void UpdateCameraTransform()
    {
        float radYaw = Camera.Yaw * MathF.PI / 180f;
        float radPitch = Camera.Pitch * MathF.PI / 180f;

        Vector3 lookDirection = Vector3.Normalize(new Vector3(
            MathF.Cos(radPitch) * MathF.Cos(radYaw),
            MathF.Sin(radPitch),
            MathF.Cos(radPitch) * MathF.Sin(radYaw)
        ));

        Vector3 targetPoint = _target.Position + new Vector3(0f, HeightOffset, 0f);
        Camera.Position = targetPoint - lookDirection * Distance;

        Vector3 toTarget = Vector3.Normalize(targetPoint - Camera.Position);
        Camera.Yaw = MathF.Atan2(toTarget.Z, toTarget.X) * 180f / MathF.PI;
        Camera.Pitch = MathF.Asin(toTarget.Y) * 180f / MathF.PI;
        Camera.UpdateVectors();
    }
}

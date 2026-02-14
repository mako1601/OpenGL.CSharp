using System.Numerics;

namespace Engine.Entities;

public sealed class FollowCameraController(Entity target)
{
    private const float DefaultHeightOffset = 0.35f;
    private const float DefaultZoomSmoothness = 14f;

    public Entity Target { get; } = target ?? throw new ArgumentNullException(nameof(target));

    public float Yaw { get; private set; } = 90f;
    public float Pitch { get; private set; } = -35f;
    public float Distance { get; private set; } = 4.5f;
    public float DistanceTarget { get; private set; } = 4.5f;
    public float MinDistance { get; set; } = 0.1f;
    public float MaxDistance { get; set; } = 10f;
    public float HeightOffset { get; set; } = DefaultHeightOffset;
    public float ZoomSmoothness { get; set; } = DefaultZoomSmoothness;

    public void Rotate(float deltaX, float deltaY, float sensitivity)
    {
        Yaw += deltaX * sensitivity / 8f;
        Pitch = Math.Clamp(Pitch - deltaY * sensitivity / 8f, -89.999f, 89.999f);
    }

    public void AddZoom(float scrollY, float zoomStep = 0.35f)
    {
        DistanceTarget = Math.Clamp(DistanceTarget - scrollY * zoomStep, MinDistance, MaxDistance);
    }

    public void SnapDistanceToTarget()
    {
        Distance = DistanceTarget;
    }

    public void UpdateCamera(Camera camera, float dt)
    {
        ArgumentNullException.ThrowIfNull(camera);

        float zoomBlend = dt <= 0f
            ? 1f
            : 1f - MathF.Exp(-ZoomSmoothness * dt);
        Distance += (DistanceTarget - Distance) * zoomBlend;

        float radYaw = Yaw * MathF.PI / 180f;
        float radPitch = Pitch * MathF.PI / 180f;

        Vector3 lookDirection = Vector3.Normalize(new Vector3(
            MathF.Cos(radPitch) * MathF.Cos(radYaw),
            MathF.Sin(radPitch),
            MathF.Cos(radPitch) * MathF.Sin(radYaw)
        ));

        Vector3 targetPoint = Target.Position + new Vector3(0f, HeightOffset, 0f);
        camera.Position = targetPoint - lookDirection * Distance;

        Vector3 toTarget = Vector3.Normalize(targetPoint - camera.Position);
        camera.Yaw = MathF.Atan2(toTarget.Z, toTarget.X) * 180f / MathF.PI;
        camera.Pitch = MathF.Asin(toTarget.Y) * 180f / MathF.PI;
        camera.UpdateVectors();
    }
}

namespace Engine.Physics;

public static class CollisionLayers
{
    public const uint Default = 1u << 0;
    public const uint StaticWorld = 1u << 1;
    public const uint DynamicBody = 1u << 2;
    public const uint Player = 1u << 3;
}

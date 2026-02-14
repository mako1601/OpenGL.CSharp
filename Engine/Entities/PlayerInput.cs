namespace Engine.Entities;

public readonly record struct PlayerInput(
    bool Forward,
    bool Backward,
    bool Left,
    bool Right,
    bool JumpPressed
);

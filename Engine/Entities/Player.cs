using System.Numerics;
using Engine.Physics;

namespace Engine.Entities;

public sealed class Player(PhysicsBody body) : Entity(body)
{
    public float MoveSpeed { get; set; } = 4f;
    public float JumpSpeed { get; set; } = 5.5f;

    public void ApplyInput(in PlayerInput input, Vector3 cameraForward, Vector3 cameraRight)
    {
        Vector3 forward = new(cameraForward.X, 0f, cameraForward.Z);
        if (forward.LengthSquared() > 0f)
        {
            forward = Vector3.Normalize(forward);
        }
        else
        {
            forward = -Vector3.UnitZ;
        }

        Vector3 right = new(cameraRight.X, 0f, cameraRight.Z);
        if (right.LengthSquared() > 0f)
        {
            right = Vector3.Normalize(right);
        }
        else
        {
            right = Vector3.UnitX;
        }

        Vector3 moveDirection = Vector3.Zero;
        if (input.Forward)  moveDirection += forward;
        if (input.Backward) moveDirection -= forward;
        if (input.Left)     moveDirection -= right;
        if (input.Right)    moveDirection += right;

        if (moveDirection.LengthSquared() > 0f)
        {
            moveDirection = Vector3.Normalize(moveDirection);
        }

        Vector3 velocity = Body.Velocity;
        velocity.X = moveDirection.X * MoveSpeed;
        velocity.Z = moveDirection.Z * MoveSpeed;

        if (input.JumpPressed && Body.IsGrounded)
        {
            velocity.Y = JumpSpeed;
        }

        Body.Velocity = velocity;
    }
}

using System.Numerics;
using Engine;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace BasicLighting;

public class Window : Engine.Window
{
    public GUI? GUI { get; set; }
    public Scene? Scene { get; set; }
    public Camera Camera { get; set; }

    public Window() : base()
    {
        WindowState.Title = "Basic Lighting";
        Camera = new Camera(Vector3.Zero, aspectRatio: (float)WindowState.Size.X / WindowState.Size.Y);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        Camera.Position = new Vector3(5f, 3f, -3.7f);
        Camera.Pitch = -25f;
        Camera.Yaw = 140f;
        Camera.UpdateVectors();
        Scene = new Scene(GL);
        GUI = new GUI(GL, WindowState, InputContext, Scene);
    }

    protected override void OnUpdate(double elapsedTime)
    {
        base.OnUpdate(elapsedTime);

        if (IsClosing) return;

        Vector3 direction = Vector3.Zero;
        if (KeyboardState.IsKeyPressed(Key.W))
        {
            direction += Vector3.Normalize(Vector3.Cross(Camera.Right, -Vector3.UnitY));
        }
        if (KeyboardState.IsKeyPressed(Key.S))
        {
            direction -= Vector3.Normalize(Vector3.Cross(Camera.Right, -Vector3.UnitY));
        }
        if (KeyboardState.IsKeyPressed(Key.A))
        {
            direction -= Camera.Right;
        }
        if (KeyboardState.IsKeyPressed(Key.D))
        {
            direction += Camera.Right;
        }
        if (KeyboardState.IsKeyPressed(Key.Space))
        {
            direction += Vector3.UnitY;
        }
        if (KeyboardState.IsKeyPressed(Key.ShiftLeft))
        {
            direction -= Vector3.UnitY;
        }

        if (direction != Vector3.Zero)
        {
            direction = Vector3.Normalize(direction);
        }

        Camera.Position += direction * (float)elapsedTime * Camera.Speed;
        GUI?.Update((float)elapsedTime);
    }

    protected override void OnRender(double elapsedTime)
    {
        base.OnRender(elapsedTime);

        Scene?.Draw(GL, Camera);
        GUI?.Render(Camera);
    }

    protected override void OnFramebufferResize(Vector2D<int> newSize)
    {
        base.OnFramebufferResize(newSize);

        Camera.ChangeAspectRatio(newSize);
    }

    protected override void OnClose()
    {
        GUI?.Dispose();
        Scene?.Dispose();

        base.OnClose();
    }

    protected override void OnMouseMove(IMouse mouse, Vector2 vector)
    {
        base.OnMouseMove(mouse, vector);

        if (mouse.Cursor.CursorMode != CursorMode.Raw || IsClosing) return;

        var deltaX = mouse.Position.X - WindowCenter.X;
        var deltaY = mouse.Position.Y - WindowCenter.Y;

        mouse.Position = WindowCenter;

        Camera.Yaw   += deltaX * Camera.Sensitivity / 8f;
        Camera.Pitch -= deltaY * Camera.Sensitivity / 8f;
        Camera.UpdateVectors();
    }

    protected override void OnScroll(IMouse mouse, ScrollWheel wheel)
    {
        base.OnScroll(mouse, wheel);

        if (IsClosing) return;

        Camera.ChangeZoom(wheel.Y);
    }
}

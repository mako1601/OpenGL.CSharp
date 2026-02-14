using System.Numerics;
using Engine.Entities;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace Physics;

public class Window : Engine.Window
{
    private float _lookDeltaX;
    private float _lookDeltaY;
    private float _zoomDelta;

    public Scene? Scene { get; set; }
    public GUI? GUI { get; set; }

    public Window() : base()
    {
        WindowState.Title = "Physics";
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        Scene = new Scene(GL, (float)WindowState.Size.X / WindowState.Size.Y);
        GUI = new GUI(GL, WindowState, InputContext, Scene);
    }

    protected override void OnUpdate(double elapsedTime)
    {
        base.OnUpdate(elapsedTime);

        if (IsClosing) return;

        var input = new PlayerInput(
            Forward: KeyboardState.IsKeyPressed(Key.W),
            Backward: KeyboardState.IsKeyPressed(Key.S),
            Left: KeyboardState.IsKeyPressed(Key.A),
            Right: KeyboardState.IsKeyPressed(Key.D),
            JumpPressed: KeyboardState.IsKeyPressed(Key.Space)
        );

        float dt = (float)elapsedTime;
        Scene?.Update(dt, input, _lookDeltaX, _lookDeltaY, _zoomDelta);
        GUI?.Update(dt);

        _lookDeltaX = 0f;
        _lookDeltaY = 0f;
        _zoomDelta = 0f;
    }

    protected override void OnRender(double elapsedTime)
    {
        base.OnRender(elapsedTime);

        if (Scene is not null)
        {
            Scene.Draw(GL);
            GUI?.Render(this);
        }
    }

    protected override void OnFramebufferResize(Vector2D<int> newSize)
    {
        base.OnFramebufferResize(newSize);

        Scene?.FollowCamera.Camera.ChangeAspectRatio(newSize);
    }

    protected override void OnClose()
    {
        GUI?.Dispose();
        Scene?.Dispose();

        base.OnClose();
    }

    protected override void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (mouse.Cursor.CursorMode != CursorMode.Raw || IsClosing) return;

        _lookDeltaX += mouse.Position.X - WindowCenter.X;
        _lookDeltaY += mouse.Position.Y - WindowCenter.Y;
        mouse.Position = WindowCenter;
    }

    protected override void OnScroll(IMouse mouse, ScrollWheel wheel)
    {
        if (IsClosing) return;

        _zoomDelta += wheel.Y;
    }

    protected override void OnKeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        base.OnKeyDown(keyboard, key, arg3);

        if (IsClosing) return;

        if (key == Key.R)
        {
            if (Scene is not null)
            {
                Scene.Player.Position = new Vector3(0f, 2.5f, -2f);
            }
        }
    }
}

using System.Numerics;
using Engine.Entities;
using Silk.NET.Input;

namespace Physics;

public class Window : Engine.Window
{
    private Scene? _scene;
    private GUI? _gui;
    private FollowCameraController? _followCamera;

    public Scene Scene => _scene ?? throw new InvalidOperationException("Scene is not initialized. OnLoad has not completed.");
    public GUI GUI => _gui ?? throw new InvalidOperationException("GUI is not initialized. OnLoad has not completed.");
    public FollowCameraController FollowCamera => _followCamera ?? throw new InvalidOperationException("FollowCamera is not initialized. OnLoad has not completed.");

    protected override bool UseDefaultCameraControls => false;

    public Window() : base()
    {
        WindowState.Title = "Physics";
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        _scene = new Scene(GL);
        _gui = new GUI(GL, WindowState, InputContext, _scene);
        _followCamera = new FollowCameraController(_scene.Player)
        {
            HeightOffset = 0.35f
        };
        _followCamera.UpdateCamera(Camera, 0f);
    }

    protected override void OnUpdate(double elapsedTime)
    {
        base.OnUpdate(elapsedTime);

        if (_isClosing) return;

        var input = new PlayerInput(
            Forward: KeyboardState.IsKeyPressed(Key.W),
            Backward: KeyboardState.IsKeyPressed(Key.S),
            Left: KeyboardState.IsKeyPressed(Key.A),
            Right: KeyboardState.IsKeyPressed(Key.D),
            JumpPressed: KeyboardState.IsKeyPressed(Key.Space)
        );

        float dt = (float)elapsedTime;
        _followCamera?.UpdateCamera(Camera, dt);
        _scene?.Update(dt, input, Camera);
        _gui?.Update(dt);
    }

    protected override void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (_isClosing) return;

        if (mouse.Cursor.CursorMode == CursorMode.Raw)
        {
            var deltaX = mouse.Position.X - WindowCenter.X;
            var deltaY = mouse.Position.Y - WindowCenter.Y;

            mouse.Position = WindowCenter;

            _followCamera?.Rotate(deltaX, deltaY, Camera.Sensitivity);
        }
    }

    protected override void OnScroll(IMouse mouse, ScrollWheel wheel)
    {
        if (_isClosing) return;

        _followCamera?.AddZoom(wheel.Y);
    }

    protected override void OnRender(double elapsedTime)
    {
        base.OnRender(elapsedTime);

        _scene?.Draw(GL, Camera);
        _gui?.Render(this, Camera);
    }

    protected override void OnClose()
    {
        _gui?.Dispose();
        _scene?.Dispose();

        base.OnClose();
    }

    protected override void OnKeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        base.OnKeyDown(keyboard, key, arg3);

        if (key == Key.Equal)
        {
            Camera.ChangeZoom(5f);
        }
        if (key == Key.Minus)
        {
            Camera.ChangeZoom(-5f);
        }
        if (key == Key.R)
        {
            if (_scene is not null)
            {
                _scene.Player.Position = new Vector3(0f, 2.5f, -2f);
            }
        }
    }
}

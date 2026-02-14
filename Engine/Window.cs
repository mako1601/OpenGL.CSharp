using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine;

public class Window
{
    private bool _isFocused = false;
    private bool _isClosing = false;
    private bool _shouldClose = false;
    private int _frameCount;
    private double _fps;
    private double _elapsedTime;
    private readonly double _updateInterval = 0.5d;

    protected GL GL { get; private set; } = null!;
    protected IWindow WindowState { get; private set; }
    protected IInputContext InputContext { get; private set; } = null!;
    protected IKeyboard KeyboardState { get; private set; } = null!;
    protected IMouse MouseState { get; private set; } = null!;

    protected Vector2 WindowCenter => new(WindowState.Size.X / 2f, WindowState.Size.Y / 2f);
    public bool IsFocused => _isFocused;
    public bool IsClosing => _isClosing;
    public double FPS => _fps;

    public Window()
    {
        WindowState = Silk.NET.Windowing.Window.Create(
            WindowOptions.Default with
            {
                Size = new Vector2D<int>(1200, 800),
                Position = new Vector2D<int>(400, 100),
                Title = "Engine.Window",
                VSync = false,
                API = new GraphicsAPI(
                    ContextAPI.OpenGL,
                    ContextProfile.Core,
                    ContextFlags.Default,
                    new APIVersion(4, 6)
                ),
            }
        );

        WindowState.Load += OnLoad;
        WindowState.Update += OnUpdate;
        WindowState.Render += OnRender;
        WindowState.FramebufferResize += OnFramebufferResize;
        WindowState.FocusChanged += OnFocusChanged;
        WindowState.Closing += OnClose;

        _isFocused = true;
    }

    public virtual void Run()
    {
        WindowState.Run();
        WindowState.Dispose();
    }

    protected virtual void OnLoad()
    {
        GL = GL.GetApi(WindowState) ?? throw new NullReferenceException(nameof(GL));

        Console.WriteLine($"Vendor: {GL.GetStringS(GLEnum.Vendor)}");
        Console.WriteLine($"Renderer: {GL.GetStringS(GLEnum.Renderer)}");
        Console.WriteLine($"Driver version: {GL.GetStringS(GLEnum.Version)}");
        Console.WriteLine($"API: {WindowState.API.API} {WindowState.API.Profile} {WindowState.API.Version.MajorVersion}.{WindowState.API.Version.MinorVersion}");

        InputContext = WindowState.CreateInput();

        if (InputContext.Keyboards.Count == 0)
        {
            throw new InvalidOperationException("No keyboard device found.");
        }

        KeyboardState = InputContext.Keyboards[0];
        foreach (var keyboard in InputContext.Keyboards)
        {
            keyboard.KeyChar += OnKeyChar;
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
        }

        if (InputContext.Mice.Count == 0)
        {
            throw new InvalidOperationException("No mouse device found.");
        }

        MouseState = InputContext.Mice[0];
        foreach (var mouse in InputContext.Mice)
        {
            mouse.Cursor.CursorMode = CursorMode.Normal;

            mouse.Click += OnClick;
            mouse.DoubleClick += OnDoubleClick;
            mouse.MouseDown += OnMouseDown;
            mouse.MouseMove += OnMouseMove;
            mouse.MouseUp += OnMouseUp;
            mouse.Scroll += OnScroll;
        }
    }

    protected virtual void OnUpdate(double elapsedTime)
    {
        if (_shouldClose)
        {
            _shouldClose = false;
            OnClose();
            WindowState.Close();
            return;
        }

        _elapsedTime += elapsedTime;
        _frameCount++;

        if (_elapsedTime > _updateInterval)
        {
            _fps = _frameCount / _elapsedTime;
            _frameCount = 0;
            _elapsedTime = 0;
        }
    }

    protected virtual void OnRender(double elapsedTime) { }

    protected virtual void OnFramebufferResize(Vector2D<int> newSize)
    {
        GL?.Viewport(newSize);
    }

    protected virtual void OnFocusChanged(bool isFocused)
    {
        _isFocused = isFocused;

        if (IsFocused)
        {
            WindowState.FramesPerSecond = 0;
            WindowState.UpdatesPerSecond = 0;
        }
        else
        {
            WindowState.FramesPerSecond = 10;
            WindowState.UpdatesPerSecond = 10;
        }
    }

    protected virtual void OnClose()
    {
        if (IsClosing) return;

        _isClosing = true;

        if (InputContext is not null)
        {
            foreach (var k in InputContext.Keyboards)
            {
                k.KeyChar -= OnKeyChar;
                k.KeyDown -= OnKeyDown;
                k.KeyUp -= OnKeyUp;
            }

            foreach (var mouse in InputContext.Mice)
            {
                mouse.Click -= OnClick;
                mouse.DoubleClick -= OnDoubleClick;
                mouse.MouseDown -= OnMouseDown;
                mouse.MouseMove -= OnMouseMove;
                mouse.MouseUp -= OnMouseUp;
                mouse.Scroll -= OnScroll;
            }
        }

        InputContext?.Dispose();
        GL?.Dispose();
    }

    #region Input

    protected virtual void OnClick(IMouse mouse, MouseButton button, Vector2 vector) { }

    protected virtual void OnDoubleClick(IMouse mouse, MouseButton button, Vector2 vector) { }

    protected virtual void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (IsClosing) return;

        if (button == MouseButton.Right)
        {
            if (mouse.Cursor.CursorMode == CursorMode.Raw)
            {
                mouse.Cursor.CursorMode = CursorMode.Normal;
                mouse.Position = new Vector2(WindowCenter.X, WindowCenter.Y);
            }
            else
            {
                mouse.Position = new Vector2(WindowCenter.X, WindowCenter.Y);
                mouse.Cursor.CursorMode = CursorMode.Raw;
            }
        }
    }

    protected virtual void OnMouseMove(IMouse mouse, Vector2 vector) { }

    protected virtual void OnMouseUp(IMouse mouse, MouseButton button) { }

    protected virtual void OnScroll(IMouse mouse, ScrollWheel wheel) { }

    protected virtual void OnKeyChar(IKeyboard keyboard, char arg2) { }

    protected virtual void OnKeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        if (IsClosing) return;

        if (key == Key.Escape)
        {
            _shouldClose = true;
        }
    }

    protected virtual void OnKeyUp(IKeyboard keyboard, Key key, int arg3) { }

    #endregion Input
}

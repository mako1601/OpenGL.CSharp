﻿using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine;

public class Window
{
    protected GL GL { get; set; }
    protected IWindow WindowState { get; set; }
    protected IInputContext InputContext { get; set; }
    protected IKeyboard KeyboardState { get; set; }
    protected IMouse MouseState { get; set; }
    protected Camera Camera { get; set; }
    protected bool _isClosing = false;
    protected Vector2 WindowCenter
    {
        get => new(WindowState.Size.X / 2f, WindowState.Size.Y / 2f);
    }
    protected bool IsFocused { get; set; }
    private bool _shouldClose = false;

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

        IsFocused = true;

        Camera = new Camera(Vector3.Zero, aspectRatio: (float)WindowState.Size.X / WindowState.Size.Y);
    }

    public virtual void Run()
    {
        WindowState.Run();
        WindowState.Dispose();
    }

    protected virtual void OnLoad()
    {
        GL = GL.GetApi(WindowState);

        Console.WriteLine($"Vendor: {GL.GetStringS(GLEnum.Vendor)}");
        Console.WriteLine($"Renderer: {GL.GetStringS(GLEnum.Renderer)}");
        Console.WriteLine($"Driver version: {GL.GetStringS(GLEnum.Version)}");
        Console.WriteLine($"API: {WindowState.API.API} {WindowState.API.Profile} {WindowState.API.Version.MajorVersion}.{WindowState.API.Version.MinorVersion}");

        InputContext = WindowState.CreateInput();

        KeyboardState = InputContext.Keyboards[0];
        foreach (var keyboard in InputContext.Keyboards)
        {
            keyboard.KeyChar += OnKeyChar;
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
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

        var io = ImGuiNET.ImGui.GetIO();

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
    }

    protected virtual void OnRender(double elapsedTime) { }

    protected virtual void OnFramebufferResize(Vector2D<int> newSize)
    {
        GL.Viewport(newSize);
        Camera.ChangeAspectRatio(newSize);
    }

    protected virtual void OnFocusChanged(bool isFocused)
    {
        IsFocused = isFocused;

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
        if (_isClosing) return;

        _isClosing = true;

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

        InputContext?.Dispose();
        GL?.Dispose();
    }

    #region Input

    protected virtual void OnClick(IMouse mouse, MouseButton button, Vector2 vector) { }

    protected virtual void OnDoubleClick(IMouse mouse, MouseButton button, Vector2 vector) { }

    protected virtual void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (_isClosing) return;

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

    protected virtual void OnMouseMove(IMouse mouse, Vector2 vector)
    {
        if (_isClosing) return;

        if (mouse.Cursor.CursorMode == CursorMode.Raw)
        {
            var deltaX = mouse.Position.X - WindowCenter.X;
            var deltaY = mouse.Position.Y - WindowCenter.Y;

            mouse.Position = new Vector2(WindowCenter.X, WindowCenter.Y);

            Camera.Yaw += deltaX * Camera.Sensitivity / 8f;
            Camera.Pitch -= deltaY * Camera.Sensitivity / 8f;

            Camera.UpdateVectors();
        }
    }

    protected virtual void OnMouseUp(IMouse mouse, MouseButton button) { }

    protected virtual void OnScroll(IMouse mouse, ScrollWheel wheel)
    {
        if (_isClosing) return;

        Camera.ChangeZoom(wheel.Y);
    }

    protected virtual void OnKeyChar(IKeyboard keyboard, char arg2) { }

    protected virtual void OnKeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        if (_isClosing) return;

        if (key == Key.Escape)
        {
            _shouldClose = true;
        }
    }

    protected virtual void OnKeyUp(IKeyboard keyboard, Key key, int arg3) { }

    #endregion Input
}

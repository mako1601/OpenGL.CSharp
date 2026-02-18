using System.Numerics;
using Engine.Physics.Colliders;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Shadows;

public sealed class GUI(GL gl, IWindow window, IInputContext input, Scene scene) : IDisposable
{
    private bool _isDisposed;
    private ImGuiController? _controller = new(gl, window, input);
    private readonly Scene _scene = scene;
    private static readonly string[] _playerShapeItems = ["Cube", "Sphere", "Capsule"];

    public void Update(float elapsedTime)
    {
        if (_isDisposed || _controller == null) return;

        _controller.Update(elapsedTime);
    }

    public void Render(Window window)
    {
        if (_isDisposed || _controller == null) return;

        ImGuiNET.ImGui.SetNextWindowSize(new Vector2(255, 267), ImGuiNET.ImGuiCond.FirstUseEver);
        ImGuiNET.ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiNET.ImGuiCond.FirstUseEver);

        ImGuiNET.ImGui.Begin("Physics Sandbox", ImGuiNET.ImGuiWindowFlags.NoMove);
        ImGuiNET.ImGui.Text($"FPS: {window.FPS:0}");
        ImGuiNET.ImGui.End();

        _controller.Render();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _controller?.Dispose();
        _controller = null;
        GC.SuppressFinalize(this);
    }
}

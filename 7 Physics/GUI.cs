using System.Numerics;
using Engine.Physics.Colliders;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Physics;

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

        var showColliders = _scene.ShowColliders;
        var playerPos = _scene.Player.Position;
        var velocity = _scene.Player.Body.Velocity;
        int playerShapeIndex = (int)_scene.CurrentPlayerShape;

        ImGuiNET.ImGui.SetNextWindowSize(new Vector2(255, 267), ImGuiNET.ImGuiCond.FirstUseEver);
        ImGuiNET.ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiNET.ImGuiCond.FirstUseEver);

        ImGuiNET.ImGui.Begin("Physics Sandbox", ImGuiNET.ImGuiWindowFlags.NoMove);
        ImGuiNET.ImGui.Text($"FPS: {window.FPS:0}");

        ImGuiNET.ImGui.SeparatorText("Player");
        ImGuiNET.ImGui.Text($"Position: X {playerPos.X:0.00}  Y {playerPos.Y:0.00}  Z {playerPos.Z:0.00}");
        ImGuiNET.ImGui.Text($"Velocity: {velocity.Length():0.00} ({velocity.X:0.00} {velocity.Y:0.00} {velocity.Z:0.00})");
        if (ImGuiNET.ImGui.Combo("Model", ref playerShapeIndex, _playerShapeItems, _playerShapeItems.Length))
        {
            _scene.SetPlayerShape((ColliderType)playerShapeIndex);
        }

        ImGuiNET.ImGui.SeparatorText("Camera");
        ImGuiNET.ImGui.Text($"Yaw: {_scene.FollowCamera.Camera.Yaw:0.00}");
        ImGuiNET.ImGui.Text($"Pitch: {_scene.FollowCamera.Camera.Pitch:0.00}");
        ImGuiNET.ImGui.Text($"Distance: {_scene.FollowCamera.Distance:0.00}");
        ImGuiNET.ImGui.Text($"FOV: {_scene.FollowCamera.Camera.Zoom:0.00}");

        ImGuiNET.ImGui.SeparatorText("");
        ImGuiNET.ImGui.Checkbox("Show colliders", ref showColliders);
        ImGuiNET.ImGui.End();

        _controller.Render();

        _scene.ShowColliders = showColliders;
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

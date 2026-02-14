using System.Numerics;
using Engine;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Physics;

public sealed class GUI(GL gl, IWindow window, IInputContext input, Scene scene) : IDisposable
{
    private bool _isDisposed;
    private readonly Scene _scene = scene;

    public ImGuiController? Controller { get; private set; } = new ImGuiController(gl, window, input);

    public void Update(float elapsedTime)
    {
        if (_isDisposed || Controller == null) return;
        Controller.Update(elapsedTime);
    }

    public void Render(Window window, Camera camera)
    {
        if (_isDisposed || Controller == null) return;

        var showColliders = _scene.ShowColliders;
        var playerPos = _scene.Player.Position;

        ImGuiNET.ImGui.SetNextWindowSize(new Vector2(256, 213), ImGuiNET.ImGuiCond.FirstUseEver);
        ImGuiNET.ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiNET.ImGuiCond.FirstUseEver);

        ImGuiNET.ImGui.Begin("Physics Sandbox", ImGuiNET.ImGuiWindowFlags.NoMove);

        ImGuiNET.ImGui.Text($"FPS: {window.FPS:0}");

        ImGuiNET.ImGui.SeparatorText("Player");
        ImGuiNET.ImGui.Text($"Position: X {playerPos.X:0.00}  Y {playerPos.Y:0.00}  Z {playerPos.Z:0.00}");
        ImGuiNET.ImGui.Text($"Velocity: {_scene.Player.Body.Velocity.Length():0.00}, ({_scene.Player.Body.Velocity.X:0.00} {_scene.Player.Body.Velocity.Y:0.00} {_scene.Player.Body.Velocity.Z:0.00})");
        ImGuiNET.ImGui.Text($"FOV: {camera.Zoom:0.00}");
        ImGuiNET.ImGui.Text($"Camera distance: {window.FollowCamera.Distance:0.00}");
        ImGuiNET.ImGui.Text($"Yaw: {camera.Yaw:0.00}");
        ImGuiNET.ImGui.Text($"Pitch: {camera.Pitch:0.00}");

        ImGuiNET.ImGui.NewLine();

        ImGuiNET.ImGui.Checkbox("Show colliders", ref showColliders);

        ImGuiNET.ImGui.End();

        Controller.Render();

        _scene.ShowColliders = showColliders;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Controller?.Dispose();
        Controller = null;
        GC.SuppressFinalize(this);
    }
}

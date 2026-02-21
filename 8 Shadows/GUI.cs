using System.Numerics;
using Engine;
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

    public void Update(float elapsedTime)
    {
        if (_isDisposed || _controller == null) return;

        _controller.Update(elapsedTime);
    }

    public void Render(Window window, Camera camera)
    {
        if (_isDisposed || _controller == null) return;

        var showDepthMapDebug = _scene.ShowDepthMapDebug;
        var freezeCsm = _scene.FreezeCsm;
        var showCascadeColors = _scene.ShowCascadeColors;
        var debugCascadeIndex = _scene.DebugCascadeIndex;
        var sunAzimuth = _scene.SunAzimuthDeg;
        var sunElevation = _scene.SunElevationDeg;

        ImGuiNET.ImGui.SetNextWindowSize(new Vector2(255, 267), ImGuiNET.ImGuiCond.FirstUseEver);
        ImGuiNET.ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiNET.ImGuiCond.FirstUseEver);

        ImGuiNET.ImGui.Begin("Physics Sandbox", ImGuiNET.ImGuiWindowFlags.NoMove);
        ImGuiNET.ImGui.Text($"FPS: {window.FPS:0}");

        ImGuiNET.ImGui.SeparatorText("Camera");
        ImGuiNET.ImGui.Text($"Position: X {camera.Position.X:0.00}  Y {camera.Position.Y:0.00}  Z {camera.Position.Z:0.00}");
        ImGuiNET.ImGui.Text($"Yaw: {camera.Yaw:0.00}");
        ImGuiNET.ImGui.Text($"Pitch: {camera.Pitch:0.00}");
        ImGuiNET.ImGui.Text($"FOV: {camera.Zoom:0.00}");

        ImGuiNET.ImGui.SeparatorText("Shadows");
        ImGuiNET.ImGui.SliderFloat("Sun azimuth", ref sunAzimuth, -180f, 180f);
        ImGuiNET.ImGui.SliderFloat("Sun elevation", ref sunElevation, -10f, 89f);
        ImGuiNET.ImGui.Checkbox("Freeze CSM", ref freezeCsm);
        ImGuiNET.ImGui.Checkbox("Debug mode", ref showDepthMapDebug);
        if (showDepthMapDebug)
        {
            ImGuiNET.ImGui.SliderInt("Debug cascade", ref debugCascadeIndex, 0, Scene.MaxCascadeIndex);
        }
        ImGuiNET.ImGui.Checkbox("Show cascade colors", ref showCascadeColors);

        ImGuiNET.ImGui.End();

        _controller.Render();

        _scene.ShowDepthMapDebug = showDepthMapDebug;
        _scene.FreezeCsm = freezeCsm;
        _scene.ShowCascadeColors = showCascadeColors;
        _scene.DebugCascadeIndex = debugCascadeIndex;
        _scene.SunAzimuthDeg = sunAzimuth;
        _scene.SunElevationDeg = sunElevation;
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

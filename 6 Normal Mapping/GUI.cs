using System.Numerics;
using Engine;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace NormalMapping;

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

        var cameraPosition = camera.Position;
        var cameraPitch = camera.Pitch;
        var cameraYaw = camera.Yaw;

        var mat = _scene.PlaneMaterial;
        mat.TryGetProperty("Shininess", out float shininess);
        mat.TryGetProperty("Ambient", out Vector3 ambient);
        mat.TryGetProperty("Diffuse", out Vector3 diffuse);
        mat.TryGetProperty("SpecularStrength", out float specularStrength);
        _scene.LightMaterial.TryGetProperty("Color", out Vector3 color);

        ImGuiNET.ImGui.SetNextWindowSize(new Vector2(372, 278), ImGuiNET.ImGuiCond.FirstUseEver);
        ImGuiNET.ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiNET.ImGuiCond.FirstUseEver);

        ImGuiNET.ImGui.Begin("Lighting Settings", ImGuiNET.ImGuiWindowFlags.NoMove);
        ImGuiNET.ImGui.Text($"FPS: {window.FPS:0}");

        ImGuiNET.ImGui.SeparatorText("Camera");
        ImGuiNET.ImGui.DragFloat3(
            "Position",
            ref cameraPosition,
            0.1f,
            float.MinValue,
            float.MinValue,
            "%.3f",
            ImGuiNET.ImGuiSliderFlags.NoInput
        );
        ImGuiNET.ImGui.DragFloat(
            "Pitch",
            ref cameraPitch,
            0.1f,
            -89.999f,
            89.999f,
            "%.3f",
            ImGuiNET.ImGuiSliderFlags.NoInput
        );
        ImGuiNET.ImGui.DragFloat(
            "Yaw",
            ref cameraYaw,
            0.1f,
            -360.0f,
            360.0f,
            "%.3f",
            ImGuiNET.ImGuiSliderFlags.NoInput
        );

        ImGuiNET.ImGui.SeparatorText("Light");
        ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 8f, 256f);
        ImGuiNET.ImGui.SliderFloat3("Ambient", ref ambient, 0f, 0.2f);
        ImGuiNET.ImGui.SliderFloat3("Diffuse", ref diffuse, 0f, 3f);
        ImGuiNET.ImGui.SliderFloat3("Color", ref color, 0f, 1f);
        ImGuiNET.ImGui.SliderFloat("SpecularStrength", ref specularStrength, 0f, 2.5f);

        ImGuiNET.ImGui.End();

        _controller.Render();

        mat.SetProperty("Shininess", shininess);
        mat.SetProperty("Ambient", ambient);
        mat.SetProperty("Diffuse", diffuse);
        mat.SetProperty("Color", color);
        mat.SetProperty("SpecularStrength", specularStrength);
        _scene.LightMaterial.SetProperty("Color", color);
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

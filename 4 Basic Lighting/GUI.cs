using System.Numerics;
using Engine;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace BasicLighting;

public sealed class GUI(GL gl, IWindow window, IInputContext input, Scene scene) : IDisposable
{
    private bool _isDisposed = false;
    private ImGuiController? _controller = new(gl, window, input);
    private readonly Scene _scene = scene;

    public void Update(float elapsedTime)
    {
        if (_isDisposed || _controller == null) return;

        _controller.Update(elapsedTime);
    }

    public void Render(Camera camera)
    {
        if (_isDisposed || _controller == null) return;

        var cameraPosition = camera.Position;
        var cameraPitch = camera.Pitch;
        var cameraYaw = camera.Yaw;

        _scene.PlaneMaterial.TryGetProperty("Shininess", out float shininess);

        ImGuiNET.ImGui.SetNextWindowSize(new Vector2(295, 517), ImGuiNET.ImGuiCond.FirstUseEver);
        ImGuiNET.ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiNET.ImGuiCond.FirstUseEver);

        ImGuiNET.ImGui.Begin("Lighting Settings", ImGuiNET.ImGuiWindowFlags.NoMove);

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
        ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 1f, 256f);
        for (int i = 0; i < _scene.Lights.Count; i++)
        {
            var light = _scene.Lights[i];
            float ambient = light.Ambient;
            float diffuse = light.Diffuse;
            float specular = light.Specular;

            ImGuiNET.ImGui.Text($"Light {i + 1}");
            ImGuiNET.ImGui.SliderFloat($"Ambient_{i + 1}", ref ambient, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat($"Diffuse_{i + 1}", ref diffuse, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat($"Specular_{i + 1}", ref specular, 0f, 1f);

            light.Ambient = ambient;
            light.Diffuse = diffuse;
            light.Specular = specular;
        }

        ImGuiNET.ImGui.End();

        _controller.Render();

        _scene.PlaneMaterial.SetProperty("Shininess", shininess);
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

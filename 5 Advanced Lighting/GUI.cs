using System.Numerics;
using Engine;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace AdvancedLighting;

public class GUI : IDisposable
{
    public ImGuiController Controller { get; set; }

    private bool _isDisposed = false;

    public GUI(GL gl, IWindow window, IInputContext input)
    {
        Controller = new ImGuiController(gl, window, input);
    }

    public void Update(float elapsedTime)
    {
        if (_isDisposed || Controller == null) return;
        Controller.Update(elapsedTime);
    }

    public void Render(Camera camera)
    {
        if (_isDisposed || Controller == null) return;

        var cameraPosition = camera.Position;
        var cameraPitch = camera.Pitch;
        var cameraYaw = camera.Yaw;

        var shininess = Scene.Shininess;
        var gamma = Scene.Gamma;
        var useBlinnPhong = Scene.UseBlinnPhong;
        var isPaused = Scene.IsPaused;

        ImGuiNET.ImGui.SetNextWindowSize(new Vector2(340, 200), ImGuiNET.ImGuiCond.FirstUseEver);
        ImGuiNET.ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiNET.ImGuiCond.FirstUseEver);

        ImGuiNET.ImGui.Begin("Lighting Settings", ImGuiNET.ImGuiWindowFlags.NoMove);
        ImGuiNET.ImGui.DragFloat3(
            "Camera Pos",
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
        ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 1f, 256f);
        ImGuiNET.ImGui.SliderFloat("Gamma", ref gamma, 1.0f, 5f);
        ImGuiNET.ImGui.Checkbox("Use Bling-Phong", ref useBlinnPhong);
        if (ImGuiNET.ImGui.Button("Pause")) {
            Scene.IsPaused = !isPaused;
        }

        ImGuiNET.ImGui.End();

        Controller.Render();

        Scene.Shininess = shininess;
        Scene.Gamma = gamma;
        Scene.UseBlinnPhong = useBlinnPhong;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Controller?.Dispose();
        Controller = null;
    }
}

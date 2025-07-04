using System.Numerics;
using Engine;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Collision;

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

    public void Render(Window window, Camera camera)
    {
        if (_isDisposed || Controller == null) return;

        var cameraPosition = camera.Position;
        var cameraPitch = camera.Pitch;
        var cameraYaw = camera.Yaw;

        var shininess = Scene.Shininess;
        var ambient = Scene.Ambient;
        var specular = Scene.Specular;

        ImGuiNET.ImGui.SetNextWindowSize(new Vector2(325, 190), ImGuiNET.ImGuiCond.FirstUseEver);
        ImGuiNET.ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiNET.ImGuiCond.FirstUseEver);

        ImGuiNET.ImGui.Begin("Lighting Settings", ImGuiNET.ImGuiWindowFlags.NoMove);
        ImGuiNET.ImGui.Text($"FPS: {window.FPS:0}");
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
        ImGuiNET.ImGui.SliderFloat("Ambient", ref ambient, 0f, 1f);
        ImGuiNET.ImGui.SliderFloat("Specular", ref specular, 0f, 1f);

        ImGuiNET.ImGui.End();

        Controller.Render();

        Scene.Shininess = shininess;
        Scene.Ambient = ambient;
        Scene.Specular = specular;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Controller?.Dispose();
        Controller = null;
    }
}

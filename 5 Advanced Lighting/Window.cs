using System.Numerics;

namespace AdvancedLighting;

public class Window : Engine.Window
{
    public GUI GUI { get; set; }
    public Scene Scene { get; set; }

    public Window() : base()
    {
        WindowState.Title = "Advanced Lighting";
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GUI = new GUI(GL, WindowState, InputContext);
        Camera.Position = new Vector3(-1.5f, 2f, -1.5f);
        Camera.Pitch = -40f;
        Camera.Yaw = 33f;
        Camera.UpdateVectors();
        Scene = new Scene(GL);
    }

    protected override void OnUpdate(double elapsedTime)
    {
        base.OnUpdate(elapsedTime);

        Scene?.Update((float)elapsedTime);
        GUI?.Update((float)elapsedTime);
    }

    protected override void OnRender(double elapsedTime)
    {
        base.OnRender(elapsedTime);

        Scene.Draw(GL, Camera, WindowState.Time);
        GUI.Render(Camera);
    }

    protected override void OnClose()
    {
        base.OnClose();

        GUI?.Dispose();
        Scene?.Dispose();
    }
}

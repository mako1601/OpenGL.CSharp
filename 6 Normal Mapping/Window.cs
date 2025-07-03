using System.Numerics;

namespace NormalMapping;

public class Window : Engine.Window
{
    public GUI GUI { get; set; }
    public Scene Scene { get; set; }

    public Window() : base()
    {
        WindowState.Title = "Normal Mapping";
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GUI = new GUI(GL, WindowState, InputContext);
        Camera.Position = new Vector3(-2.5f, 2.5f, -4f);
        Camera.Pitch = -35f;
        Camera.Yaw = 61f;
        Camera.UpdateVectors();
        Scene = new Scene(GL);
    }

    protected override void OnUpdate(double elapsedTime)
    {
        base.OnUpdate(elapsedTime);

        GUI?.Update((float)elapsedTime);
    }

    protected override void OnRender(double elapsedTime)
    {
        base.OnRender(elapsedTime);

        Scene.Draw(GL, Camera, WindowState.Time);
        GUI.Render(this, Camera);
    }

    protected override void OnClose()
    {
        base.OnClose();

        GUI?.Dispose();
        Scene?.Dispose();
    }
}

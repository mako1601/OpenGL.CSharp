using System.Numerics;

namespace Collision;

public class Window : Engine.Window
{
    public GUI GUI { get; set; }
    public Scene Scene { get; set; }

    public Window() : base()
    {
        WindowState.Title = "Collision";
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GUI = new GUI(GL, WindowState, InputContext);
        Camera.Position = new Vector3(0f, 1.5f, -2.5f);
        Camera.Pitch = 0f;
        Camera.Yaw = 90f;
        Camera.Speed = 2f;
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

        Scene.Draw(GL, Camera, (float)elapsedTime);
        GUI.Render(this, Camera);
    }

    protected override void OnClose()
    {
        base.OnClose();

        GUI?.Dispose();
        Scene?.Dispose();
    }
}

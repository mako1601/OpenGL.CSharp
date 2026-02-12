using System.Numerics;

namespace BasicLighting;

public class Window : Engine.Window
{
    public GUI GUI { get; set; }
    public Scene Scene { get; set; }

    public Window() : base()
    {
        WindowState.Title = "Basic Lighting";
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        Camera.Position = new Vector3(5f, 3f, -3.7f);
        Camera.Pitch = -25f;
        Camera.Yaw = 140f;
        Camera.UpdateVectors();
        Scene = new Scene(GL);
        GUI = new GUI(GL, WindowState, InputContext, Scene);
    }

    protected override void OnUpdate(double elapsedTime)
    {
        base.OnUpdate(elapsedTime);

        GUI?.Update((float)elapsedTime);
    }

    protected override void OnRender(double elapsedTime)
    {
        base.OnRender(elapsedTime);

        Scene.Draw(GL, Camera);
        GUI.Render(Camera);
    }

    protected override void OnClose()
    {
        base.OnClose();

        GUI?.Dispose();
        Scene?.Dispose();
    }
}

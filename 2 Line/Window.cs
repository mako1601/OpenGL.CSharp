using System.Numerics;

namespace Line;

public class Window : Engine.Window
{
    public Scene Scene { get; set; }

    public Window() : base()
    {
        WindowState.Title = "Line";
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        Scene = new Scene(GL);
        Camera.Position = new Vector3(2f, 2.5f, 0.7f);
        Camera.Pitch = -44f;
        Camera.Yaw = 179f;
        Camera.UpdateVectors();
    }

    protected override void OnRender(double elapsedTime)
    {
        base.OnRender(elapsedTime);

        Scene.Draw(GL, Camera);
    }

    protected override void OnClose()
    {
        base.OnClose();

        Scene?.Dispose();
    }
}

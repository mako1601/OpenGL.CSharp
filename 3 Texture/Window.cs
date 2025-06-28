using System.Numerics;

namespace Texture;

public class Window : Engine.Window
{
    public Scene Scene { get; set; }

    public Window() : base()
    {
        WindowState.Title = "Texture";
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        Camera.Position = new Vector3(2f, 2.5f, 0.7f);
        Camera.Pitch = -44f;
        Camera.Yaw = 179f;
        Camera.UpdateVectors();
        Scene = new Scene(GL);
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

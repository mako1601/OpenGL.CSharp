using System.Numerics;
using Silk.NET.Input;

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

    protected override void OnMouseDown(IMouse mouse, MouseButton button)
    {
        base.OnMouseDown(mouse, button);

        if (MouseState.Cursor.CursorMode == CursorMode.Raw && button == MouseButton.Left)
        {
            var transform = new Engine.Physics.Utilities.Transform
            {
                Position = Camera.Position + Camera.Front * 0.5f,
                Scale = new Vector3(0.1f)
            };

            var obj = new Engine.Physics.Core.PhysicsObject(
                transform,
                new Engine.Physics.Core.Rigidbody
                {
                    Mass = 0.05f,
                    UseGravity = true,
                    Restitution = 0.7f,
                    Friction = 0.4f,
                },
                new Engine.Physics.Colliders.SphereCollider(transform, new Vector3(1f))
            );

            obj.Rigidbody.AddForce(Camera.Front * 100f);
            Scene.Objects.Add(obj);
            Scene.PhysicsWorld.AddObject(obj);
        }
    }

    protected override void OnKeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        base.OnKeyDown(keyboard, key, arg3);

        if (key == Key.X)
        {
            foreach (var obj in Scene.Objects)
            {
                obj.Rigidbody.Gravity = -obj.Rigidbody.Gravity;
            }
        }
    }
}

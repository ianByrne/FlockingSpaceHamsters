using Godot;
using System;

public class MainCamera : Camera
{
    private const float cameraSpeed = 10.0f;
    private const float spinSpeed = 0.1f;
    private const float mouseSensitivity = 0.1f;
    private const float mouseSmoothness = 0.5f;

    private Vector2 mousePosition = new Vector2(0,0);
    private float yaw = 0.0f;
    private float totalYaw = 0.0f;
    private float pitch = 0.0f;
    private float totalPitch = 0.0f;


    public override void _PhysicsProcess(float delta)
    {
        ProcessKeyInput(delta);
        ProcessMouseInput(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if(@event is InputEventMouseMotion)
        {
            InputEventMouseMotion mouseEvent = (InputEventMouseMotion)@event;

            mousePosition = mouseEvent.Relative;
        }
    }

    private void ProcessKeyInput(float delta)
    {
        Vector3 velocity = new Vector3(0,0,0);

        if(Input.IsActionPressed("move_forward"))
        {
            velocity.z -= cameraSpeed;
        }

        if(Input.IsActionPressed("move_back"))
        {
            velocity.z += cameraSpeed;
        }

        if(Input.IsActionPressed("strafe_left"))
        {
            velocity.x -= cameraSpeed;
        }

        if(Input.IsActionPressed("strafe_right"))
        {
            velocity.x += cameraSpeed;
        }

        base.Translate(velocity * delta);
    }

    private void ProcessMouseInput(float delta)
    {
        mousePosition *= mouseSensitivity;
        yaw = yaw * mouseSmoothness + mousePosition.x * (1.0f - mouseSmoothness);
        pitch = pitch * mouseSmoothness + mousePosition.y * (1.0f - mouseSmoothness);
        mousePosition = new Vector2(0,0);

        yaw = Mathf.Clamp(yaw, -360 - totalYaw, 360 - totalYaw);
        pitch = Mathf.Clamp(pitch, -90 - totalPitch, 90 - totalPitch);

        totalYaw += yaw;
        totalPitch += pitch;

        RotateY(Mathf.Deg2Rad(-yaw));
        RotateObjectLocal(new Vector3(1,0,0), Mathf.Deg2Rad(-pitch));
    }
}

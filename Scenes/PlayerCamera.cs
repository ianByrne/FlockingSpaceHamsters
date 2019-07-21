using Godot;
using System;

public class PlayerCamera : Camera
{
    private const float distance = 7.0f;
    private const float height = 3.5f;

    public override void _Ready()
    {
        // SetAsToplevel(true);
        // this.Current = true;
    }

    public override void _PhysicsProcess(float delta)
    {
        var target = ((Player)GetParent()).GetGlobalTransform().origin;
        var pos = this.GlobalTransform.origin;
        var up = Vector3.Up;

        var offset = pos - target;
        offset = offset.Normalized() * distance;
        offset.y = height;

        pos = target + offset;

        LookAtFromPosition(pos, target, up);
    }
}

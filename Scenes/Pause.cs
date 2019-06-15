using Godot;
using System;

public class Pause : Control
{
    public override void _UnhandledInput(InputEvent @event)
    {
        if(@event.IsActionPressed("ui_pause"))
        {
            bool paused = !this.GetTree().Paused;
            this.GetTree().Paused = paused;
            this.Visible = paused;
        }
    }
}

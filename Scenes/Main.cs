using Godot;
using System;

public class Main : Spatial
{
    public override void _Ready()
    {
        // Prevent the mouse cursor from running off screen (and also hide it)
        Input.SetMouseMode(Input.MouseMode.Captured);
    }

    public override void _Process(float delta)
    {
        ProcessInput();
    }

    private void ProcessInput()
    {
        if(Input.IsActionPressed("exit"))
        {
            GetTree().Quit();
        }
    }
}
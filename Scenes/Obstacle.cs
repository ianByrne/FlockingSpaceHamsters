using Godot;
using System;

public class Obstacle : StaticBody
{
    public override void _Ready()
    {
        this.AddToGroup("obstacles");
    }
}

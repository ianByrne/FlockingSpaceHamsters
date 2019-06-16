using Godot;
using System;

public class Main : Spatial
{
    private PackedScene critterScene;

    public override void _Ready()
    {
        // Prevent the mouse cursor from running off screen (and also hide it)
        Input.SetMouseMode(Input.MouseMode.Captured);

        // Spawn some critters
        int critterCount = 20;

        Random rnd = new Random();
        critterScene = (PackedScene)ResourceLoader.Load("res://Scenes/Critter.tscn");

        for (int i = 0; i < critterCount; ++i)
        {
            Critter critter = (Critter)critterScene.Instance();

            // Vector3 position = new Vector3(0,0,0);
            Vector3 position = new Vector3();
            position.x = 0.5f + rnd.Next(-50, 50);
            position.y = 0.5f + rnd.Next(-50, 50);
            position.z = 0.5f + rnd.Next(-50, 50);

            // Vector3 linearVelocity = new Vector3(-1,-1,-1);
            Vector3 linearVelocity = new Vector3();
            linearVelocity.x = 0.5f + rnd.Next(-5, 5);
            linearVelocity.y = 0.5f + rnd.Next(-5, 5);
            linearVelocity.z = 0.5f + rnd.Next(-5, 5);

            Transform transform = Transform.Identity;
            transform = transform.Translated(position);
            // transform = transform.LookingAt(new Vector3(1,0,0), Vector3.Up);
            transform = transform.LookingAt(position + linearVelocity, Vector3.Up);

            critter.SetTransform(transform);
            critter.SetLinearVelocity(linearVelocity);
            critter.PauseMode = PauseModeEnum.Stop;

            CallDeferred("add_child", critter);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if(@event.IsActionPressed("exit"))
        {
            GetTree().Quit();
        }
    }
}
using Godot;
using System;

public class Main : Spatial
{
    private PackedScene critterScene;
    private PackedScene obstacleScene;

    public override void _Ready()
    {
        // Prevent the mouse cursor from running off screen (and also hide it)
        Input.SetMouseMode(Input.MouseMode.Captured);

        // Spawn some critters and obstacles
        int critterCount = 70;
        int obstacleCount = 20;

        Random rnd = new Random();
        critterScene = (PackedScene)ResourceLoader.Load("res://Scenes/Critter.tscn");
        obstacleScene = (PackedScene)ResourceLoader.Load("res://Scenes/Obstacle.tscn");

        for (int i = 0; i < obstacleCount; ++i)
        {
            Obstacle obstacle = (Obstacle)obstacleScene.Instance();

            Vector3 position = new Vector3();
            position.x = 0.5f + rnd.Next(-45, 45);
            position.y = 0.5f + rnd.Next(-45, 45);
            position.z = 0.5f + rnd.Next(-45, 45);

            Vector3 rotationAxis = new Vector3();
            rotationAxis.x = rnd.Next(-1, 1);
            rotationAxis.y = rnd.Next(-1, 1);
            rotationAxis.z = rnd.Next(-1, 1);

            float rotationAngle = rnd.Next(0, 90);

            Transform transform = Transform.Identity;
            transform = transform.Translated(position);
            transform = transform.Rotated(rotationAxis, rotationAngle);

            obstacle.SetTransform(transform);

            CallDeferred("add_child", obstacle);
        }

        for (int i = 0; i < critterCount; ++i)
        {
            Critter critter = (Critter)critterScene.Instance();

            // Vector3 position = new Vector3(0,0,5);
            Vector3 position = new Vector3();
            position.x = 0.5f + rnd.Next(-45, 45);
            position.y = 0.5f + rnd.Next(-45, 45);
            position.z = 0.5f + rnd.Next(-45, 45);

            // Vector3 linearVelocity = new Vector3(0,0,1);
            Vector3 linearVelocity = new Vector3();
            linearVelocity.x = 0.5f + rnd.Next(-5, 5);
            linearVelocity.y = 0.5f + rnd.Next(-5, 5);
            linearVelocity.z = 0.5f + rnd.Next(-5, 5);

            Transform transform = Transform.Identity;
            transform = transform.Translated(position);
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
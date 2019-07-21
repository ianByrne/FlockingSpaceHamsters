using Godot;
using System;

public class Main : Spatial
{
    public override void _Ready()
    {
        // Prevent the mouse cursor from running off screen (and also hide it)
        Input.SetMouseMode(Input.MouseMode.Captured);

        // Spawn the player
        // PackedScene playerScene = (PackedScene)ResourceLoader.Load("res://Scenes/Player.tscn");
        // Player player = (Player)playerScene.Instance();
        // player.SetName("player");

        // Vector3 playerPosition = new Vector3(50,0,0);

        // Transform playerTransform = Transform.Identity;
        // playerTransform = playerTransform.Translated(playerPosition);
        // playerTransform = playerTransform.LookingAt(Vector3.Zero, Vector3.Up);

        // player.SetTransform(playerTransform);
        // player.PauseMode = PauseModeEnum.Stop;

        // CallDeferred("add_child", player);

        // Spawn some critters and obstacles
        int critterCount = 20;

        Vector3[] obstaclePositions = new Vector3[6] {
            new Vector3(50,0,0),
            new Vector3(20,10,10),
            new Vector3(-20,-10,10),
            new Vector3(60,10,-10),
            new Vector3(0,0,50),
            new Vector3(-10,10,60)
        };

        Random rnd = new Random();
        PackedScene critterScene = (PackedScene)ResourceLoader.Load("res://Scenes/Critter.tscn");
        PackedScene obstacleScene = (PackedScene)ResourceLoader.Load("res://Scenes/Obstacle.tscn");

        for (int i = 0; i < obstaclePositions.Length; ++i)
        {
            Obstacle obstacle = (Obstacle)obstacleScene.Instance();

            Vector3 position = obstaclePositions[i];
            // position.x = 0.5f + rnd.Next(-45, 45);
            // position.y = 0.5f + rnd.Next(-45, 45);
            // position.z = 0.5f + rnd.Next(-45, 45);

            // Vector3 rotationAxis = new Vector3();
            // rotationAxis.x = rnd.Next(-1, 1);
            // rotationAxis.y = rnd.Next(-1, 1);
            // rotationAxis.z = rnd.Next(-1, 1);

            float rotationAngle = rnd.Next(0, 90);

            Transform transform = Transform.Identity;
            transform = transform.Translated(position);
            // transform = transform.Rotated(rotationAxis, rotationAngle);

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
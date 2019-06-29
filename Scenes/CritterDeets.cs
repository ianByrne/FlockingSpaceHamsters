using Godot;
using System;

public class CritterDeets : Control
{
    private MainCamera camera;
    private Label label;
    private Critter critter;

    public override void _Ready()
    {
        camera = (MainCamera)this.GetNode("/root/Main/MainCamera");
        label = (Label)GetNode("Label");
    }

    public override void _Process(float delta)
    {
        if(Input.IsActionJustPressed("follow"))
        {
            camera.FollowCritter = !camera.FollowCritter;
        }

        if(critter != null)
        {
            string text = "";
            text += $"CloseNeighbours/Neighbours: {critter.CloseNeighbourCount}/{critter.NeighbourCount}";
            text += $"\nPosition: {critter.Transform.origin}";
            text += $"\nAlignment: {critter.Alignment}";
            text += $"\nCohesion: {critter.Cohesion}";
            text += $"\nSeparation: {critter.Separation}";
            text += $"\nComeBack: {critter.ComeBack}";

            label.SetText(text);

            if(camera.FollowCritter)
            {
                camera.TargetTransform = critter.Transform;
            }
        }
        else
        {
            label.SetText("");
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed && eventMouseButton.ButtonIndex == 1)
        {
            var from = camera.ProjectRayOrigin(eventMouseButton.Position);
            var to = from + camera.ProjectRayNormal(eventMouseButton.Position) * 500;

            var spaceState = camera.GetWorld().DirectSpaceState;
            var result = spaceState.IntersectRay(from, to);

            if(result.ContainsKey("collider"))
            {
                critter = result["collider"] as Critter;

                if(critter != null)
                {
                    critter.SetSelected(true);
                }
            }
            else if(critter != null)
            {
                critter.SetSelected(false);
                critter = null;
            }
        }
    }
}

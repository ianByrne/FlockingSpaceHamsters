using Godot;

public class Neighbour
{
    public Vector3 WorldPosition { get; set; } = Vector3.Zero;
    public Vector3 LocalHeading { get; set; } = Vector3.Zero;

    public Neighbour(Critter critter)
    {
        this.WorldPosition = critter.Transform.origin;
        this.LocalHeading = -critter.Transform.basis.z;
    }
}

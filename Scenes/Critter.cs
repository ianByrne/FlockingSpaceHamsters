using Godot;
using System;
using System.Collections.Generic;

// Still need to sort out separation force/position. It seems it might
// be trying to set a point near the origin, perhaps, and it's just therefore
// adding zero to the final position. Maybe.

// Currently trying to 'select' a critter. There is a _InputEvent method which
// apparently does what I need, but can't get it working - it seems to get consumed before
// it makes it to the critter class (should inputs be handled by the classes?
// Or move it into some kind of root 'handle input' thing? _Input vs _UnhandledInput?)
// So far the "select_critter" control has been mapped to right click.
// https://docs.godotengine.org/en/3.1/tutorials/physics/ray-casting.html

public class Critter : RigidBody
{
    public bool Selected
    {
        get
        {
            return this.GetNode<MeshInstance>("perceptionBubbleMesh").IsVisible();
        }
        set
        {
            if(value)
            {
                this.GetNode<MeshInstance>("perceptionBubbleMesh").SetVisible(true);
                this.GetNode<MeshInstance>("closePerceptionBubbleMesh").SetVisible(true);
                this.GetNode<ImmediateGeometry>("desiredPositionLine").SetVisible(true);
            }
            else
            {
                this.GetNode<MeshInstance>("perceptionBubbleMesh").SetVisible(false);
                this.GetNode<MeshInstance>("closePerceptionBubbleMesh").SetVisible(false);
                this.GetNode<ImmediateGeometry>("desiredPositionLine").SetVisible(false);
            }
        }
    }

    public Vector3 Cohesion { get; private set; }
    public Vector3 Alignment { get; private set; }
    public Vector3 Separation { get; private set; }
    public Vector3 ComeBack { get; private set; }
    public int NeighbourCount { get; private set; }
    public int CloseNeighbourCount { get; private set; }

    private const float maxSpeed = 10.0f;
    private const float maxForce = 0.01f;
    private const float perceptionRadius = 15.0f;
    private const float closePerceptionRadius = 7.0f;
    private const float perceptionRadius2 = perceptionRadius * perceptionRadius;
    private const float closePerceptionRadius2 = closePerceptionRadius * closePerceptionRadius;

    public override void _Ready()
    {
        this.AddToGroup("critters");

        this.SetAngularDamp(0.8f);

        // Create perception bubble mesh
        var materialPerception = new SpatialMaterial();
        materialPerception.FlagsTransparent = true;
        materialPerception.AlbedoColor = new Color(0.0f, 1.0f, 0.0f, 0.02f);

        var sphereMeshPerception = new SphereMesh();
        sphereMeshPerception.Material = materialPerception;
        sphereMeshPerception.Radius = perceptionRadius;
        sphereMeshPerception.Height = perceptionRadius * 2.0f;

        var meshInstancePerception = new MeshInstance();
        meshInstancePerception.Mesh = sphereMeshPerception;
        meshInstancePerception.SetName("perceptionBubbleMesh");
        meshInstancePerception.SetVisible(false);

        this.AddChild(meshInstancePerception);
        
        // Create close perception bubble mesh
        var materialClosePerception = new SpatialMaterial();
        materialClosePerception.FlagsTransparent = true;
        materialClosePerception.AlbedoColor = new Color(1.0f, 0.0f, 0.0f, 0.02f);

        var sphereMesh = new SphereMesh();
        sphereMesh.Material = materialClosePerception;
        sphereMesh.Radius = closePerceptionRadius;
        sphereMesh.Height = closePerceptionRadius * 2.0f;

        var meshInstanceClosePerception = new MeshInstance();
        meshInstanceClosePerception.Mesh = sphereMesh;
        meshInstanceClosePerception.SetName("closePerceptionBubbleMesh");
        meshInstanceClosePerception.SetVisible(false);

        this.AddChild(meshInstanceClosePerception);

        // Create desired position line
        var desiredPositionLine = new ImmediateGeometry();
        // desiredPositionLine.SetColor(new Color(1,0,0,1)); // This doesn't seem to work
        desiredPositionLine.SetName("desiredPositionLine");
        desiredPositionLine.SetVisible(false);

        this.AddChild(desiredPositionLine);
    }
    
    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        IList<Neighbour> neighbours = GetNeighbours();
        
        // Turn critter to point in appropriate direction
        // desiredPosition starts a point directly in front of the critter in world coordinates
        var desiredPosition = state.Transform.origin - state.Transform.basis.z;
        desiredPosition += GetComeBackDesiredPosition(state.Transform.origin);
        desiredPosition += GetFlockingDesiredPosition(state.Transform.origin, neighbours);

        var currentQuat = state.Transform.basis.Quat();
        var headingQuat = state.Transform.LookingAt(desiredPosition, Vector3.Up).basis.Quat();
        var newQuat = currentQuat.Slerp(headingQuat, maxForce);

        var trans = new Transform(new Basis(newQuat), state.Transform.origin);

        state.SetTransform(trans);

        // Push critter in direction that it's facing
        var forwardForce = GetForwardForce(-state.Transform.basis.z, state.LinearVelocity);
        state.ApplyCentralImpulse(forwardForce);

        // Set the desired position line
        // Lines are drawn in local space (the line is a child of the critter)
        var desiredPositionLine = this.GetNode<ImmediateGeometry>("desiredPositionLine");
        desiredPositionLine.Clear();
        desiredPositionLine.Begin(Mesh.PrimitiveType.Lines);
        desiredPositionLine.AddVertex(Vector3.Zero);
        desiredPositionLine.AddVertex(state.Transform.XformInv(desiredPosition)); // To convert from global desiredPosition to local
        desiredPositionLine.End();
    }

    private IList<Neighbour> GetNeighbours()
    {
        var neighbours = new List<Neighbour>();

        var critters = GetTree().GetNodesInGroup("critters");
        
        foreach(Critter critter in critters)
        {
            if(this == critter)
            {
                continue;
            }

            if(critter.Transform.origin.DistanceSquaredTo(this.Transform.origin) <= perceptionRadius2)
            {
                neighbours.Add(new Neighbour()
                {
                    WorldPosition = critter.Transform.origin,
                    LocalHeading = -critter.Transform.basis.z
                });
            }
        }

        return neighbours;
    }

    private Vector3 GetForwardForce(Vector3 heading, Vector3 linearVelocity)
    {
        // Move at max speed
        var forwardForce = SetLength(heading, maxSpeed);

        // Remove current velocity so as to not compound it each loop 
        forwardForce -= linearVelocity;

        return forwardForce;
    }

    private Vector3 GetFlockingDesiredPosition(Vector3 currentPosition, IList<Neighbour> neighbours)
    {
        var flockingDesiredPosition = Vector3.Zero;

        NeighbourCount = 0;
        
        if(neighbours != null)
        {
            NeighbourCount = neighbours.Count;
        }

        if(neighbours != null && NeighbourCount > 0)
        {
            Alignment = Vector3.Zero;
            Cohesion = Vector3.Zero;
            Separation = Vector3.Zero;

            CloseNeighbourCount = 0;

            foreach(var neighbour in neighbours)
            {
                Alignment += currentPosition + neighbour.LocalHeading;
                Cohesion += neighbour.WorldPosition;

                // Close neighbours
                if(currentPosition.DistanceSquaredTo(neighbour.WorldPosition) < closePerceptionRadius2)
                {
                    // Separate - closer neighbours have greater effect
                    Vector3 desiredSeparation = currentPosition - neighbour.WorldPosition;

                    float distance = currentPosition.DistanceSquaredTo(neighbour.WorldPosition);

                    if(distance > 0.0f)
                    {
                        desiredSeparation /= distance;
                    }

                    Separation += desiredSeparation;

                    ++CloseNeighbourCount;
                }
            }

            Alignment /= NeighbourCount;
            Cohesion /= NeighbourCount;
            Cohesion -= currentPosition;

            if(CloseNeighbourCount > 0)
            {
                Separation /= CloseNeighbourCount;
                Separation -= currentPosition;
            }

            // Add all the forces
            flockingDesiredPosition += Alignment;
            flockingDesiredPosition += Cohesion;
            flockingDesiredPosition += Separation * 12.5f;
        }

        return flockingDesiredPosition;
    }

    private Vector3 GetComeBackDesiredPosition(Vector3 currentPosition)
    {
        var desiredPosition = Vector3.Zero;
        
        var attractionPoint = new Vector3(0, 0, 0);

        float distance = currentPosition.DistanceSquaredTo(attractionPoint);

        if(distance > 500.0f)
        {
            desiredPosition = attractionPoint - currentPosition;
        }

        ComeBack = desiredPosition;

        return desiredPosition;
    }

    private Vector3 SetLength(Vector3 vector, float length)
    {
        if(vector.LengthSquared() > 0 && length > 0)
        {
            if(!vector.IsNormalized())
            {
                vector = vector.Normalized() * length;
            }
            else
            {
                vector *= length;
            }
        }

        return vector;
    }
    
    private Vector3 LimitLength(Vector3 vector, float length)
    {
        if(length > 0 && vector.LengthSquared() > length * length)
        {
            vector = SetLength(vector, length);
        }
        
        return vector;
    }
}

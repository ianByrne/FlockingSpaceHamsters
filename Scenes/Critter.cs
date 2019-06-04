using Godot;
using System;
using System.Collections.Generic;

public class Critter : RigidBody
{
    private float maxSpeed = 10.0f;
    private float maxForce = 0.1f;
    private float perceptionRadius = 10.0f;
    private float closePerceptionRadius = 5.5f;

    public override void _Ready()
    {
        this.AddToGroup("critters");

        this.SetAngularDamp(0.8f);

        // Create perception bubble mesh
        var material = new SpatialMaterial();
        material.FlagsTransparent = true;
        material.AlbedoColor = new Color(1.0f, 1.0f, 1.0f, 0.02f);

        var sphereMesh = new SphereMesh();
        sphereMesh.Material = material;
        sphereMesh.Radius = this.perceptionRadius;
        sphereMesh.Height = this.perceptionRadius * 2.0f;

        var meshInstance = new MeshInstance();
        meshInstance.Mesh = sphereMesh;

        this.AddChild(meshInstance);
    }
    
    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        IList<Neighbour> neighbours = GetNeighbours();
        
        // Turn critter to point in appropriate direction
        // desiredPosition starts a point directly in front of the critter in world coordinates
        var desiredPosition = state.Transform.origin - state.Transform.basis.z;
        desiredPosition += GetComeBackDesiredPosition(state.Transform.origin) * 2.5f;
        desiredPosition += GetFlockingDesiredPosition(state.Transform.origin, neighbours);

        var currentQuat = state.Transform.basis.Quat();
        var headingQuat = state.Transform.LookingAt(desiredPosition, Vector3.Up).basis.Quat();
        var newQuat = currentQuat.Slerp(headingQuat, maxForce);

        var trans = new Transform(new Basis(newQuat), state.Transform.origin);

        state.SetTransform(trans);

        // Push critter in direction that it's facing
        var forwardForce = GetForwardForce(-state.Transform.basis.z, state.LinearVelocity);
        state.ApplyCentralImpulse(forwardForce);
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

            if(critter.Transform.origin.DistanceSquaredTo(this.Transform.origin) <= perceptionRadius)
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
        var flockingHeading = Vector3.Zero;

        if(neighbours != null && neighbours.Count > 0)
        {
            var alignment = Vector3.Zero;
            var cohesion = Vector3.Zero;
            var separation = Vector3.Zero;

            int closeNeighbourCount = 0;

            foreach(var neighbour in neighbours)
            {
                // Align
                alignment += currentPosition + neighbour.LocalHeading;

                // Close neighbours
                if(currentPosition.DistanceSquaredTo(neighbour.WorldPosition) < closePerceptionRadius)
                {
                    // Cohere
                    cohesion += neighbour.WorldPosition;

                    // Separate - closer neighbours have greater effect
                    Vector3 desiredSeparation = currentPosition + (currentPosition - neighbour.WorldPosition);

                    float distance = currentPosition.DistanceSquaredTo(neighbour.WorldPosition);

                    if(distance > 0.0f)
                    {
                        desiredSeparation /= distance;
                    }

                    separation += desiredSeparation;

                    ++closeNeighbourCount;
                }
            }

            alignment /= neighbours.Count;

            if(closeNeighbourCount > 0)
            {
                cohesion /= closeNeighbourCount;
                // cohesion = cohesion.Normalized();

                separation /= closeNeighbourCount;
                // separation = separation.Normalized();
            }

            // Add all the forces
            flockingHeading += alignment;
            flockingHeading += cohesion;
            flockingHeading += separation * 2.5f;
        }

        return flockingHeading;
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

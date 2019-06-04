using Godot;
using System;
using System.Collections.Generic;

public class Critter : RigidBody
{
    private float maxSpeed = 16.0f;
    private float maxForce = 0.09f;
    private float perceptionRadius = 150.0f;
    private float closePerceptionRadius = 70.5f;

    public override void _Ready()
    {
        AddToGroup("critters");

        this.SetAngularDamp(0.8f);
    }
    
    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        IList<Critter> neighbours = GetNeighbours();
        
        // Turn critter to point in appropriate direction
        // currentHeading is a point directly in front of the critter in world coordinates
        var currentHeading = state.Transform.origin - state.Transform.basis.z;
        var newHeading = currentHeading;
        newHeading += GetComeBackHeading(currentHeading);
        newHeading += GetFlockingHeading(currentHeading, neighbours);

        var currentQuat = state.Transform.basis.Quat();
        var headingQuat = state.Transform.LookingAt(newHeading, Vector3.Up).basis.Quat();
        var newQuat = currentQuat.Slerp(headingQuat, maxForce);

        Transform trans = new Transform(new Basis(newQuat), state.Transform.origin);

        state.SetTransform(trans);

        // Push critter in direction that it's facing
        Vector3 forwardForce = GetForwardForce(state);
        state.ApplyCentralImpulse(forwardForce);
    }

    private IList<Critter> GetNeighbours()
    {
        IList<Critter> neighbours = new List<Critter>();

        var critters = GetTree().GetNodesInGroup("critters");
        
        foreach(Critter critter in critters)
        {
            if(this == critter)
            {
                continue;
            }

            if(critter.Translation.DistanceSquaredTo(this.Translation) <= perceptionRadius)
            {
                neighbours.Add(critter);
            }
        }

        return neighbours;
    }

    private Vector3 GetForwardForce(PhysicsDirectBodyState state)
    {
        // Move in "heading" direction
        Vector3 forwardForce = -state.Transform.basis.z;

        // At max speed
        forwardForce = SetLength(forwardForce, maxSpeed);

        // Remove current velocity so as to not compound it each loop 
        forwardForce -= state.LinearVelocity;

        return forwardForce;
    }

    private Vector3 GetFlockingHeading(Vector3 currentHeading, IList<Critter> neighbours)
    {
        Vector3 flockingHeading = Vector3.Zero;

        if(neighbours != null && neighbours.Count > 0)
        {
            Vector3 alignment = Vector3.Zero;
            Vector3 cohesion = Vector3.Zero;
            Vector3 separation = Vector3.Zero;

            int closeNeighbourCount = 0;

            foreach(var neighbour in neighbours)
            {
                // Align
                alignment += -neighbour.Transform.basis.z;

                // Close neighbours
                if(this.Transform.origin.DistanceSquaredTo(neighbour.Transform.origin) < closePerceptionRadius)
                {
                    // Cohere
                    cohesion += neighbour.Transform.origin;

                    // Separate - closer neighbours have greater effect
                    Vector3 desiredPosition = this.Transform.origin + (this.Transform.origin - neighbour.Transform.origin);

                    float distance = this.Transform.origin.DistanceSquaredTo(neighbour.Transform.origin);

                    if(distance > 0.0f)
                    {
                        desiredPosition /= distance;
                    }

                    separation += desiredPosition;

                    ++closeNeighbourCount;
                }
            }

            alignment /= neighbours.Count;

            if(closeNeighbourCount > 0)
            {
                cohesion /= closeNeighbourCount;
                cohesion -= currentHeading;
                cohesion = cohesion.Normalized();

                separation /= closeNeighbourCount;
                separation = separation.Normalized();
            }

            // Add all the forces
            flockingHeading += alignment;
            flockingHeading += cohesion;
            flockingHeading += separation;
        }

        return flockingHeading;
    }

    private Vector3 GetComeBackHeading(Vector3 currentHeading)
    {
        var newHeading = Vector3.Zero;
        
        Vector3 attractionPoint = new Vector3(0, 0, 0);

        float distance = this.Translation.DistanceSquaredTo(attractionPoint);

        if(distance > 500.0f)
        {
            newHeading = attractionPoint - currentHeading;
        }

        return newHeading.Normalized();
    }

    private Vector3 SetLength(Vector3 vector, float length)
    {
        if(vector.LengthSquared() > 0 && length > 0)
        {
            vector = vector.Normalized() * length;
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

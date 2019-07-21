using Godot;
using System;

public class Player : RigidBody
{
    private const float maxSpeed = 15.0f;
    private const float maxForce = 0.01f;

    public override void _Ready()
    {
        this.SetAngularDamp(0.8f);
    }

    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        // Turn critter to point in appropriate direction
        // desiredPosition starts a point directly in front of the critter in world coordinates
        var desiredPosition = state.Transform.origin - state.Transform.basis.z;

        if(Input.IsActionPressed("player_up"))
        {
            desiredPosition += state.Transform.basis.y * 5.0f;
        }

        if(Input.IsActionPressed("player_down"))
        {
            desiredPosition -= state.Transform.basis.y * 5.0f;
        }

        if(Input.IsActionPressed("player_left"))
        {
            desiredPosition -= state.Transform.basis.x * 5.0f;
        }

        if(Input.IsActionPressed("player_right"))
        {
            desiredPosition += state.Transform.basis.x * 5.0f;
        }

        var currentQuat = state.Transform.basis.Quat();
        var headingQuat = state.Transform.LookingAt(desiredPosition, Vector3.Up).basis.Quat();

        var newQuat = currentQuat.Slerp(headingQuat, maxForce);

        var trans = new Transform(new Basis(newQuat), state.Transform.origin);

        state.SetTransform(trans);

        // Push critter in direction that it's facing
        var forwardForce = GetForwardForce(-state.Transform.basis.z, state.LinearVelocity);
        state.ApplyCentralImpulse(forwardForce);
    }

    private Vector3 GetForwardForce(Vector3 heading, Vector3 linearVelocity)
    {
        // Move at max speed
        var forwardForce = SetLength(heading, maxSpeed);

        // Remove current velocity so as to not compound it each loop 
        forwardForce -= linearVelocity;

        return forwardForce;
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
}

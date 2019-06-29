using Godot;
using System;
using System.Collections.Generic;

/*
TODO: Add obstacles, sort out mystery crashes and errors, get tighter flocks with less crashes (more responsive to avoidance?)
I don't really trust the current obstacle avoidance or separation logic.
*/

public class Critter : RigidBody
{
    private const float maxSpeed = 10.0f;
    private const float maxForce = 0.01f;
    private const float perceptionRadius = 25.0f;
    private const float closePerceptionRadius = 11.0f;
    private const float perceptionRadius2 = perceptionRadius * perceptionRadius;
    private const float closePerceptionRadius2 = closePerceptionRadius * closePerceptionRadius;

    public Vector3 Cohesion { get; private set; }
    public Vector3 Alignment { get; private set; }
    public Vector3 Separation { get; private set; }
    public Vector3 ComeBack { get; private set; }
    public int NeighbourCount { get { return neighbours.Count; } }
    public int CloseNeighbourCount { get; private set; }

    private IList<Neighbour> neighbours;
    private IList<Vector3> obstacles;

    public Critter()
    {
        neighbours = new List<Neighbour>();
        obstacles = new List<Vector3>();
    }

    public override void _Ready()
    {
        this.AddToGroup("critters");

        this.SetAngularDamp(0.8f);

        CreatePerceptionMesh();
        CreateClosePerceptionMesh();
        CreateDesiredPositionLine();
    }

    public override void _PhysicsProcess(float delta)
    {
        GetNeighboursAndObstacles();
    }
    
    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        // Turn critter to point in appropriate direction
        // desiredPosition starts a point directly in front of the critter in world coordinates
        var desiredPosition = state.Transform.origin - state.Transform.basis.z;
        desiredPosition += GetComeBackDesiredPosition(state.Transform.origin);
        desiredPosition += GetFlockingDesiredPosition(state.Transform.origin, neighbours);
        desiredPosition += GetAvoidObstaclesPosition(state.Transform.origin, obstacles) * 20.5f;

        var currentQuat = state.Transform.basis.Quat();
        var headingQuat = state.Transform.LookingAt(desiredPosition, Vector3.Up).basis.Quat();

        var force = maxForce;
        if(obstacles.Count > 0)
        {
            force = 0.05f;
        }

        var newQuat = currentQuat.Slerp(headingQuat, force);

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

    public void SetSelected(bool selected)
    {
        this.GetNode<MeshInstance>("perceptionMesh").SetVisible(selected);
        this.GetNode<MeshInstance>("closePerceptionBubbleMesh").SetVisible(selected);
        this.GetNode<ImmediateGeometry>("desiredPositionLine").SetVisible(selected);
    }

    private void CreatePerceptionMesh()
    {
        var material = new SpatialMaterial();
        material.FlagsTransparent = true;
        material.AlbedoColor = new Color(0.0f, 1.0f, 0.0f, 0.02f);
        material.SetCullMode(SpatialMaterial.CullMode.Disabled);

        // Pyramid
        var surfaceTool = new SurfaceTool();
        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

        // Top face
        surfaceTool.AddNormal(new Vector3(0.0f, 0.5f, 0.5f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(-perceptionRadius / 2, perceptionRadius / 2, -perceptionRadius)); // top left

        surfaceTool.AddNormal(new Vector3(0.0f, 0.5f, 0.5f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(perceptionRadius / 2, perceptionRadius / 2, -perceptionRadius)); // top right

        surfaceTool.AddNormal(new Vector3(0.0f, 0.5f, 0.5f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(0, 0, 2.0f)); // home

        // Right face
        surfaceTool.AddNormal(new Vector3(0.5f, 0.0f, 0.5f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(perceptionRadius / 2, perceptionRadius / 2, -perceptionRadius)); // top right

        surfaceTool.AddNormal(new Vector3(0.5f, 0.0f, 0.5f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(perceptionRadius / 2, -perceptionRadius / 2, -perceptionRadius)); // bottom right

        surfaceTool.AddNormal(new Vector3(0.5f, 0.0f, 0.5f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(0, 0, 2.0f)); // home

        // Bottom face
        surfaceTool.AddNormal(new Vector3(0.0f, -0.5f, 0.5f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(perceptionRadius / 2, -perceptionRadius / 2, -perceptionRadius)); // bottom right

        surfaceTool.AddNormal(new Vector3(0.0f, -0.5f, 0.5f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(-perceptionRadius / 2, -perceptionRadius / 2, -perceptionRadius)); // bottom left

        surfaceTool.AddNormal(new Vector3(0.0f, -0.5f, 0.5f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(0, 0, 2.0f)); // home

        // Left face
        surfaceTool.AddNormal(new Vector3(-0.5f, 0.0f, 0.5f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(-perceptionRadius / 2, -perceptionRadius / 2, -perceptionRadius)); // bottom left

        surfaceTool.AddNormal(new Vector3(-0.5f, 0.0f, 0.5f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(-perceptionRadius / 2, perceptionRadius / 2, -perceptionRadius)); // top left

        surfaceTool.AddNormal(new Vector3(-0.5f, 0.0f, 0.5f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(0, 0, 2.0f)); // home

        // Base face1
        surfaceTool.AddNormal(new Vector3(0.0f, 0.0f, -1.0f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(perceptionRadius / 2, perceptionRadius / 2, -perceptionRadius)); // top right

        surfaceTool.AddNormal(new Vector3(0.0f, 0.0f, -1.0f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(-perceptionRadius / 2, perceptionRadius / 2, -perceptionRadius)); // top left

        surfaceTool.AddNormal(new Vector3(0.0f, 0.0f, -1.0f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(-perceptionRadius / 2, -perceptionRadius / 2, -perceptionRadius)); // bottom left

        // Base face2
        surfaceTool.AddNormal(new Vector3(0.0f, 0.0f, -1.0f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(-perceptionRadius / 2, -perceptionRadius / 2, -perceptionRadius)); // bottom left

        surfaceTool.AddNormal(new Vector3(0.0f, 0.0f, -1.0f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(perceptionRadius / 2, -perceptionRadius / 2, -perceptionRadius)); // bottom right

        surfaceTool.AddNormal(new Vector3(0.0f, 0.0f, -1.0f));
        surfaceTool.AddColor(new Color(0.0f, 1.0f, 0.0f, 0.02f));
        surfaceTool.AddVertex(new Vector3(perceptionRadius / 2, perceptionRadius / 2, -perceptionRadius)); // top right

        surfaceTool.Index();

        var mesh = surfaceTool.Commit();
        mesh.SurfaceSetMaterial(0, material);

        var meshInstance = new MeshInstance();
        meshInstance.Mesh = mesh;
        meshInstance.SetName("perceptionMesh");
        meshInstance.SetVisible(false);

        this.AddChild(meshInstance);
    }

    private void CreateClosePerceptionMesh()
    {
        var material = new SpatialMaterial();
        material.FlagsTransparent = true;
        material.AlbedoColor = new Color(1.0f, 0.0f, 0.0f, 0.02f);

        var mesh = new SphereMesh();
        mesh.Material = material;
        mesh.Radius = closePerceptionRadius;
        mesh.Height = closePerceptionRadius * 2.0f;

        var meshInstance = new MeshInstance();
        meshInstance.Mesh = mesh;
        meshInstance.SetName("closePerceptionBubbleMesh");
        meshInstance.SetVisible(false);

        this.AddChild(meshInstance);
    }

    private void CreateDesiredPositionLine()
    {
        var line = new ImmediateGeometry();
        // line.SetColor(new Color(1,0,0,1)); // This doesn't seem to work
        line.SetName("desiredPositionLine");
        line.SetVisible(false);

        this.AddChild(line);
    }

    private void GetNeighboursAndObstacles()
    {
        neighbours = new List<Neighbour>();
        obstacles = new List<Vector3>();

        var spaceState = GetWorld().DirectSpaceState;

        var perceptionShape = GetNode("perceptionMesh") as MeshInstance;

        if(perceptionShape == null)
        {
            return;
        }

        PhysicsShapeQueryParameters collisionShape = new PhysicsShapeQueryParameters();
        collisionShape.SetTransform(perceptionShape.GetGlobalTransform());;
        collisionShape.SetShape(perceptionShape.Mesh.CreateConvexShape());

        var result = spaceState.IntersectShape(collisionShape);

        foreach(Godot.Collections.Dictionary collision in result)
        {
            if(collision.ContainsKey("collider"))
            {
                if(collision["collider"] is Critter critter && critter != this)
                {
                    neighbours.Add(new Neighbour(critter));
                    continue;
                }
                else if(collision["collider"] is StaticBody staticBody && staticBody.IsInGroup("obstacles"))
                {
                    // Cast ray to object's origin to find where it hits
                    var rayResult = spaceState.IntersectRay(this.Transform.origin, staticBody.Transform.origin);

                    if(rayResult.Count > 0)
                    {
                        obstacles.Add((Vector3)rayResult["position"]);
                    }
                }
            }
        }
    }

    private Vector3 GetForwardForce(Vector3 heading, Vector3 linearVelocity)
    {
        // Move at max speed
        var forwardForce = SetLength(heading, maxSpeed);

        // Remove current velocity so as to not compound it each loop 
        forwardForce -= linearVelocity;

        return forwardForce;
    }

    private Vector3 GetAvoidObstaclesPosition(Vector3 currentPosition, IList<Vector3> obstacles)
    {
        var avoidObstaclesPosition = Vector3.Zero;

        int obstacleCount = obstacles.Count;

        if(obstacleCount > 0)
        {
            foreach(var obstacle in obstacles)
            {
                avoidObstaclesPosition += currentPosition - obstacle;
            }

            avoidObstaclesPosition /= obstacleCount;
            avoidObstaclesPosition -= currentPosition;
        }

        return avoidObstaclesPosition;
    }

    private Vector3 GetFlockingDesiredPosition(Vector3 currentPosition, IList<Neighbour> neighbours)
    {
        var flockingDesiredPosition = Vector3.Zero;
        
        Alignment = Vector3.Zero;
        Cohesion = Vector3.Zero;
        Separation = Vector3.Zero;

        CloseNeighbourCount = 0;

        if(neighbours != null && NeighbourCount > 0)
        {
            foreach(var neighbour in neighbours)
            {
                Alignment += SetLength(neighbour.LocalHeading, maxSpeed); // Neighbour's heading in local coords
                Cohesion += neighbour.WorldPosition - currentPosition; // Neigbour's position in local coords

                // Close neighbours
                if(currentPosition.DistanceSquaredTo(neighbour.WorldPosition) < closePerceptionRadius2)
                {
                    Separation += currentPosition - neighbour.WorldPosition;

                    ++CloseNeighbourCount;
                }
            }

            Alignment /= NeighbourCount;
            Cohesion /= NeighbourCount;

            if(CloseNeighbourCount > 0)
            {
                Separation /= CloseNeighbourCount;
            }

            Separation *= 10.0f;

            // Add all the positions
            flockingDesiredPosition += Alignment;
            flockingDesiredPosition += Cohesion;
            flockingDesiredPosition += Separation;
            flockingDesiredPosition += currentPosition;
        }

        return flockingDesiredPosition;
    }

    private Vector3 GetComeBackDesiredPosition(Vector3 currentPosition)
    {
        ComeBack = Vector3.Zero;
        
        var attractionPoint = new Vector3(0, 0, 0);

        float distance = currentPosition.DistanceSquaredTo(attractionPoint);

        if(distance > 1500.0f)
        {
            ComeBack = (attractionPoint - currentPosition) - currentPosition;
        }

        return ComeBack;
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

using SoftBodyPhysics;
using UnityEngine;

public class CircularSpringBody : PressureBasedBody
{
    [Header("Circle Parameters")]
    public int numPoints = 16;
    public float radius = 1f;

    [Header("Spring Settings")]
    [Range(0f, 1f)]
    public float perimeterSpringStiffness = 0.8f;
    [Range(0f, 1f)]
    public float perimeterSpringDamping = 0.1f;
    public bool createPerimeterSprings = true;
    public bool createInternalSprings = true;

    [Header("Visualization")]
    public bool drawFilled = true;
    public Color fillColor = new Color(0.2f, 0.6f, 0.9f, 0.5f);

    private LineRenderer lineRenderer;

    protected override void Initialize()
    {
        points.Clear();
        constraints.Clear();

        // Set target area based on circle properties
        targetArea = radius * radius * Mathf.PI;

        // Create points around the circle
        for (int i = 0; i < numPoints; i++)
        {
            float angle = 2 * Mathf.PI * i / numPoints;
            Vector2 offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            PointMass point = new PointMass((Vector2)transform.position + offset, mass);
            points.Add(point);
        }

        // Create perimeter springs
        if (createPerimeterSprings)
        {
            CreatePerimeterSprings();
        }

        // Create internal springs for structure
        if (createInternalSprings)
        {
            CreateInternalSprings();
        }

        // Setup renderer
        SetupRenderer();
    }


    public override void UpdatePhysics(float deltaTime)
    {
        // Apply gravity and integrate points
        base.UpdatePhysics(deltaTime);

        // Handle collisions with solid objects
        HandleCollisionsWithSolids();

        // Solve constraints multiple times for stability
        for (int i = 0; i < constraintIterations; i++)
        {
            foreach (var constraint in constraints)
            {
                constraint.Solve();
            }
        }

        // Update transform position based on center
        transform.position = GetCenter();
    }

    // Add this method to handle collisions with solid objects
    private void HandleCollisionsWithSolids()
    {
        foreach (PointMass point in points)
        {
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(point.Position, 0.1f))
            {
                // Skip triggers
                if (collider.isTrigger) continue;

                // Get closest point on collider
                Vector2 closestPoint = collider.ClosestPoint(point.Position);
                float distance = Vector2.Distance(point.Position, closestPoint);

                // If the point is near or inside the collider
                if (distance < 0.1f || collider.OverlapPoint(point.Position))
                {
                    // Calculate normal direction
                    Vector2 normal = (point.Position - closestPoint).normalized;

                    // If normal is zero (point is inside), use direction from center
                    if (normal.magnitude < 0.001f)
                    {
                        normal = (point.Position - (Vector2)collider.bounds.center).normalized;

                        // If still zero, use a default direction
                        if (normal.magnitude < 0.001f)
                        {
                            normal = Vector2.up;
                        }
                    }

                    // Push the point out
                    point.Position = closestPoint + normal * 0.1f;

                    // Reflect velocity for bouncing
                    Vector2 velocity = point.Position - point.PreviousPosition;
                    Vector2 reflectedVelocity = Vector2.Reflect(velocity, normal) * 0.3f;
                    point.PreviousPosition = point.Position - reflectedVelocity;
                }
            }
        }
    }

    public override void HandleCollision(ISoftBodyObject other)
    {
        var otherPoints = other.GetPoints();

        foreach (var pointA in points)
        {
            foreach (var pointB in otherPoints)
            {
                float minDistance = 0.2f;
                Vector2 delta = pointA.Position - pointB.Position;
                float distance = delta.magnitude;

                if (distance < minDistance)
                {
                    // Mass-weighted collision response
                    float totalMass = pointA.Mass + pointB.Mass;
                    float pointAFactor = pointA.IsFixed ? 0 : pointB.Mass / totalMass;
                    float pointBFactor = pointB.IsFixed ? 0 : pointA.Mass / totalMass;

                    Vector2 correction = delta.normalized * (minDistance - distance);

                    if (!pointA.IsFixed)
                        pointA.ApplyDisplacement(correction * pointAFactor);

                    if (!pointB.IsFixed)
                        pointB.ApplyDisplacement(-correction * pointBFactor);
                }
            }
        }
    }

    private void CreatePerimeterSprings()
    {
        for (int i = 0; i < points.Count; i++)
        {
            int nextIndex = (i + 1) % points.Count;

            // Use SpringConstraint instead of DistanceConstraint
            constraints.Add(new SpringConstraint(
                points[i],
                points[nextIndex],
                Vector2.Distance(points[i].Position, points[nextIndex].Position),
                perimeterSpringStiffness,
                perimeterSpringDamping
            ));
        }
    }

    private void CreateInternalSprings()
    {
        int totalPoints = points.Count;
        int connectToNeighbors = Mathf.Min(4, totalPoints / 4); // Connect to several non-adjacent neighbors

        for (int i = 0; i < totalPoints; i++)
        {
            for (int offset = 2; offset <= connectToNeighbors + 1; offset++)
            {
                int targetIndex = (i + offset) % totalPoints;

                constraints.Add(new SpringConstraint(
                    points[i],
                    points[targetIndex],
                    Vector2.Distance(points[i].Position, points[targetIndex].Position),
                    perimeterSpringStiffness ,
                    perimeterSpringDamping
                ));
            }
        }
    }

    private void SetupRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = numPoints + 1; // +1 to close the loop
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.loop = true;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = fillColor;
        lineRenderer.endColor = fillColor;
    }

    private void LateUpdate()
    {
        if (lineRenderer != null && points.Count > 0)
        {
            // Update line renderer positions
            for (int i = 0; i < points.Count; i++)
            {
                lineRenderer.SetPosition(i, points[i].Position);
            }

            // Close the loop
            lineRenderer.SetPosition(points.Count, points[0].Position);
        }
    }
}
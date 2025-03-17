using System.Collections.Generic;
using UnityEngine;

public class Blob
{
    public List<BlobPoint> Points = new List<BlobPoint>();
    public Vector2 Center;

    public float Radius, Circumference, ChordLength;
    public float TargetArea;
    public float dampness, grav, maxDisp;

    // Feature toggles from BlobFeatures
    private BlobFeatures features;
    SoftBodySimulator sim;

    private Dictionary<Collider2D, Vector2> lastColliderPositions = new Dictionary<Collider2D, Vector2>();

    public float minimumCCDMovement = 0.01f;

    

    public Blob(Vector2 origin, int numPoints, float radius, float puffiness, float dampening, float gravity, float maxDisplacement, float maxVelocity, BlobFeatures features)
    {
        Radius = radius;
        TargetArea = radius * radius * Mathf.PI * puffiness;
        Circumference = radius * 2 * Mathf.PI;
        ChordLength = Circumference / numPoints;
        dampness = dampening;
        grav = gravity;
        maxDisp = maxDisplacement;
        this.features = features;

        Points = new List<BlobPoint>();
        for (int i = 0; i < numPoints; i++)
        {
            float angle = 2 * Mathf.PI * i / numPoints - Mathf.PI / 2;
            Vector2 offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            Points.Add(new BlobPoint(origin + offset, maxVelocity));
        }

        Center = origin;
        sim = SoftBodySimulator.Instance;

        Time.fixedDeltaTime = 0.01f;
    }

    public void UpdateParameters(float dampening, float gravity, float radius, float puffiness, float maxDisplacement, float maxVelocity, BlobFeatures newFeatures)
    {
        dampness = dampening;
        grav = gravity;
        maxDisp = maxDisplacement;
        features = newFeatures;

        // Update radius-dependent parameters
        Radius = radius;
        TargetArea = radius * radius * Mathf.PI * puffiness;
        Circumference = radius * 2 * Mathf.PI;
        ChordLength = Circumference / Points.Count;

        // Update max velocity for all points
        foreach (BlobPoint point in Points)
        {
            point.UpdateParameters(maxVelocity);
        }
    }

    public void Update(Vector2 mousePosition, bool isRightMousePressed, bool isRightMouseReleased, Bounds bounds, List<Collider2D> solidObjects, List<Blob> otherBlobs, float mouseInteractionRadius)
    {
        if (features.enableResetOnSpace && Input.GetKeyDown(KeyCode.Space))
        {
            ResetPosition();
        }

        foreach (BlobPoint point in Points)
        {
            point.VerletIntegrate(dampness  );
            point.ApplyGravity(grav );
        }
        int substeps = Mathf.Max(10, Mathf.CeilToInt( sim.blobParams.maxVelocity * 5));
        for (int j = 0; j < 20; j++)
        {
            if (features.enableSprings) ApplySpringForces();
            if (features.enablePressure) ApplyPressureExpansion();
            if (features.enableJellyEffect) ApplyJellyConstraints();

            foreach (BlobPoint point in Points)
            {
                point.ApplyDisplacement();
                if (features.enableSpringDragging)
                    point.HandleMouseInteraction(mousePosition, mouseInteractionRadius, isRightMousePressed, isRightMouseReleased);
                point.KeepInBounds(bounds);
                point.CheckCollisionDuringMovement(solidObjects);
            }

            if (features.enableCollisions) HandleCollisions(solidObjects);
            //if (features.enableCollisions) HandleColliderBounds(solidObjects);
            if (features.enableSoftBodyCollisions) HandleSoftBodyCollisions(otherBlobs);
        }
    }

    private void ApplySpringForces()
    {
        for (int i = 0; i < Points.Count; i++)
        {
            BlobPoint cur = Points[i];
            BlobPoint next = Points[i == Points.Count - 1 ? 0 : i + 1];

            Vector2 diff = next.Position - cur.Position;
            float distance = diff.magnitude;

            if (distance > ChordLength)
            {
                float error = (distance - ChordLength) / 2f;
                Vector2 offset = diff.normalized * error;
                cur.AccumulateDisplacement(offset);
                next.AccumulateDisplacement(-offset);
            }
        }
    }

    private void ApplyPressureExpansion()
    {
        float curArea = GetArea();
        float error = TargetArea - curArea;

        // Calculate pressure force based on area difference
        float pressureFactor = error / (Circumference * 10f);

        // Apply expansion/contraction forces
        for (int i = 0; i < Points.Count; i++)
        {
            BlobPoint prev = Points[(i + Points.Count - 1) % Points.Count];
            BlobPoint cur = Points[i];
            BlobPoint next = Points[(i + 1) % Points.Count];

            Vector2 secant = next.Position - prev.Position;
            Vector2 normal = new Vector2(-secant.y, secant.x).normalized * pressureFactor;

            if (normal.magnitude > maxDisp)
            {
                normal = normal.normalized * maxDisp;
            }

            cur.AccumulateDisplacement(normal);
        }
    }

    private void ApplyJellyConstraints()
    {
        Vector2 center = GetCenter();
        foreach (BlobPoint point in Points)
        {
            Vector2 diff = center - point.Position;
            point.AccumulateDisplacement(diff * features.jellyStrength);
        }
    }

    public float GetArea()
    {
        // Calculate area using shoelace formula
        float area = 0;
        for (int i = 0; i < Points.Count; i++)
        {
            Vector2 cur = Points[i].Position;
            Vector2 next = Points[(i + 1) % Points.Count].Position;
            area += (cur.x * next.y - next.x * cur.y);
        }
        return Mathf.Abs(area) / 2f;
    }

    public Vector2 GetCenter()
    {
        Vector2 sum = Vector2.zero;
        foreach (BlobPoint point in Points)
        {
            sum += point.Position;
        }
        return sum / Points.Count;
    }

    public void ResetPosition()
    {
        Vector2 currentCenter = GetCenter();
        Vector2 offset = Center - currentCenter;

        foreach (BlobPoint point in Points)
        {
            point.Position += offset;
            point.PreviousPosition += offset;
        }

        // Re-initialize the blob shape
        for (int i = 0; i < Points.Count; i++)
        {
            float angle = 2 * Mathf.PI * i / Points.Count - Mathf.PI / 2;
            Vector2 newPos = Center + new Vector2(Mathf.Cos(angle) * Radius, Mathf.Sin(angle) * Radius);
            Points[i].Position = newPos;
            Points[i].PreviousPosition = newPos;
        }
    }

    private void HandleCollisions(List<Collider2D> solidObjects)
    {
        if (!features.enableCollisions) return;

        // Track which colliders we're in contact with this frame
        HashSet<Collider2D> currentColliders = new HashSet<Collider2D>();

        foreach (BlobPoint point in Points)
        {
            foreach (Collider2D collider in solidObjects)
            {
                // Get the closest point on the collider's surface from the point's position
                Vector2 closestPoint = collider.ClosestPoint(point.Position);

                // Calculate distance to the closest point
                float distance = Vector2.Distance(point.Position, closestPoint);

                // If the point is near or inside the collider
                if (distance < 0.1f || collider.OverlapPoint(point.Position))
                {
                    // Add to our tracking set
                    currentColliders.Add(collider);

                    // Calculate the normal direction to push out
                    Vector2 normal = (point.Position - closestPoint).normalized;

                    // If normal is zero (which happens when point is inside), use direction from center
                    if (normal.magnitude < 0.001f)
                    {
                        normal = (point.Position - (Vector2)collider.bounds.center).normalized;

                        // If still zero (rare case), use a default direction
                        if (normal.magnitude < 0.001f)
                        {
                            normal = Vector2.up;
                        }
                    }

                    // Push the point to the surface of the collider plus a small offset
                    point.Position = closestPoint + normal * 0.1f;

                    // Reflect velocity for bouncing effect but reduce it slightly for damping
                    Vector2 velocity = point.Position - point.PreviousPosition;
                    Vector2 reflectedVelocity = Vector2.Reflect(velocity, normal) * features.bounceFactor;

                    point.PreviousPosition = point.Position - reflectedVelocity;

                    // If on top of the collider (normal pointing upward)
                    if (normal.y > 0.7f) // Approximately pointing up
                    {
                        // Apply movement from the platform
                        ApplyPlatformMovement(point, collider);
                    }
                }
            }
        }

        // Update last positions for next frame
        foreach (Collider2D collider in currentColliders)
        {
            lastColliderPositions[collider] = collider.bounds.center;
        }

        // Clean up colliders we're no longer in contact with
        List<Collider2D> toRemove = new List<Collider2D>();
        foreach (var kvp in lastColliderPositions)
        {
            if (!currentColliders.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var collider in toRemove)
        {
            lastColliderPositions.Remove(collider);
        }
    }

    // Add this method to your Blob class
    private void ApplyPlatformMovement(BlobPoint point, Collider2D platform)
    {
        // If we have a previous position for this platform
        if (lastColliderPositions.TryGetValue(platform, out Vector2 lastPos))
        {
            // Calculate how much the platform moved
            Vector2 platformDelta = (Vector2)platform.bounds.center - lastPos;

            // Apply that movement to the blob point
            point.Position += platformDelta;
            point.PreviousPosition += platformDelta;
        }
        else
        {
            // First contact with this platform, just record its position
            lastColliderPositions[platform] = platform.bounds.center;
        }
    }

    private void HandleSoftBodyCollisions(List<Blob> otherBlobs)
    {
        if (!features.enableSoftBodyCollisions) return;

        foreach (Blob other in otherBlobs)
        {
            //if (other == this) continue; // Don't collide with itself

            foreach (BlobPoint p1 in Points)
            {
                foreach (BlobPoint p2 in other.Points)
                {
                    float dist = Vector2.Distance(p1.Position, p2.Position);
                    if (dist < ChordLength * 0.8f) // Prevent overlap
                    {
                        Vector2 pushDir = (p1.Position - p2.Position).normalized;
                        float penetration = (ChordLength * 0.8f - dist) / 2f;
                        p1.AccumulateDisplacement(pushDir * penetration);
                        p2.AccumulateDisplacement(-pushDir * penetration);
                    }
                }
            }
        }
    }

    private void HandleColliderBounds(List<Collider2D> solidObjects)
    {
        if (!features.enableCollisions) return;

        // Track which colliders we're in contact with this frame
        HashSet<Collider2D> currentColliders = new HashSet<Collider2D>();

        foreach (BlobPoint point in Points)
        {
            foreach (Collider2D collider in solidObjects)
            {
                // Check if point is inside or intersecting with the collider
                if (collider.OverlapPoint(point.Position))
                {
                    currentColliders.Add(collider);

                    // Find the closest point on the collider surface
                    Vector2 closestSurfacePoint = collider.ClosestPoint(point.PreviousPosition);

                    // If the closest point is the same as the current position, we need to find a different reference
                    if ((closestSurfacePoint - point.Position).sqrMagnitude < 0.001f)
                    {
                        // Try to push away from the center of the collider
                        Vector2 direction = (point.Position - (Vector2)collider.bounds.center).normalized;

                        // If direction is zero, use a default direction
                        if (direction.sqrMagnitude < 0.001f)
                        {
                            direction = Vector2.up;
                        }

                        // Move the point outside the collider in the direction away from center
                        // Cast a ray from center of collider in the direction to find the edge
                        RaycastHit2D hit = Physics2D.Raycast(collider.bounds.center, direction, 100f);
                        if (hit.collider == collider)
                        {
                            // Move to the hit point plus a small offset
                            point.Position = hit.point + direction * 0.1f;
                        }
                        else
                        {
                            // Fallback to using the bounds
                            float boundsDist = Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y);
                            point.Position = (Vector2)collider.bounds.center + direction * (boundsDist + 0.1f);
                        }
                    }
                    else
                    {
                        // Simple case: just move to the closest surface point plus a small offset
                        Vector2 normal = (point.PreviousPosition - closestSurfacePoint).normalized;

                        // If normal is zero or very small, use direction from collider center
                        if (normal.sqrMagnitude < 0.001f)
                        {
                            normal = (point.Position - (Vector2)collider.bounds.center).normalized;
                            if (normal.sqrMagnitude < 0.001f)
                            {
                                normal = Vector2.up;
                            }
                        }

                        // Move to the surface point plus a small offset in the normal direction
                        point.Position = closestSurfacePoint + normal * 0.1f;
                    }

                    // Calculate velocity for bounce effect
                    Vector2 velocity = point.Position - point.PreviousPosition;

                    // If hitting a platform from above, apply platform movement
                    if (velocity.y < 0 && point.Position.y > collider.bounds.center.y)
                    {
                        ApplyPlatformMovement(point, collider);
                    }

                    // Update previous position to maintain velocity
                    // This is similar to KeepInBounds where we maintain the appropriate velocity
                    point.PreviousPosition = point.Position - velocity * features.bounceFactor;

                    break; // Only handle one collider per point per frame
                }
            }
        }

        // Update last positions for tracking movement
        foreach (Collider2D collider in currentColliders)
        {
            lastColliderPositions[collider] = collider.bounds.center;
        }

        // Clean up colliders we're no longer in contact with
        List<Collider2D> toRemove = new List<Collider2D>();
        foreach (var kvp in lastColliderPositions)
        {
            if (!currentColliders.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var collider in toRemove)
        {
            lastColliderPositions.Remove(collider);
        }
    }

    public Vector2 GetPreviousCenter()
    {
        Vector2 sum = Vector2.zero;
        foreach (BlobPoint point in Points)
        {
            sum += point.PreviousPosition;
        }
        return sum / Points.Count;
    }
}
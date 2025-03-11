using UnityEngine;
using System.Collections.Generic;

public class Blob
{
    public List<BlobPoint> Points = new List<BlobPoint>();
    public Vector2 Center;

    public float Radius, Area, Circumference, ChordLength;
    public float dampness, grav;

    private SoftBodySimulator sim;

    // Feature Toggles
    public bool enableSprings = true;
    public bool enableCollisions = true;
    public bool enableMorphing = false;
    public bool enablePressure = true;
    public bool enableSpringDragging = true;
    public bool enableSoftBodyCollisions = false;
    public bool enableJellyEffect = false;
    public bool enableResetOnSpace = true;

    public Blob(Vector2 origin, int numPoints, float radius, float puffiness, float dampening, float gravity)
    {
        Radius = radius;
        Area = radius * radius * Mathf.PI * puffiness;
        Circumference = radius * 2 * Mathf.PI;
        ChordLength = Circumference / numPoints;
        dampness = dampening;
        grav = gravity;

        Points = new List<BlobPoint>();
        for (int i = 0; i < numPoints; i++)
        {
            float angle = 2 * Mathf.PI * i / numPoints - Mathf.PI / 2;
            Vector2 offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            Points.Add(new BlobPoint(origin + offset));
        }

        sim = SoftBodySimulator.Instance;
        Center = origin;
    }

    public void Update(Vector2 mousePosition, bool isRightMousePressed, bool isRightMouseReleased, Bounds bounds, List<Collider2D> solidObjects, List<Blob> otherBlobs)
    {
        if (enableResetOnSpace && Input.GetKeyDown(KeyCode.Space))
        {
            ResetPosition();
        }

        foreach (BlobPoint point in Points)
        {
            point.VerletIntegrate(dampness);
            point.ApplyGravity(grav);
        }

        for (int j = 0; j < 10; j++)
        {
            if (enableSprings) ApplySpringForces();
            if (enablePressure) ApplyPressureExpansion();
            if (enableJellyEffect) ApplyJellyConstraints();

            foreach (BlobPoint point in Points)
            {
                point.ApplyDisplacement();
                if (enableSpringDragging) point.HandleMouseInteraction(mousePosition, 1f, isRightMousePressed, isRightMouseReleased);
                point.KeepInBounds(bounds);
            }

            if (enableCollisions) HandleCollisions(solidObjects);
            if (enableSoftBodyCollisions) HandleSoftBodyCollisions(otherBlobs);
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
        float error = Area - curArea;
        float offset = error / Circumference;

        for (int i = 0; i < Points.Count; i++)
        {
            BlobPoint prev = Points[i == 0 ? Points.Count - 1 : i - 1];
            BlobPoint cur = Points[i];
            BlobPoint next = Points[i == Points.Count - 1 ? 0 : i + 1];

            Vector2 secant = next.Position - prev.Position;
            Vector2 normal = new Vector2(-secant.y, secant.x).normalized * offset;

            if (normal.magnitude > sim.maxDisplacement)
            {
                normal = normal.normalized * sim.maxDisplacement;
            }

            cur.AccumulateDisplacement(normal);
        }
    }

    private void ApplyJellyConstraints() //no idea idc not tested
    {
        Vector2 center = GetCenter();
        foreach (BlobPoint point in Points)
        {
            Vector2 diff = center - point.Position;
            point.AccumulateDisplacement(diff * 0.1f);
        }
    }

    public float GetArea()
    {
        float area = 0;
        for (int i = 0; i < Points.Count; i++)
        {
            Vector2 cur = Points[i].Position;
            Vector2 next = Points[i == Points.Count - 1 ? 0 : i + 1].Position;
            area += ((cur.x - next.x) * (cur.y + next.y) / 2);
        }
        return Mathf.Abs(area);
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
        Vector2 center = GetCenter();
        Vector2 offset = Center - center;
        foreach (BlobPoint point in Points)
        {
            point.Position += offset;
            point.PreviousPosition += offset;
        }
    }

    private void HandleCollisions(List<Collider2D> solidObjects)
    {
        if (!enableCollisions) return;

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
                    Vector2 reflectedVelocity = Vector2.Reflect(velocity, normal) * 0.8f;

                    point.PreviousPosition = point.Position - reflectedVelocity;
                }
            }
        }
    }

    private void HandleSemiSolidCollisions(List<Collider2D> solidObjects) //blob slowly goes through idk why or how fuck it
    {
        if (!enableCollisions) return;

        foreach (BlobPoint point in Points)
        {
            foreach (Collider2D collider in solidObjects)
            {
                if (collider.OverlapPoint(point.Position))
                {
                    // Get the closest point on the collider's surface
                    Vector2 closestPoint = collider.ClosestPoint(point.Position);

                    // If we're inside the collider, move the point to the closest surface point
                    if (closestPoint != point.Position)
                    {
                        // Set the position directly to the surface, similar to how KeepInBounds works
                        point.Position = closestPoint;

                        // Update the previous position to maintain momentum along the surface
                        Vector2 velocity = point.Position - point.PreviousPosition;
                        Vector2 normal = ((Vector2)collider.bounds.center - closestPoint).normalized;
                        Vector2 tangent = new Vector2(-normal.y, normal.x);

                        // Project velocity onto the tangent to maintain sliding motion
                        float tangentVelocity = Vector2.Dot(velocity, tangent);
                        Vector2 newVelocity = tangent * tangentVelocity * 0.9f; // Add a bit of friction

                        point.PreviousPosition = point.Position - newVelocity;
                    }
                }
            }
        }
    }

    private void HandleSoftBodyCollisions(List<Blob> otherBlobs) //not tested idc
    {
        if (!enableSoftBodyCollisions) return;

        foreach (Blob other in otherBlobs)
        {
            if (other == this) continue; // Don't collide with itself

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
}

using UnityEngine;

public class BlobPoint
{
    public Vector2 Position;
    public Vector2 PreviousPosition;
    public Vector2 Displacement;
    public int DisplacementWeight;
    private bool isDragging = false;
    private Vector2 dragOffset;
    private float maxVelocity;

    public BlobPoint(Vector2 position, float maxVelocity)
    {
        Position = position;
        PreviousPosition = position;
        Displacement = Vector2.zero;
        DisplacementWeight = 0;
        this.maxVelocity = maxVelocity;
    }

    public void UpdateParameters(float newMaxVelocity)
    {
        this.maxVelocity = newMaxVelocity;
    }

    public void VerletIntegrate(float dampingFactor)
    {
        Vector2 temp = Position;
        Vector2 velocity = (Position - PreviousPosition) * dampingFactor;

        // Cap velocity to avoid sudden movement
        if (velocity.magnitude > maxVelocity)
        {
            velocity = velocity.normalized * maxVelocity;
        }

        Position += velocity;
        PreviousPosition = temp;
    }

    public void ApplyGravity(float gravityForce)
    {
        Position += new Vector2(0, -gravityForce);
    }

    public void AccumulateDisplacement(Vector2 offset)
    {
        Displacement += offset;
        DisplacementWeight += 1;
    }

    public void ApplyDisplacement()
    {
        if (DisplacementWeight > 0)
        {
            Displacement /= DisplacementWeight;
            Position += Displacement;
            Displacement = Vector2.zero;
            DisplacementWeight = 0;
        }
    }

    public void KeepInBounds(Bounds bounds)
    {
        Position.x = Mathf.Clamp(Position.x, bounds.min.x, bounds.max.x);
        Position.y = Mathf.Clamp(Position.y, bounds.min.y, bounds.max.y);
    }

    public void HandleMouseInteraction(Vector2 mousePosition, float interactionRadius, bool isRightMousePressed, bool isRightMouseReleased)
    {
        if (isRightMousePressed && !isDragging && Vector2.Distance(Position, mousePosition) < interactionRadius)
        {
            // Start dragging
            isDragging = true;
            dragOffset = Position - mousePosition;
        }

        if (isDragging)
        {
            if (isRightMousePressed)
            {
                // Continue dragging
                Position = mousePosition + dragOffset;
            }
            else if (isRightMouseReleased)
            {
                // Stop dragging and apply velocity
                isDragging = false;
                Vector2 velocity = (Position - PreviousPosition) / Time.deltaTime;
                PreviousPosition = Position - velocity * Time.deltaTime; // Apply velocity for throwing
            }
        }
    }
}

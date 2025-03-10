using UnityEngine;

public class BlobPoint
{
    public Vector2 Position;
    public Vector2 PreviousPosition;
    public Vector2 Displacement;
    public int DisplacementWeight;

    private bool isDragging = false; // Track if the point is being dragged
    private Vector2 dragOffset; // Offset between the mouse and the point's position

    public BlobPoint(Vector2 position)
    {
        Position = position;
        PreviousPosition = position;
        Displacement = Vector2.zero;
        DisplacementWeight = 0;
    }

    public void VerletIntegrate(float dampingFactor = 0.99f)
    {
        Vector2 temp = Position;
        Vector2 velocity = (Position - PreviousPosition) * dampingFactor;

        // Limit the maximum velocity to prevent instability
        float maxVelocity = 5f; // Adjust this value as needed
        if (velocity.magnitude > maxVelocity)
        {
            velocity = velocity.normalized * maxVelocity;
        }

        Position += velocity;
        PreviousPosition = temp;
    }

    public void ApplyGravity(float gravityForce = 1f)
    {
        Position += new Vector2(0, -gravityForce); // Note: using negative Y as gravity in Unity
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

    public void KeepInBounds(float screenWidth, float screenHeight)
    {
        // Convert screen bounds to world coordinates
        Vector2 minBounds = Camera.main.ScreenToWorldPoint(Vector2.zero);
        Vector2 maxBounds = Camera.main.ScreenToWorldPoint(new Vector2(screenWidth, screenHeight));

        // Constrain the position within the screen bounds
        Position.x = Mathf.Clamp(Position.x, minBounds.x, maxBounds.x);
        Position.y = Mathf.Clamp(Position.y, minBounds.y, maxBounds.y);
    }

    public void HandleMouseInteraction(Vector2 mousePosition, float collisionRadius, bool isRightMousePressed, bool isRightMouseReleased)
    {
        if (isRightMousePressed && Vector2.Distance(Position, mousePosition) < collisionRadius)
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
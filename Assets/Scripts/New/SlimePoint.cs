using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Represents a point in the SlimeBody, ported from your original BlobPoint class
    /// </summary>
    public class SlimePoint : IPointMass
    {
        public Vector2 Position { get; set; }
        public Vector2 PreviousPosition { get; set; }
        public Vector2 Velocity => (Position - PreviousPosition) / Time.fixedDeltaTime;
        public float Mass { get; set; } = 1f;
        public bool IsFixed { get; set; } = false;

        private Vector2 displacement = Vector2.zero;
        private int displacementCount = 0;
        private float maxVelocity;
        private bool isDragging = false;
        private Vector2 dragOffset;

        public SlimePoint(Vector2 position, float maxVelocity = 3f, float mass = 1f)
        {
            Position = position;
            PreviousPosition = position;
            this.maxVelocity = maxVelocity;
            Mass = mass;
        }

        public void UpdateParameters(float newMaxVelocity)
        {
            this.maxVelocity = newMaxVelocity;
        }

        public void ApplyForce(Vector2 force)
        {
            if (IsFixed || isDragging) return;
            Position += force * Time.fixedDeltaTime * Time.fixedDeltaTime / Mass;
        }

        public void ApplyDisplacement(Vector2 disp)
        {
            if (IsFixed || isDragging) return;
            displacement += disp;
            displacementCount++;
        }

        public void ApplyDisplacement()
        {
            if (displacementCount > 0 && !IsFixed && !isDragging)
            {
                Position += displacement / displacementCount;
                displacement = Vector2.zero;
                displacementCount = 0;
            }
        }

        public void VerletIntegrate(float deltaTime, float dampingFactor)
        {
            if (IsFixed || isDragging)
            {
                PreviousPosition = Position;
                return;
            }

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
            if (IsFixed || isDragging) return;
            Position += new Vector2(0, -gravityForce);
        }

        public void KeepInBounds(Bounds bounds)
        {
            if (IsFixed) return;
            Vector2 clampedPosition = new Vector2(
                Mathf.Clamp(Position.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(Position.y, bounds.min.y, bounds.max.y)
            );
            Position = clampedPosition;
        }
        public void HandleMouseInteraction(Vector2 mousePosition, float interactionRadius, bool isMousePressed, bool isMouseReleased)
        {
            if (IsFixed) return;

            if (isMousePressed && !isDragging && Vector2.Distance(Position, mousePosition) < interactionRadius)
            {
                // Start dragging
                isDragging = true;
                dragOffset = Position - mousePosition;
            }

            if (isDragging)
            {
                if (isMousePressed)
                {
                    // Continue dragging
                    Position = mousePosition + dragOffset;
                }
                else if (isMouseReleased)
                {
                    // Stop dragging and apply velocity
                    isDragging = false;
                    Vector2 velocity = (Position - PreviousPosition) / Time.fixedDeltaTime;
                    PreviousPosition = Position - velocity * Time.fixedDeltaTime; // Apply velocity for throwing
                }
            }
        }
    }
}
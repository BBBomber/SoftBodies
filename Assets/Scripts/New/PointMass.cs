using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Basic point mass implementation for the soft body physics system
    /// </summary>
    public class PointMass : IPointMass
    {
        public Vector2 Position { get; set; }
        public Vector2 PreviousPosition { get; set; }
        public Vector2 Velocity => (Position - PreviousPosition) / Time.fixedDeltaTime;
        public float Mass { get; set; } = 1f;
        public bool IsFixed { get; set; } = false;

        private Vector2 accumulatedForce = Vector2.zero;
        private Vector2 accumulatedDisplacement = Vector2.zero;
        private int displacementCount = 0;
        private float maxVelocity = float.MaxValue;

        public PointMass(Vector2 position, float mass = 1f, float maxVelocity = float.MaxValue)
        {
            Position = position;
            PreviousPosition = position;
            Mass = mass;
            this.maxVelocity = maxVelocity;
        }

        public void ApplyForce(Vector2 force)
        {
            if (IsFixed) return;
            accumulatedForce += force / Mass;
        }

        public void ApplyDisplacement(Vector2 displacement)
        {
            if (IsFixed) return;
            accumulatedDisplacement += displacement;
            displacementCount++;
        }

        public void VerletIntegrate(float deltaTime, float dampingFactor)
        {
            if (IsFixed)
            {
                PreviousPosition = Position;
                return;
            }

            // Apply forces
            Vector2 acceleration = accumulatedForce;
            accumulatedForce = Vector2.zero;

            // Store current position
            Vector2 temp = Position;

            // Calculate damped velocity
            Vector2 velocity = (Position - PreviousPosition) * dampingFactor;

            // Cap velocity if needed
            if (maxVelocity < float.MaxValue && velocity.magnitude > maxVelocity)
            {
                velocity = velocity.normalized * maxVelocity;
            }

            // Update position using Verlet integration
            Position += velocity + acceleration * deltaTime * deltaTime;

            // Store previous position for next step
            PreviousPosition = temp;

            // Apply accumulated displacements
            if (displacementCount > 0)
            {
                Position += accumulatedDisplacement / displacementCount;
                accumulatedDisplacement = Vector2.zero;
                displacementCount = 0;
            }
        }

        public void ApplyGravity(float gravityForce)
        {
            if (IsFixed ) return;
            Position += new Vector2(0, -gravityForce);
        }
    }
}
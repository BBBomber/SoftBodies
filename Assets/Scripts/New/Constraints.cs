using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Distance constraint between two point masses
    /// </summary>
    public class DistanceConstraint : IConstraint
    {
        private IPointMass pointA;
        private IPointMass pointB;
        private float restDistance;
        private float stiffness;

        public bool IsActive { get; set; } = true;

        public DistanceConstraint(IPointMass pointA, IPointMass pointB, float restDistance, float stiffness = 1f)
        {
            this.pointA = pointA;
            this.pointB = pointB;
            this.restDistance = restDistance;
            this.stiffness = Mathf.Clamp01(stiffness);
        }

        public DistanceConstraint(IPointMass pointA, IPointMass pointB, float stiffness = 1f)
        {
            this.pointA = pointA;
            this.pointB = pointB;
            this.restDistance = Vector2.Distance(pointA.Position, pointB.Position);
            this.stiffness = Mathf.Clamp01(stiffness);
        }

        public void Solve()
        {
            if (!IsActive) return;

            Vector2 delta = pointB.Position - pointA.Position;
            float currentDistance = delta.magnitude;

            // Skip if points are at the same position
            if (currentDistance < 0.0001f) return;

            float errorFactor = (currentDistance - restDistance) / currentDistance;

            // Scale by stiffness
            errorFactor *= stiffness;

            // Calculate movement amounts based on mass
            float totalMass = pointA.Mass + pointB.Mass;
            float pointAFactor = pointA.IsFixed ? 0 : pointB.Mass / totalMass;
            float pointBFactor = pointB.IsFixed ? 0 : pointA.Mass / totalMass;

            // Apply corrections
            if (!pointA.IsFixed)
                pointA.Position += delta * errorFactor * pointAFactor;

            if (!pointB.IsFixed)
                pointB.Position -= delta * errorFactor * pointBFactor;
        }
    }

    /// <summary>
    /// Spring constraint between two point masses (softer than distance constraint)
    /// </summary>
    public class SpringConstraint : IConstraint
    {
        private IPointMass pointA;
        private IPointMass pointB;
        private float restDistance;
        private float stiffness;
        private float damping;

        public bool IsActive { get; set; } = true;

        public SpringConstraint(IPointMass pointA, IPointMass pointB, float restDistance, float stiffness = 0.5f, float damping = 0.1f)
        {
            this.pointA = pointA;
            this.pointB = pointB;
            this.restDistance = restDistance;
            this.stiffness = Mathf.Clamp01(stiffness);
            this.damping = Mathf.Clamp01(damping);
        }

        public SpringConstraint(IPointMass pointA, IPointMass pointB, float stiffness = 0.5f, float damping = 0.1f)
        {
            this.pointA = pointA;
            this.pointB = pointB;
            this.restDistance = Vector2.Distance(pointA.Position, pointB.Position);
            this.stiffness = Mathf.Clamp01(stiffness);
            this.damping = Mathf.Clamp01(damping);
        }

        public void Solve()
        {
            if (!IsActive) return;

            Vector2 delta = pointB.Position - pointA.Position;
            float currentDistance = delta.magnitude;

            // Skip if points are at the same position
            if (currentDistance < 0.0001f) return;

            // Calculate spring force
            float springForce = (currentDistance - restDistance) * stiffness;

            // Add damping
            Vector2 relativeVelocity = pointB.Velocity - pointA.Velocity;
            float dampingForce = Vector2.Dot(relativeVelocity, delta.normalized) * damping;

            // Total force
            float totalForce = springForce + dampingForce;
            Vector2 force = delta.normalized * totalForce;

            // Apply forces
            if (!pointA.IsFixed)
                pointA.ApplyForce(force);

            if (!pointB.IsFixed)
                pointB.ApplyForce(-force);
        }
    }
}
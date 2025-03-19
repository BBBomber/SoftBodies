using System.Collections.Generic;
using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Base component for spring-based soft bodies
    /// </summary>
    public class SpringBasedBody : SoftBodyObject
    {
        [Header("Spring Settings")]
        [Range(0f, 1f)]
        public float springStiffness = 0.5f;
        [Range(0f, 1f)]
        public float springDamping = 0.1f;
        public bool createPerimeterSprings = true;
        public bool createInternalSprings = false;

        [Header("Gas Pressure Settings")]
        public bool useGasPressure = false;
        public float pressureAmount = 1.0f;
        public float pressureForce = 0.5f;

        protected float targetArea;
        protected float currentArea;

        [Header("Debug Visualization")]
        public bool showSprings = true;

        protected override void Initialize()
        {
            // Setup would be implemented in derived classes
        }

        public override void UpdatePhysics(float deltaTime)
        {
            base.UpdatePhysics(deltaTime);

            // Apply gas pressure if enabled
            if (useGasPressure)
            {
                currentArea = CalculateArea();
                ApplyPressureForce();
            }
        }

        protected void CreatePerimeterSprings()
        {
            for (int i = 0; i < points.Count; i++)
            {
                int nextIndex = (i + 1) % points.Count;
                constraints.Add(new SpringConstraint(points[i], points[nextIndex], springStiffness, springDamping));
            }
        }

        protected void CreateInternalSprings()
        {
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 2; j < points.Count; j++)
                {
                    // Skip adjacent points (already handled by perimeter springs)
                    if (j == (i + 1) % points.Count) continue;

                    // Also skip direct opposites for odd-numbered point counts
                    if (points.Count % 2 == 1 && j == (i + points.Count / 2) % points.Count) continue;

                    constraints.Add(new SpringConstraint(points[i], points[j], springStiffness * 0.5f, springDamping));
                }
            }
        }

        protected void ApplyPressureForce()
        {
            if (!useGasPressure) return;

            // Calculate pressure error
            float areaError = targetArea * pressureAmount - currentArea;
            float pressureFactor = areaError / points.Count * pressureForce;

            // Apply pressure to each point
            for (int i = 0; i < points.Count; i++)
            {
                IPointMass current = points[i];
                IPointMass prev = points[(i + points.Count - 1) % points.Count];
                IPointMass next = points[(i + 1) % points.Count];

                // Calculate normal direction (perpendicular to the edge)
                Vector2 edge = next.Position - prev.Position;
                Vector2 normal = new Vector2(-edge.y, edge.x).normalized;

                // Apply pressure force in normal direction
                current.ApplyForce(normal * pressureFactor);
            }
        }

        public override Vector2 GetCenter()
        {
            if (points.Count == 0) return transform.position;

            Vector2 sum = Vector2.zero;
            foreach (var point in points)
            {
                sum += point.Position;
            }
            return sum / points.Count;
        }

        public override void HandleCollision(ISoftBodyObject other)
        {
            // Similar to pressure-based collision handling
            var otherPoints = other.GetPoints();

            foreach (var pointA in points)
            {
                foreach (var pointB in otherPoints)
                {
                    float minDistance = 0.1f; // Minimum separation distance
                    Vector2 delta = pointA.Position - pointB.Position;
                    float distance = delta.magnitude;

                    if (distance < minDistance)
                    {
                        // Collision response
                        Vector2 correction = delta.normalized * (minDistance - distance) * 0.5f;

                        if (!pointA.IsFixed)
                            pointA.ApplyDisplacement(correction);

                        if (!pointB.IsFixed)
                            pointB.ApplyDisplacement(-correction);
                    }
                }
            }
        }

        // Visualization for debugging
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showSprings || constraints.Count == 0) return;

            // Draw springs
            Gizmos.color = Color.yellow;

            foreach (var constraint in constraints)
            {
                if (constraint is SpringConstraint springConstraint)
                {
                    // This is just a visual representation since we can't directly access SpringConstraint's points
                    // In a real implementation, we might need to add methods to get these points
                }
            }

            // For now, just draw all points and connections as a fallback
            if (points.Count > 0)
            {
                // Draw points
                Gizmos.color = Color.cyan;
                foreach (var point in points)
                {
                    Gizmos.DrawSphere(point.Position, 0.1f);
                }

                // Draw connections
                Gizmos.color = Color.magenta;
                for (int i = 0; i < points.Count; i++)
                {
                    Vector2 p1 = points[i].Position;
                    Vector2 p2 = points[(i + 1) % points.Count].Position;
                    Gizmos.DrawLine(p1, p2);
                }

                // Draw center
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(GetCenter(), 0.15f);
            }
        }

    }

}
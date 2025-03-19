using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Base component for pressure-based soft bodies like the slime
    /// </summary>
    public class PressureBasedBody : SoftBodyObject
    {
        [Header("Pressure Settings")]
        public float targetPressure = 1.5f;
        public float pressureForce = 1.0f;

        protected float targetArea;
        protected float currentArea;

        [Header("Debug Visualization")]
        public bool showPressureForces = false;

        protected override void Initialize()
        {
            // Setup would be implemented in derived classes
        }

        public override void UpdatePhysics(float deltaTime)
        {
            base.UpdatePhysics(deltaTime);

            // Calculate current area
            currentArea = CalculateArea();

            // Apply pressure force
            ApplyPressureForce();
        }

        protected void ApplyPressureForce()
        {
            // Calculate pressure error
            float areaError = targetArea * targetPressure - currentArea;
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
            // Basic collision response
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
            if (!Application.isPlaying || !showPressureForces || points.Count == 0) return;

            Gizmos.color = Color.green;

            // Calculate pressure for visualization
            float areaError = targetArea * targetPressure - currentArea;
            float pressureFactor = areaError / points.Count * pressureForce;

            // Draw pressure force vectors
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 current = points[i].Position;
                Vector2 prev = points[(i + points.Count - 1) % points.Count].Position;
                Vector2 next = points[(i + 1) % points.Count].Position;

                Vector2 edge = next - prev;
                Vector2 normal = new Vector2(-edge.y, edge.x).normalized;

                // Draw normal vector scaled by pressure
                Gizmos.DrawLine(current, current + normal * pressureFactor * 2);

                // Draw arrow tip
                Vector2 arrowTip = current + normal * pressureFactor * 2;
                Vector2 arrowLeft = arrowTip - normal * 0.2f + new Vector2(normal.y, -normal.x) * 0.2f;
                Vector2 arrowRight = arrowTip - normal * 0.2f - new Vector2(normal.y, -normal.x) * 0.2f;

                Gizmos.DrawLine(arrowTip, arrowLeft);
                Gizmos.DrawLine(arrowTip, arrowRight);
            }
        }
    }
}
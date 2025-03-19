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
        public float springStiffness = 1f;
        [Range(0f, 1f)]
        public float springDamping = 0.1f;
        public bool createPerimeterSprings = true;
        public bool createInternalSprings = true;

        [Header("Gas Pressure Settings")]
        public bool useGasPressure = true;
        public float pressureAmount = 1.0f;
        public float pressureForce = 0.5f;
        public float targetRadius = 1.0f; // Add this line

        protected float targetArea;
        protected float currentArea;

        [Header("Debug Visualization")]
        public bool showSprings = true;

        private Dictionary<Collider2D, Vector2> lastColliderPositions = new Dictionary<Collider2D, Vector2>();

        [SerializeField] private List<Collider2D> solidObjects = new List<Collider2D>();
        protected override void Initialize()
        {
            // Setup would be implemented in derived classes
            solidObjects.AddRange(FindObjectsByType<Collider2D>(FindObjectsSortMode.None));
        }

        public override void UpdatePhysics(float deltaTime)
        {
            base.UpdatePhysics(deltaTime);
            targetArea = targetRadius * targetRadius * Mathf.PI * pressureAmount;
            // Apply gas pressure if enabled
            
            if (useGasPressure)
            {
                currentArea = CalculateArea();
                ApplyPressureForce();
                //EnforceMaximumSize();
                
            }
            HandleCollisions1(solidObjects);
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
            int totalPoints = points.Count;
            int connectToNeighbors = 2; // Connect to the next 2 non-adjacent neighbors on each side

            for (int i = 0; i < totalPoints; i++)
            {
                // Connect to the next few non-adjacent neighbors
                for (int offset = 2; offset <= connectToNeighbors + 1; offset++)
                {
                    // Connect forward (clockwise)
                    int forwardIndex = (i + offset) % totalPoints;
                    constraints.Add(new SpringConstraint(points[i], points[forwardIndex], springStiffness * 0.5f, springDamping));

                    // No need to connect backward (counterclockwise) since those connections
                    // will be handled when we process the other points
                    // This prevents duplicate springs
                }
            }
        }

        protected void ApplyPressureForce()
        {
            if (!useGasPressure) return;

            // Calculate current area
            currentArea = CalculateArea();
            
            // Calculate pressure error - WITH PROPER SIGN
            float areaError = targetArea * pressureAmount - currentArea;

            // Add a safeguard against extreme expansion
            areaError = Mathf.Clamp(areaError, -currentArea * 0.5f, currentArea * 0.5f);

            float pressureFactor = areaError / points.Count * pressureForce;

            // Debug log to see what's happening
            if (Time.frameCount % 60 == 0) // Log once per second
            {
                Debug.Log($"Area: Current={currentArea:F2}, Target={targetArea * pressureAmount:F2}, Error={areaError:F2}, Factor={pressureFactor:F2}");
            }

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

        public void HandleCollisions1(List<Collider2D> solidObjects)
        {
            // Track which colliders we're in contact with this frame
            HashSet<Collider2D> currentColliders = new HashSet<Collider2D>();

            foreach (PointMass point in points)
            {
                foreach (Collider2D collider in solidObjects)
                {
                    // Get the closest point on the collider's surface
                    Vector2 closestPoint = collider.ClosestPoint(point.Position);
                    float distance = Vector2.Distance(point.Position, closestPoint);

                    // If the point is near or inside the collider
                    if (distance < 0.1f || collider.OverlapPoint(point.Position))
                    {
                        // Add to our tracking set
                        currentColliders.Add(collider);

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

       
        protected void EnforceMaximumSize()
        {
            if (!useGasPressure) return;

            // Calculate current radius (average distance from center)
            Vector2 center = GetCenter();
            float maxAllowedRadius = targetRadius * 2f; // Max 2x original size

            float totalRadius = 0f;
            foreach (var point in points)
            {
                float distance = Vector2.Distance(point.Position, center);
                totalRadius += distance;
            }
            float avgRadius = totalRadius / points.Count;

            // If the average radius is too large, scale all points inward
            if (avgRadius > maxAllowedRadius)
            {
                float scaleFactor = maxAllowedRadius / avgRadius;
                foreach (var point in points)
                {
                    Vector2 dirFromCenter = point.Position - center;
                    point.Position = center + dirFromCenter * scaleFactor;
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
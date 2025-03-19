using UnityEngine;
using System.Collections.Generic;
namespace SoftBodyPhysics
{
    /// <summary>
    /// Improved circular soft body that maintains its shape better
    /// </summary>
    public class CircularSpringBody : SpringBasedBody
    {
        [Header("Circle Parameters")]
        public int numPoints = 16;
        public float radius = 1f;

        [Header("Structure Reinforcement")]
        [Range(0f, 1f)]
        public float structuralStiffness = 0.9f; // Higher stiffness for structural springs
        public bool createRadialSprings = true;  // Connect each point to the center
        public bool createCrossSprings = true;   // Connect points across the circle

        [Header("Visualization")]
        public bool drawFilled = true;
        public Color fillColor = new Color(0.2f, 0.6f, 0.9f, 0.5f);
        public Material lineMaterial;

        private LineRenderer lineRenderer;
        private PointMass centerPoint; // Center point for radial connections

        protected override void Initialize()
        {
            // Clear existing data
            points.Clear();
            constraints.Clear();
            targetRadius = radius;

            // Calculate target area for pressure
            targetArea = radius * radius * Mathf.PI * pressureAmount;

            // Create center point if we're using radial springs
            if (createRadialSprings)
            {
                centerPoint = new PointMass((Vector2)transform.position);
                // Don't add to points list as we don't want it affected by pressure
            }

            // Create points in a circle
            for (int i = 0; i < numPoints; i++)
            {
                float angle = 2 * Mathf.PI * i / numPoints;
                Vector2 offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                PointMass point = new PointMass((Vector2)transform.position + offset, mass);
                points.Add(point);
            }

            // Create perimeter springs (with higher stiffness for better shape retention)
            if (createPerimeterSprings)
            {
                CreateStructuralSprings();
            }

            // Create radial springs (from each point to center)
            if (createRadialSprings)
            {
                CreateRadialSprings();
            }

            // Create cross springs (point to opposite point)
            if (createCrossSprings)
            {
                CreateCrossSprings();
            }

            // Create other internal springs
            if (createInternalSprings)
            {
                CreateInternalSprings();
            }

            // Setup renderer
            SetupRenderer();

            base.Initialize();
        }



        private void CreateStructuralSprings()
        {
            // Create perimeter springs with higher stiffness for structural integrity
            for (int i = 0; i < points.Count; i++)
            {
                int nextIndex = (i + 1) % points.Count;
                // Use structural stiffness which should be higher than regular springs
                constraints.Add(new DistanceConstraint(points[i], points[nextIndex], structuralStiffness));
            }
        }

        private void CreateRadialSprings()
        {
            // Connect each point to the center
            for (int i = 0; i < points.Count; i++)
            {
                // Use a more rigid constraint for radial connections
                constraints.Add(new DistanceConstraint(points[i], centerPoint, structuralStiffness * 0.9f));
            }
        }

        private void CreateCrossSprings()
        {
            // Connect each point with the point across from it (or nearly across)
            for (int i = 0; i < points.Count / 2; i++)
            {
                int oppositeIndex = (i + numPoints / 2) % numPoints;
                constraints.Add(new DistanceConstraint(points[i], points[oppositeIndex], structuralStiffness * 0.8f));
            }

            // Add some diagonal cross springs for more stability
            int step = Mathf.Max(1, numPoints / 8); // Avoid too many springs with high point counts
            for (int i = 0; i < points.Count; i += step)
            {
                for (int j = i + step * 2; j < i + numPoints / 2; j += step * 2)
                {
                    int index = j % numPoints;
                    constraints.Add(new DistanceConstraint(points[i], points[index], structuralStiffness * 0.7f));
                }
            }
        }

        private void SetupRenderer()
        {
            // Setup renderer
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.positionCount = numPoints + 1; // +1 to close the loop
            lineRenderer.loop = true;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;

            if (lineMaterial != null)
            {
                lineRenderer.material = lineMaterial;
            }
            else
            {
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            lineRenderer.startColor = fillColor;
            lineRenderer.endColor = fillColor;
        }

        public override void UpdatePhysics(float deltaTime)
        {
            // First update center point if it exists
            if (createRadialSprings && centerPoint != null)
            {
                // Keep center point at the average position
                centerPoint.Position = GetCenter();
                centerPoint.PreviousPosition = centerPoint.Position; // No velocity for center
            }

            // Then update regular physics
            base.UpdatePhysics(deltaTime);
            
            // Apply additional shape maintenance
           // EnforceCircularShape();
        }

        private void EnforceCircularShape()
        {
            // Additional step to help maintain circular shape
            Vector2 center = GetCenter();

            // Calculate average radius
            float avgRadius = 0f;
            foreach (var point in points)
            {
                avgRadius += Vector2.Distance(point.Position, center);
            }
            avgRadius /= points.Count;

            // Softly pull points toward their ideal circular position
            for (int i = 0; i < points.Count; i++)
            {
                float angle = 2 * Mathf.PI * i / numPoints;
                Vector2 idealPos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * avgRadius;

                // Apply a very gentle pull toward the ideal position (0.05 strength)
                Vector2 correction = (idealPos - points[i].Position) * 0.05f;
                points[i].ApplyDisplacement(correction);
            }
        }

        // Use a new method name since we can't override the parent method
        protected void ApplyCustomPressureForce()
        {
            if (!useGasPressure) return;

            // Calculate current area using more accurate method
            currentArea = CalculatePolygonArea();

            // Calculate pressure with smoother error handling
            float targetPressureArea = targetArea * pressureAmount;
            float pressureError = targetPressureArea - currentArea;

            // Limit the pressure error to avoid extreme forces
            pressureError = Mathf.Clamp(pressureError, -currentArea * 0.2f, currentArea * 0.2f);

            // Distribute the pressure force more evenly
            float pressureFactor = pressureError / points.Count * pressureForce;

            // Apply pressure forces in normal direction
            for (int i = 0; i < points.Count; i++)
            {
                IPointMass current = points[i];
                IPointMass prev = points[(i + points.Count - 1) % points.Count];
                IPointMass next = points[(i + 1) % points.Count];

                // Calculate edge vectors
                Vector2 toPrev = prev.Position - current.Position;
                Vector2 toNext = next.Position - current.Position;

                // Calculate normal (average of perpendicular vectors to both edges)
                Vector2 normal1 = new Vector2(-toPrev.y, toPrev.x).normalized;
                Vector2 normal2 = new Vector2(toNext.y, -toNext.x).normalized;
                Vector2 normal = (normal1 + normal2).normalized;

                // Apply pressure force
                current.ApplyForce(normal * pressureFactor);
            }
        }

        private float CalculatePolygonArea()
        {
            // More accurate polygon area calculation
            float area = 0;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p1 = points[i].Position;
                Vector2 p2 = points[(i + 1) % points.Count].Position;
                area += (p1.x * p2.y - p2.x * p1.y);
            }
            return Mathf.Abs(area) / 2f;
        }

        private void LateUpdate()
        {
            if (points.Count == 0) return;

            // Update line renderer positions
            for (int i = 0; i < points.Count; i++)
            {
                lineRenderer.SetPosition(i, points[i].Position);
            }

            // Close the loop
            lineRenderer.SetPosition(points.Count, points[0].Position);
        }
    }
}
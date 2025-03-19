using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Circular soft body that uses springs and pressure
    /// </summary>
    public class CircularSpringBody : SpringBasedBody
    {
        [Header("Circle Parameters")]
        public int numPoints = 16;
        public float radius = 1f;

        [Header("Visualization")]
        public bool drawFilled = true;
        public Color fillColor = new Color(0.2f, 0.6f, 0.9f, 0.5f);
        public Material lineMaterial;
        private LineRenderer lineRenderer;

        protected override void Initialize()
        {
            // Create points in a circle
            points.Clear();
            constraints.Clear();

            // Calculate target area for pressure
            targetArea = radius * radius * Mathf.PI * pressureAmount;

            for (int i = 0; i < numPoints; i++)
            {
                float angle = 2 * Mathf.PI * i / numPoints;
                Vector2 offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                PointMass point = new PointMass((Vector2)transform.position + offset);
                points.Add(point);
            }

            // Create perimeter springs
            if (createPerimeterSprings)
            {
                CreatePerimeterSprings();
            }

            // Create internal springs
            if (createInternalSprings)
            {
                CreateInternalSprings();
            }

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

        private void LateUpdate()
        {
            if (points.Count == 0) return;

            // Update line renderer
            for (int i = 0; i < points.Count; i++)
            {
                lineRenderer.SetPosition(i, points[i].Position);
            }
            // Close the loop
            lineRenderer.SetPosition(points.Count, points[0].Position);
        }

        private void OnDrawGizmos()
        {
            // Base visualization handled by parent class

            // Additional filled circle visualization
            if (Application.isPlaying && drawFilled && points.Count > 2)
            {
                // Draw filled polygon
                Vector3[] vertices = new Vector3[points.Count];
                for (int i = 0; i < points.Count; i++)
                {
                    vertices[i] = points[i].Position;
                }

                Gizmos.color = fillColor;
                Vector2 center = GetCenter();

                // Draw triangles from center to each adjacent pair of points
                for (int i = 0; i < points.Count; i++)
                {
                    Vector3 p1 = vertices[i];
                    Vector3 p2 = vertices[(i + 1) % points.Count];
                    Gizmos.DrawLine(p1, p2);

                    if (drawFilled)
                    {
                        // Draw a triangle for filled appearance
                        Gizmos.DrawLine(center, p1);
                        Gizmos.DrawLine(center, p2);
                    }
                }
            }
        }
    }
}
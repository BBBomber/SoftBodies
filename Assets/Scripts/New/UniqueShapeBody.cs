using System.Collections.Generic;
using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Unique shape soft body that uses a reference frame
    /// </summary>
    public class UniqueShapeBody : FrameBasedBody
    {
        [Header("Shape Definition")]
        public Vector2[] shapePoints;

        [Header("Spring Settings")]
        public bool usePerimeterSprings = true;
        public bool useInternalSprings = false;
        [Range(0f, 1f)]
        public float springStiffness = 0.5f;
        [Range(0f, 1f)]
        public float springDamping = 0.1f;

        [Header("Visualization")]
        public bool drawFilled = true;
        public Color fillColor = new Color(0.2f, 0.6f, 0.9f, 0.5f);
        public Material lineMaterial;
        private LineRenderer lineRenderer;

        protected override void Initialize()
        {
            if (shapePoints == null || shapePoints.Length < 3)
            {
                Debug.LogError("UniqueShapeBody: At least 3 shape points are required");
                shapePoints = new Vector2[] {
                    new Vector2(-1, -1),
                    new Vector2(1, -1),
                    new Vector2(0, 1)
                };
            }

            // Clear existing data
            points.Clear();
            constraints.Clear();
            framePoints.Clear();

            // Create points and store frame reference
            for (int i = 0; i < shapePoints.Length; i++)
            {
                Vector2 worldPos = transform.TransformPoint(shapePoints[i]);
                PointMass point = new PointMass(worldPos);
                points.Add(point);
                framePoints.Add(shapePoints[i]);
            }

            // Create perimeter springs if enabled
            if (usePerimeterSprings)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    int nextIndex = (i + 1) % points.Count;
                    constraints.Add(new SpringConstraint(points[i], points[nextIndex], springStiffness, springDamping));
                }
            }

            // Create internal springs if enabled
            if (useInternalSprings && points.Count > 3)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    for (int j = i + 2; j < points.Count; j++)
                    {
                        // Skip adjacent points (already handled by perimeter springs)
                        if (j == (i + 1) % points.Count) continue;

                        // Skip direct opposites for odd-numbered point counts
                        if (points.Count % 2 == 1 && j == (i + points.Count / 2) % points.Count) continue;

                        constraints.Add(new SpringConstraint(points[i], points[j], springStiffness * 0.5f, springDamping));
                    }
                }
            }

            // Calculate frame center
            frameCenterOffset = Vector2.zero;
            for (int i = 0; i < framePoints.Count; i++)
            {
                frameCenterOffset += framePoints[i];
            }
            frameCenterOffset /= framePoints.Count;

            // Initialize frame rotation
            frameRotation = 0f;

            // Setup renderer
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.positionCount = points.Count + 1; // +1 to close the loop
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

        // Allow manual rotation of the frame
        public void RotateFrame(float angleDegrees)
        {
            frameRotation += angleDegrees * Mathf.Deg2Rad;
        }

        // Allow manual positioning of the frame
        public void SetFramePosition(Vector2 position)
        {
            transform.position = position;
            useAutomaticFramePositioning = false;

            Vector2 oldCenter = GetCenter();
            Vector2 offset = position - oldCenter;

            // Move all points
            foreach (var point in points)
            {
                point.Position += offset;
                point.PreviousPosition += offset;
            }
        }

        // Allow shape changing by modifying frame points
        public void ModifyFramePoint(int index, Vector2 newLocalPosition)
        {
            if (index < 0 || index >= framePoints.Count)
            {
                Debug.LogError($"ModifyFramePoint: Index {index} out of range");
                return;
            }

            framePoints[index] = newLocalPosition;

            // Recalculate frame center offset
            frameCenterOffset = Vector2.zero;
            for (int i = 0; i < framePoints.Count; i++)
            {
                frameCenterOffset += framePoints[i];
            }
            frameCenterOffset /= framePoints.Count;
        }

        private void OnDrawGizmos()
        {
            // Base visualization handled by parent class

            // Additional filled shape visualization
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
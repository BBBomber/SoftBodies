using UnityEngine;
using System.Collections.Generic;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Debug utilities for soft body physics
    /// </summary>
    public class SoftBodyDebugger : MonoBehaviour
    {
        [Header("Physics Debug")]
        public bool showPointMasses = true;
        public bool showConstraints = true;
        public bool showCenters = true;
        public bool showVelocities = false;
        public bool showCollisionInfo = false;

        [Header("Force Debug")]
        public bool showGravity = false;
        public bool showPressure = false;

        [Header("Visualization Settings")]
        public float pointSize = 0.1f;
        public float velocityScale = 0.5f;
        public Color pointColor = Color.yellow;
        public Color constraintColor = Color.cyan;
        public Color centerColor = Color.red;
        public Color velocityColor = Color.green;
        public Color gravityColor = Color.blue;
        public Color pressureColor = Color.magenta;

        private List<ISoftBodyObject> softBodies = new List<ISoftBodyObject>();

        private void Start()
        {
            // Find all soft bodies in the scene
            FindSoftBodies();
        }

        [ContextMenu("Find Soft Bodies")]
        public void FindSoftBodies()
        {
            softBodies.Clear();

            // Find all implementations of ISoftBodyObject
            SoftBodyObject[] bodies = FindObjectsByType<SoftBodyObject>(FindObjectsSortMode.None);
            foreach (SoftBodyObject body in bodies)
            {
                softBodies.Add(body);
            }

            Debug.Log($"Found {softBodies.Count} soft bodies");
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            foreach (ISoftBodyObject softBody in softBodies)
            {
                if (softBody == null) continue;

                List<IPointMass> points = softBody.GetPoints();
                Vector2 center = softBody.GetCenter();

                // Draw point masses
                if (showPointMasses)
                {
                    Gizmos.color = pointColor;
                    foreach (IPointMass point in points)
                    {
                        Gizmos.DrawSphere(point.Position, pointSize);

                        // Draw different color for fixed points
                        if (point.IsFixed)
                        {
                            Gizmos.color = Color.blue;
                            Gizmos.DrawWireSphere(point.Position, pointSize * 1.2f);
                            Gizmos.color = pointColor;
                        }
                    }
                }

                // Draw constraints (connections between points)
                if (showConstraints)
                {
                    Gizmos.color = constraintColor;
                    for (int i = 0; i < points.Count; i++)
                    {
                        Vector2 p1 = points[i].Position;
                        Vector2 p2 = points[(i + 1) % points.Count].Position;
                        Gizmos.DrawLine(p1, p2);
                    }
                }

                // Draw center
                if (showCenters)
                {
                    Gizmos.color = centerColor;
                    Gizmos.DrawSphere(center, pointSize * 1.5f);
                }

                // Draw velocities
                if (showVelocities)
                {
                    Gizmos.color = velocityColor;
                    foreach (IPointMass point in points)
                    {
                        Vector2 velocity = point.Velocity;
                        Gizmos.DrawLine(point.Position, point.Position + velocity * velocityScale);

                        // Draw arrow tip
                        Vector2 arrowTip = point.Position + velocity * velocityScale;
                        if (velocity.magnitude > 0.01f)
                        {
                            Vector2 arrowDir = velocity.normalized;
                            Vector2 arrowLeft = arrowTip - arrowDir * 0.1f + new Vector2(arrowDir.y, -arrowDir.x) * 0.05f;
                            Vector2 arrowRight = arrowTip - arrowDir * 0.1f - new Vector2(arrowDir.y, -arrowDir.x) * 0.05f;

                            Gizmos.DrawLine(arrowTip, arrowLeft);
                            Gizmos.DrawLine(arrowTip, arrowRight);
                        }
                    }
                }

                // Draw gravity
                if (showGravity && SoftBodyPhysicsManager.Instance != null)
                {
                    Gizmos.color = gravityColor;
                    Vector2 gravity = SoftBodyPhysicsManager.Instance.gravityDirection * SoftBodyPhysicsManager.Instance.gravity;

                    foreach (IPointMass point in points)
                    {
                        Vector2 force = gravity * point.Mass;
                        Gizmos.DrawLine(point.Position, point.Position + force * velocityScale * 0.1f);
                    }
                }

                // Draw pressure (for pressure-based bodies)
                if (showPressure)
                {
                    // Need to check type at runtime
                    if (softBody is PressureBasedBody pressureBody)
                    {
                        DrawPressureForces(pressureBody);
                    }
                }
            }
        }

        private void DrawPressureForces(PressureBasedBody pressureBody)
        {
            Gizmos.color = pressureColor;
            List<IPointMass> points = pressureBody.GetPoints();

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 current = points[i].Position;
                Vector2 prev = points[(i + points.Count - 1) % points.Count].Position;
                Vector2 next = points[(i + 1) % points.Count].Position;

                Vector2 edge = next - prev;
                Vector2 normal = new Vector2(-edge.y, edge.x).normalized;

                // Note: We can't directly access pressureFactor, so just visualize the direction
                Gizmos.DrawLine(current, current + normal * 0.2f);

                // Draw arrow tip
                Vector2 arrowTip = current + normal * 0.2f;
                Vector2 arrowLeft = arrowTip - normal * 0.05f + new Vector2(normal.y, -normal.x) * 0.03f;
                Vector2 arrowRight = arrowTip - normal * 0.05f - new Vector2(normal.y, -normal.x) * 0.03f;

                Gizmos.DrawLine(arrowTip, arrowLeft);
                Gizmos.DrawLine(arrowTip, arrowRight);
            }
        }

        [ContextMenu("Log Soft Body Info")]
        public void LogSoftBodyInfo()
        {
            FindSoftBodies();

            foreach (ISoftBodyObject softBody in softBodies)
            {
                if (softBody == null) continue;

                string bodyType = softBody.GetType().Name;
                int pointCount = softBody.GetPoints().Count;
                Vector2 center = softBody.GetCenter();
                Bounds bounds = softBody.GetBounds();

                Debug.Log($"Body: {bodyType}, Points: {pointCount}, Center: {center}, Size: {bounds.size}");
            }
        }
    }

    /// <summary>
    /// Performance monitor for soft body physics
    /// </summary>
    public class SoftBodyPerformanceMonitor : MonoBehaviour
    {
        [Header("Performance Monitoring")]
        public bool showStats = true;
        public bool logWarnings = true;
        public int warningThreshold = 5; // ms

        private float updateTime;
        private int softBodyCount;
        private int pointCount;
        private int constraintCount;

        private void LateUpdate()
        {
            // Measure time taken by physics
            updateTime = Time.deltaTime * 1000; // convert to ms

            // Count objects
            SoftBodyObject[] bodies = FindObjectsByType<SoftBodyObject>(FindObjectsSortMode.None);
            softBodyCount = bodies.Length;

            pointCount = 0;
            constraintCount = 0;

            foreach (SoftBodyObject body in bodies)
            {
                if (body == null) continue;

                pointCount += body.GetPoints().Count;

                // We can't easily count constraints without exposing them in the interface
                // This is just an estimate
                if (body is SpringBasedBody || body is SlimeBody)
                {
                    constraintCount += body.GetPoints().Count * 2; // rough estimate
                }
            }

            // Log warnings if performance is poor
            if (logWarnings && updateTime > warningThreshold)
            {
                Debug.LogWarning($"Soft body physics taking {updateTime:F2} ms with {softBodyCount} bodies, {pointCount} points");
            }
        }

        private void OnGUI()
        {
            if (!showStats) return;

            GUILayout.BeginArea(new Rect(10, Screen.height - 100, 200, 90));
            GUILayout.Label($"Physics time: {updateTime:F2} ms");
            GUILayout.Label($"Soft bodies: {softBodyCount}");
            GUILayout.Label($"Points: {pointCount}");
            GUILayout.Label($"Constraints (est): {constraintCount}");
            GUILayout.EndArea();
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Base component for frame-based soft bodies (unique shapes)
    /// </summary>
    public class FrameBasedBody : SoftBodyObject
    {
        [Header("Frame Settings")]
        public bool useAutomaticFramePositioning = true;
        public bool useAutomaticFrameRotation = true;
        [Range(0f, 1f)]
        public float frameStiffness = 0.8f;

        protected List<Vector2> framePoints = new List<Vector2>();
        protected Vector2 frameCenterOffset;
        protected float frameRotation;

        [Header("Debug Visualization")]
        public bool showFrameReference = true;

        protected override void Initialize()
        {
            // Setup would be implemented in derived classes
        }

        public override void UpdatePhysics(float deltaTime)
        {
            base.UpdatePhysics(deltaTime);

            if (useAutomaticFramePositioning || useAutomaticFrameRotation)
            {
                UpdateFrameTransform();
            }

            ApplyFrameConstraints();
        }

        protected void UpdateFrameTransform()
        {
            Vector2 center = GetCenter();

            if (useAutomaticFramePositioning)
            {
                // Update frame position based on center
                frameCenterOffset = center - (Vector2)transform.position;
            }

            if (useAutomaticFrameRotation)
            {
                // Calculate average rotation angle
                frameRotation = CalculateAverageRotation();
            }
        }

        protected float CalculateAverageRotation()
        {
            if (points.Count != framePoints.Count) return 0f;

            float sumAngle = 0f;
            Vector2 center = GetCenter();

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 frameOffset = framePoints[i] - frameCenterOffset;
                Vector2 currentOffset = points[i].Position - center;

                // Skip near-zero vectors to avoid division by zero
                if (frameOffset.magnitude < 0.001f || currentOffset.magnitude < 0.001f)
                    continue;

                float frameAngle = Mathf.Atan2(frameOffset.y, frameOffset.x);
                float currentAngle = Mathf.Atan2(currentOffset.y, currentOffset.x);

                float angle = Mathf.DeltaAngle(frameAngle * Mathf.Rad2Deg, currentAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                sumAngle += angle;
            }

            return sumAngle / points.Count;
        }

        protected void ApplyFrameConstraints()
        {
            if (points.Count != framePoints.Count) return;

            Vector2 center = GetCenter();

            for (int i = 0; i < points.Count; i++)
            {
                // Calculate target position based on frame
                Vector2 frameOffset = framePoints[i] - frameCenterOffset;

                // Rotate the frame offset
                float cos = Mathf.Cos(frameRotation);
                float sin = Mathf.Sin(frameRotation);
                Vector2 rotatedOffset = new Vector2(
                    frameOffset.x * cos - frameOffset.y * sin,
                    frameOffset.x * sin + frameOffset.y * cos
                );

                // Target position
                Vector2 targetPos = center + rotatedOffset;

                // Apply constraint
                Vector2 delta = targetPos - points[i].Position;
                points[i].ApplyDisplacement(delta * frameStiffness);
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
            // Similar collision handling as other types
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
            if (!Application.isPlaying || !showFrameReference || framePoints.Count == 0) return;

            Vector2 center = GetCenter();

            // Draw frame reference points
            Gizmos.color = Color.blue;
            for (int i = 0; i < framePoints.Count; i++)
            {
                // Calculate position relative to current center
                Vector2 frameOffset = framePoints[i] - frameCenterOffset;

                // Rotate the frame offset
                float cos = Mathf.Cos(frameRotation);
                float sin = Mathf.Sin(frameRotation);
                Vector2 rotatedOffset = new Vector2(
                    frameOffset.x * cos - frameOffset.y * sin,
                    frameOffset.x * sin + frameOffset.y * cos
                );

                // Target position
                Vector2 targetPos = center + rotatedOffset;

                // Draw target position
                Gizmos.DrawSphere(targetPos, 0.1f);

                // Draw line to actual point
                if (i < points.Count)
                {
                    Gizmos.DrawLine(targetPos, points[i].Position);
                }
            }

            // Draw frame connections
            Gizmos.color = Color.green;
            for (int i = 0; i < framePoints.Count; i++)
            {
                Vector2 frameOffset1 = framePoints[i] - frameCenterOffset;
                Vector2 frameOffset2 = framePoints[(i + 1) % framePoints.Count] - frameCenterOffset;

                // Rotate the frame offsets
                float cos = Mathf.Cos(frameRotation);
                float sin = Mathf.Sin(frameRotation);

                Vector2 rotatedOffset1 = new Vector2(
                    frameOffset1.x * cos - frameOffset1.y * sin,
                    frameOffset1.x * sin + frameOffset1.y * cos
                );

                Vector2 rotatedOffset2 = new Vector2(
                    frameOffset2.x * cos - frameOffset2.y * sin,
                    frameOffset2.x * sin + frameOffset2.y * cos
                );

                // Target positions
                Vector2 targetPos1 = center + rotatedOffset1;
                Vector2 targetPos2 = center + rotatedOffset2;

                // Draw line between frame points
                Gizmos.DrawLine(targetPos1, targetPos2);
            }
        }
    }
}
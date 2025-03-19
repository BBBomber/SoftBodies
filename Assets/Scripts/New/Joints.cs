using System.Collections.Generic;
using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Weld joint that keeps two points at the same position
    /// </summary>
    public class WeldJoint : MonoBehaviour, IJoint
    {
        public SoftBodyObject objectA;
        public SoftBodyObject objectB;
        public int pointIndexA;
        public int pointIndexB;

        public bool IsActive { get; set; } = true;

        public ISoftBodyObject ObjectA => objectA;
        public ISoftBodyObject ObjectB => objectB;

        private IPointMass pointA;
        private IPointMass pointB;

        private void Start()
        {
            if (objectA == null || objectB == null)
            {
                Debug.LogError("WeldJoint: Both objects must be assigned");
                return;
            }

            var pointsA = objectA.GetPoints();
            var pointsB = objectB.GetPoints();

            if (pointIndexA >= 0 && pointIndexA < pointsA.Count &&
                pointIndexB >= 0 && pointIndexB < pointsB.Count)
            {
                pointA = pointsA[pointIndexA];
                pointB = pointsB[pointIndexB];

                SoftBodyPhysicsManager.Instance?.RegisterJoint(this);
            }
            else
            {
                Debug.LogError("WeldJoint: Point indices out of range");
            }
        }

        private void OnDestroy()
        {
            SoftBodyPhysicsManager.Instance?.UnregisterJoint(this);
        }

        public void UpdateJoint()
        {
            if (!IsActive || pointA == null || pointB == null) return;

            // Calculate average position
            Vector2 averagePos = (pointA.Position + pointB.Position) * 0.5f;

            // Move both points to the average position
            if (!pointA.IsFixed)
                pointA.Position = averagePos;

            if (!pointB.IsFixed)
                pointB.Position = averagePos;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !IsActive || pointA == null || pointB == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(pointA.Position, pointB.Position);
            Gizmos.DrawSphere(pointA.Position, 0.12f);
        }
    }

    /// <summary>
    /// Weighted joint that connects the weighted center of a group of points from two objects
    /// </summary>
    public class WeightedJoint : MonoBehaviour, IJoint
    {
        public SoftBodyObject objectA;
        public SoftBodyObject objectB;
        public List<int> pointIndicesA = new List<int>();
        public List<float> weightsA = new List<float>();
        public List<int> pointIndicesB = new List<int>();
        public List<float> weightsB = new List<float>();

        [Range(0f, 1f)]
        public float stiffness = 1.0f;

        public bool IsActive { get; set; } = true;

        public ISoftBodyObject ObjectA => objectA;
        public ISoftBodyObject ObjectB => objectB;

        private List<IPointMass> pointsA = new List<IPointMass>();
        private List<IPointMass> pointsB = new List<IPointMass>();

        private void Start()
        {
            if (objectA == null || objectB == null)
            {
                Debug.LogError("WeightedJoint: Both objects must be assigned");
                return;
            }

            if (pointIndicesA.Count != weightsA.Count || pointIndicesB.Count != weightsB.Count)
            {
                Debug.LogError("WeightedJoint: Point indices and weights must have the same count");
                return;
            }

            var allPointsA = objectA.GetPoints();
            var allPointsB = objectB.GetPoints();

            // Gather points based on indices
            bool valid = true;
            pointsA.Clear();
            pointsB.Clear();

            for (int i = 0; i < pointIndicesA.Count; i++)
            {
                if (pointIndicesA[i] < 0 || pointIndicesA[i] >= allPointsA.Count)
                {
                    valid = false;
                    Debug.LogError($"WeightedJoint: Point index {pointIndicesA[i]} out of range for object A");
                    break;
                }
                pointsA.Add(allPointsA[pointIndicesA[i]]);
            }

            for (int i = 0; i < pointIndicesB.Count; i++)
            {
                if (pointIndicesB[i] < 0 || pointIndicesB[i] >= allPointsB.Count)
                {
                    valid = false;
                    Debug.LogError($"WeightedJoint: Point index {pointIndicesB[i]} out of range for object B");
                    break;
                }
                pointsB.Add(allPointsB[pointIndicesB[i]]);
            }

            if (valid)
            {
                // Normalize weights
                NormalizeWeights(weightsA);
                NormalizeWeights(weightsB);

                SoftBodyPhysicsManager.Instance?.RegisterJoint(this);
            }
        }

        private void NormalizeWeights(List<float> weights)
        {
            float sum = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                sum += weights[i];
            }

            if (sum > 0)
            {
                for (int i = 0; i < weights.Count; i++)
                {
                    weights[i] /= sum;
                }
            }
            else
            {
                // Equal weights if sum is zero
                float equalWeight = 1.0f / weights.Count;
                for (int i = 0; i < weights.Count; i++)
                {
                    weights[i] = equalWeight;
                }
            }
        }

        private void OnDestroy()
        {
            SoftBodyPhysicsManager.Instance?.UnregisterJoint(this);
        }

        public void UpdateJoint()
        {
            if (!IsActive || pointsA.Count == 0 || pointsB.Count == 0) return;

            // Calculate weighted centers
            Vector2 centerA = CalculateWeightedCenter(pointsA, weightsA);
            Vector2 centerB = CalculateWeightedCenter(pointsB, weightsB);

            // Calculate correction
            Vector2 delta = centerB - centerA;

            // Apply correction to both sets of points
            ApplyCorrection(pointsA, weightsA, delta * stiffness * 0.5f);
            ApplyCorrection(pointsB, weightsB, -delta * stiffness * 0.5f);
        }

        private Vector2 CalculateWeightedCenter(List<IPointMass> points, List<float> weights)
        {
            Vector2 center = Vector2.zero;
            for (int i = 0; i < points.Count; i++)
            {
                center += points[i].Position * weights[i];
            }
            return center;
        }

        private void ApplyCorrection(List<IPointMass> points, List<float> weights, Vector2 correction)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (!points[i].IsFixed)
                {
                    points[i].ApplyDisplacement(correction * weights[i]);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !IsActive || pointsA.Count == 0 || pointsB.Count == 0) return;

            // Draw weighted centers
            Vector2 centerA = CalculateWeightedCenter(pointsA, weightsA);
            Vector2 centerB = CalculateWeightedCenter(pointsB, weightsB);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(centerA, 0.15f);
            Gizmos.DrawSphere(centerB, 0.15f);
            Gizmos.DrawLine(centerA, centerB);

            // Draw connections to points
            Gizmos.color = new Color(0, 0.8f, 0, 0.3f); // Transparent green

            for (int i = 0; i < pointsA.Count; i++)
            {
                Gizmos.DrawLine(centerA, pointsA[i].Position);
            }

            for (int i = 0; i < pointsB.Count; i++)
            {
                Gizmos.DrawLine(centerB, pointsB[i].Position);
            }
        }
    }

    /// <summary>
    /// Fixed joint that locks specific points in place
    /// </summary>
    public class FixedPointJoint : MonoBehaviour
    {
        public SoftBodyObject targetObject;
        public List<int> pointIndices = new List<int>();

        private List<IPointMass> points = new List<IPointMass>();
        private List<Vector2> fixedPositions = new List<Vector2>();

        public bool IsActive { get; set; } = true;

        private void Start()
        {
            if (targetObject == null)
            {
                Debug.LogError("FixedPointJoint: Target object must be assigned");
                return;
            }

            var allPoints = targetObject.GetPoints();

            // Validate and store points
            points.Clear();
            fixedPositions.Clear();

            for (int i = 0; i < pointIndices.Count; i++)
            {
                if (pointIndices[i] < 0 || pointIndices[i] >= allPoints.Count)
                {
                    Debug.LogError($"FixedPointJoint: Point index {pointIndices[i]} out of range");
                    continue;
                }

                IPointMass point = allPoints[pointIndices[i]];
                points.Add(point);
                fixedPositions.Add(point.Position);
                point.IsFixed = true;
            }
        }

        private void Update()
        {
            if (!IsActive) return;

            // Ensure points stay fixed
            for (int i = 0; i < points.Count; i++)
            {
                points[i].Position = fixedPositions[i];
                points[i].PreviousPosition = fixedPositions[i];
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !IsActive || points.Count == 0) return;

            Gizmos.color = Color.blue;

            for (int i = 0; i < points.Count; i++)
            {
                Gizmos.DrawSphere(fixedPositions[i], 0.15f);

                // Draw a cross to indicate fixed point
                Vector2 pos = fixedPositions[i];
                float size = 0.1f;

                Gizmos.DrawLine(new Vector2(pos.x - size, pos.y - size),
                                new Vector2(pos.x + size, pos.y + size));
                Gizmos.DrawLine(new Vector2(pos.x - size, pos.y + size),
                                new Vector2(pos.x + size, pos.y - size));
            }
        }
    }
}
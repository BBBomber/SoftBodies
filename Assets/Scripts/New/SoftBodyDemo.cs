using System.Collections.Generic;
using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Demo script to set up a soft body physics scene
    /// </summary>
    public class SoftBodyDemo : MonoBehaviour
    {
        [Header("Slime Setup")]
        public GameObject slimePrefab;
        public Vector2 slimeStartPosition = new Vector2(0, 2);

        [Header("Circle Objects")]
        public int circleCount = 3;
        public float minCircleRadius = 0.5f;
        public float maxCircleRadius = 1.5f;
        public Vector2 circleSpawnArea = new Vector2(8, 4);

        [Header("Unique Shape Objects")]
        public int shapeCount = 2;
        public GameObject[] shapeTemplates;

        [Header("Joints Demo")]
        public bool createJointedObjects = true;
        public float jointDistance = 3f;

        private void Start()
        {
            // Ensure there's a physics manager
            if (SoftBodyPhysicsManager.Instance == null)
            {
                GameObject managerObj = new GameObject("SoftBodyPhysicsManager");
                managerObj.AddComponent<SoftBodyPhysicsManager>();
            }

            // Create the slime player if needed
            if (slimePrefab != null)
            {
                GameObject slimeObj = Instantiate(slimePrefab, slimeStartPosition, Quaternion.identity);
                slimeObj.name = "PlayerSlime";

                // Add controller if it doesn't exist
                if (slimeObj.GetComponent<SlimeCharController>() == null)
                {
                    SlimeCharController controller = slimeObj.AddComponent<SlimeCharController>();
                    controller.slimeBody = slimeObj.GetComponent<SlimeBody>();
                }
            }
            else
            {
                // Create a default slime
                GameObject slimeObj = new GameObject("PlayerSlime");
                slimeObj.transform.position = slimeStartPosition;

                SlimeBody slimeBody = slimeObj.AddComponent<SlimeBody>();
                slimeBody.numPoints = 16;
                slimeBody.radius = 1f;
                slimeBody.puffiness = 1.5f;

                SlimeCharController controller = slimeObj.AddComponent<SlimeCharController>();
                controller.slimeBody = slimeBody;
            }

            // Create circle objects
            for (int i = 0; i < circleCount; i++)
            {
                CreateCircleObject(i);
            }

            // Create unique shape objects
            for (int i = 0; i < shapeCount; i++)
            {
                CreateUniqueShapeObject(i);
            }

            // Create jointed objects
            if (createJointedObjects)
            {
                CreateJointedObjects();
            }
        }

        private void CreateCircleObject(int index)
        {
            float radius = Random.Range(minCircleRadius, maxCircleRadius);
            Vector2 position = new Vector2(
                Random.Range(-circleSpawnArea.x / 2, circleSpawnArea.x / 2),
                Random.Range(1, circleSpawnArea.y)
            );

            GameObject circleObj = new GameObject($"Circle_{index}");
            circleObj.transform.position = position;

            CircularSpringBody body = circleObj.AddComponent<CircularSpringBody>();
            body.radius = radius;
            body.numPoints = Mathf.Max(8, Mathf.RoundToInt(radius * 8)); // More points for bigger circles
            body.springStiffness = Random.Range(0.3f, 0.7f);
            body.useGasPressure = Random.value > 0.3f; // 70% chance to use pressure
            body.pressureAmount = Random.Range(0.8f, 1.5f);
            body.fillColor = new Color(
                Random.Range(0.2f, 0.9f),
                Random.Range(0.2f, 0.9f),
                Random.Range(0.2f, 0.9f),
                0.5f
            );
        }

        private void CreateUniqueShapeObject(int index)
        {
            Vector2 position = new Vector2(
                Random.Range(-circleSpawnArea.x / 2, circleSpawnArea.x / 2),
                Random.Range(1, circleSpawnArea.y)
            );

            GameObject shapeObj = new GameObject($"Shape_{index}");
            shapeObj.transform.position = position;

            UniqueShapeBody body = shapeObj.AddComponent<UniqueShapeBody>();

            // Generate a random shape if no templates
            if (shapeTemplates == null || shapeTemplates.Length == 0 || index >= shapeTemplates.Length)
            {
                int pointCount = Random.Range(3, 8);
                Vector2[] points = new Vector2[pointCount];

                float size = Random.Range(0.5f, 1.5f);
                for (int i = 0; i < pointCount; i++)
                {
                    float angle = 2 * Mathf.PI * i / pointCount;
                    float dist = size * (0.7f + 0.3f * Mathf.Sin(angle * 3)); // Make it bumpy
                    points[i] = new Vector2(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist);
                }

                body.shapePoints = points;
            }
            else
            {
                // Use template if available
                GameObject template = shapeTemplates[index % shapeTemplates.Length];

                // Get points from the template if it has a collider
                PolygonCollider2D polyCollider = template.GetComponent<PolygonCollider2D>();
                if (polyCollider != null)
                {
                    body.shapePoints = polyCollider.points;
                }
                else
                {
                    // Default triangle shape
                    body.shapePoints = new Vector2[] {
                        new Vector2(-1, -1),
                        new Vector2(1, -1),
                        new Vector2(0, 1)
                    };
                }
            }

            body.frameStiffness = Random.Range(0.3f, 0.8f);
            body.usePerimeterSprings = true;
            body.useInternalSprings = Random.value > 0.5f;
            body.fillColor = new Color(
                Random.Range(0.2f, 0.9f),
                Random.Range(0.2f, 0.9f),
                Random.Range(0.2f, 0.9f),
                0.5f
            );
        }

        private void CreateJointedObjects()
        {
            // Create two objects joined together
            Vector2 position = new Vector2(0, 4);

            // Circle object
            GameObject circleObj = new GameObject("JointedCircle");
            circleObj.transform.position = new Vector2(position.x - jointDistance / 2, position.y);

            CircularSpringBody circleBody = circleObj.AddComponent<CircularSpringBody>();
            circleBody.radius = 0.7f;
            circleBody.numPoints = 12;
            circleBody.springStiffness = 0.7f;
            circleBody.useGasPressure = true;
            circleBody.pressureAmount = 1.2f;
            circleBody.fillColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);

            // Shape object
            GameObject shapeObj = new GameObject("JointedShape");
            shapeObj.transform.position = new Vector2(position.x + jointDistance / 2, position.y);

            UniqueShapeBody shapeBody = shapeObj.AddComponent<UniqueShapeBody>();
            shapeBody.shapePoints = new Vector2[] {
                new Vector2(-0.7f, -0.7f),
                new Vector2(0.7f, -0.7f),
                new Vector2(0.7f, 0.7f),
                new Vector2(-0.7f, 0.7f)
            };
            shapeBody.frameStiffness = 0.6f;
            shapeBody.usePerimeterSprings = true;
            shapeBody.fillColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);

            // Create a weighted joint
            GameObject jointObj = new GameObject("WeightedJoint");
            WeightedJoint joint = jointObj.AddComponent<WeightedJoint>();

            joint.objectA = circleBody;
            joint.objectB = shapeBody;

            // Add points to connect (right side of circle, left side of shape)
            joint.pointIndicesA = new List<int> { 0, 1, 2, 3 }; // First few points, will be fixed later
            joint.weightsA = new List<float> { 1, 1, 1, 1 };

            joint.pointIndicesB = new List<int> { 0, 3 }; // Left points of the rectangle
            joint.weightsB = new List<float> { 1, 1 };

            joint.stiffness = 0.8f;

            // We'll fix the indices in Start of the joint
            StartCoroutine(FixJointIndicesAfterInitialization(joint));
        }

        private System.Collections.IEnumerator FixJointIndicesAfterInitialization(WeightedJoint joint)
        {
            // Wait a frame to make sure the bodies are initialized
            yield return null;

            // For circle, find points on the right side
            CircularSpringBody circle = joint.objectA as CircularSpringBody;
            if (circle != null && circle.GetPoints().Count > 0)
            {
                List<int> rightSidePoints = new List<int>();
                Vector2 center = circle.GetCenter();

                for (int i = 0; i < circle.GetPoints().Count; i++)
                {
                    Vector2 point = circle.GetPoints()[i].Position;
                    // Points on the right side (x > center.x)
                    if (point.x > center.x)
                    {
                        rightSidePoints.Add(i);
                    }
                }

                // Use up to 4 points from the right side
                joint.pointIndicesA.Clear();
                for (int i = 0; i < Mathf.Min(4, rightSidePoints.Count); i++)
                {
                    joint.pointIndicesA.Add(rightSidePoints[i]);
                }

                joint.weightsA.Clear();
                for (int i = 0; i < joint.pointIndicesA.Count; i++)
                {
                    joint.weightsA.Add(1f);
                }
            }

            // For shape, find left side points
            UniqueShapeBody shape = joint.objectB as UniqueShapeBody;
            if (shape != null && shape.GetPoints().Count > 0)
            {
                List<int> leftSidePoints = new List<int>();
                Vector2 center = shape.GetCenter();

                for (int i = 0; i < shape.GetPoints().Count; i++)
                {
                    Vector2 point = shape.GetPoints()[i].Position;
                    // Points on the left side (x < center.x)
                    if (point.x < center.x)
                    {
                        leftSidePoints.Add(i);
                    }
                }

                // Use up to 2 points from the left side
                joint.pointIndicesB.Clear();
                for (int i = 0; i < Mathf.Min(2, leftSidePoints.Count); i++)
                {
                    joint.pointIndicesB.Add(leftSidePoints[i]);
                }

                joint.weightsB.Clear();
                for (int i = 0; i < joint.pointIndicesB.Count; i++)
                {
                    joint.weightsB.Add(1f);
                }
            }
        }
    }
}
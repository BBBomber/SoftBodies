using System.Collections.Generic;
using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Factory class for creating soft bodies from colliders and other shapes
    /// </summary>
    public static class SoftBodyFactory
    {
        /// <summary>
        /// Creates a soft body from a polygon collider
        /// </summary>
        public static UniqueShapeBody CreateFromPolygonCollider(GameObject source, bool preserveCollider = false)
        {
            PolygonCollider2D collider = source.GetComponent<PolygonCollider2D>();
            if (collider == null)
            {
                Debug.LogError("CreateFromPolygonCollider: Source object does not have a PolygonCollider2D");
                return null;
            }

            // Create a new game object for the soft body
            GameObject softBodyObj = new GameObject(source.name + "_SoftBody");
            softBodyObj.transform.position = source.transform.position;
            softBodyObj.transform.rotation = source.transform.rotation;
            softBodyObj.transform.localScale = source.transform.localScale;

            // Add soft body component
            UniqueShapeBody softBody = softBodyObj.AddComponent<UniqueShapeBody>();

            // Copy the points from the collider
            softBody.shapePoints = new Vector2[collider.points.Length];
            for (int i = 0; i < collider.points.Length; i++)
            {
                softBody.shapePoints[i] = collider.points[i];
            }

            // Setup additional properties
            softBody.usePerimeterSprings = true;
            softBody.useInternalSprings = collider.points.Length > 5; // Use internal springs for complex shapes

            // Copy renderer properties if available
            SpriteRenderer sourceRenderer = source.GetComponent<SpriteRenderer>();
            if (sourceRenderer != null)
            {
                // Add a line renderer to visualize the soft body
                LineRenderer lineRenderer = softBodyObj.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.1f;
                lineRenderer.endWidth = 0.1f;
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = sourceRenderer.color;
                lineRenderer.endColor = sourceRenderer.color;

                softBody.fillColor = new Color(
                    sourceRenderer.color.r,
                    sourceRenderer.color.g,
                    sourceRenderer.color.b,
                    0.5f
                );

                softBody.drawFilled = true;
            }

            // Disable or remove the original collider
            if (!preserveCollider)
            {
                collider.enabled = false;
            }

            return softBody;
        }

        /// <summary>
        /// Creates a circular soft body from a circle collider
        /// </summary>
        public static CircularSpringBody CreateFromCircleCollider(GameObject source, bool preserveCollider = false)
        {
            CircleCollider2D collider = source.GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                Debug.LogError("CreateFromCircleCollider: Source object does not have a CircleCollider2D");
                return null;
            }

            // Create a new game object for the soft body
            GameObject softBodyObj = new GameObject(source.name + "_SoftBody");
            softBodyObj.transform.position = source.transform.position;

            // Add soft body component
            CircularSpringBody softBody = softBodyObj.AddComponent<CircularSpringBody>();

            // Copy properties from the collider
            softBody.radius = collider.radius * source.transform.localScale.x;
            softBody.numPoints = Mathf.Max(8, Mathf.RoundToInt(softBody.radius * 8));

            // Setup additional properties
            softBody.createPerimeterSprings = true;
            softBody.createInternalSprings = false;
            //softBody.useGasPressure = true;

            // Copy renderer properties if available
            SpriteRenderer sourceRenderer = source.GetComponent<SpriteRenderer>();
            if (sourceRenderer != null)
            {
                softBody.fillColor = new Color(
                    sourceRenderer.color.r,
                    sourceRenderer.color.g,
                    sourceRenderer.color.b,
                    0.5f
                );

                softBody.drawFilled = true;
            }

            // Disable or remove the original collider
            if (!preserveCollider)
            {
                collider.enabled = false;
            }

            return softBody;
        }

        /// <summary>
        /// Creates a slime body with the specified parameters
        /// </summary>
        public static SlimeBody CreateSlime(Vector2 position, float radius, int numPoints, float puffiness = 1.5f)
        {
            GameObject slimeObj = new GameObject("Slime");
            slimeObj.transform.position = position;

            SlimeBody slime = slimeObj.AddComponent<SlimeBody>();
            slime.radius = radius;
            slime.numPoints = numPoints;
            slime.puffiness = puffiness;

            return slime;
        }

        /// <summary>
        /// Creates a weld joint between two points of different soft bodies
        /// </summary>
        public static WeldJoint CreateWeldJoint(SoftBodyObject bodyA, int pointIndexA, SoftBodyObject bodyB, int pointIndexB)
        {
            GameObject jointObj = new GameObject("WeldJoint");
            WeldJoint joint = jointObj.AddComponent<WeldJoint>();

            joint.objectA = bodyA;
            joint.objectB = bodyB;
            joint.pointIndexA = pointIndexA;
            joint.pointIndexB = pointIndexB;

            return joint;
        }

        /// <summary>
        /// Creates a weighted joint between groups of points on two soft bodies
        /// </summary>
        public static WeightedJoint CreateWeightedJoint(
            SoftBodyObject bodyA, List<int> pointIndicesA, List<float> weightsA,
            SoftBodyObject bodyB, List<int> pointIndicesB, List<float> weightsB,
            float stiffness = 1.0f
        )
        {
            GameObject jointObj = new GameObject("WeightedJoint");
            WeightedJoint joint = jointObj.AddComponent<WeightedJoint>();

            joint.objectA = bodyA;
            joint.objectB = bodyB;
            joint.pointIndicesA = new List<int>(pointIndicesA);
            joint.weightsA = new List<float>(weightsA);
            joint.pointIndicesB = new List<int>(pointIndicesB);
            joint.weightsB = new List<float>(weightsB);
            joint.stiffness = stiffness;

            return joint;
        }

        /// <summary>
        /// Creates fixed point constraints for a soft body
        /// </summary>
        public static FixedPointJoint CreateFixedPointJoint(SoftBodyObject body, List<int> pointIndices)
        {
            GameObject jointObj = new GameObject("FixedPointJoint");
            FixedPointJoint joint = jointObj.AddComponent<FixedPointJoint>();

            joint.targetObject = body;
            joint.pointIndices = new List<int>(pointIndices);

            return joint;
        }
    }

    /// <summary>
    /// Editor tools for creating soft bodies
    /// </summary>
    public class SoftBodyCreator : MonoBehaviour
    {
        public GameObject[] sourceObjects;
        public bool convertOnStart = false;
        public bool preserveOriginalColliders = false;

        private void Start()
        {
            if (convertOnStart)
            {
                ConvertAllSourceObjects();
            }
        }

        [ContextMenu("Convert All Source Objects")]
        public void ConvertAllSourceObjects()
        {
            if (sourceObjects == null || sourceObjects.Length == 0)
            {
                Debug.LogWarning("No source objects assigned");
                return;
            }

            foreach (GameObject source in sourceObjects)
            {
                if (source == null) continue;

                if (source.GetComponent<CircleCollider2D>() != null)
                {
                    SoftBodyFactory.CreateFromCircleCollider(source, preserveOriginalColliders);
                }
                else if (source.GetComponent<PolygonCollider2D>() != null)
                {
                    SoftBodyFactory.CreateFromPolygonCollider(source, preserveOriginalColliders);
                }
                else
                {
                    Debug.LogWarning($"Object {source.name} does not have a supported collider type");
                }
            }
        }
    }
}
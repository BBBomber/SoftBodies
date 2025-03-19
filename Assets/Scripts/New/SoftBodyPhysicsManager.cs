using System.Collections.Generic;
using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Central manager for the soft body physics system
    /// </summary>
    public class SoftBodyPhysicsManager : MonoBehaviour
    {
        public static SoftBodyPhysicsManager Instance { get; private set; }

        [Header("Physics Settings")]
        public float gravity = 9.8f;
        public Vector2 gravityDirection = Vector2.down;
        public int solverIterations = 10;
        public float fixedTimeStep = 0.016f;

        [Header("Global Collision Settings")]
        public bool enableCollisions = true;
        public int broadphaseSubdivisions = 10;


        [Header("Stabilization Settings")]
        [Range(0.7f, 1.0f)]
        public float lateralDampingFactor = 0.92f;

        private List<ISoftBodyObject> softBodyObjects = new List<ISoftBodyObject>();
        private List<IJoint> joints = new List<IJoint>();
        private CollisionManager collisionManager;



        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                collisionManager = new CollisionManager(broadphaseSubdivisions);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void FixedUpdate()
        {
            // Update all soft body objects
            foreach (var softBody in softBodyObjects)
            {
                softBody.UpdatePhysics(fixedTimeStep);
            }
            ApplyGlobalLateralDamping();
            // Handle collisions
            if (enableCollisions)
            {
                collisionManager.DetectAndResolveCollisions(softBodyObjects);
            }

            // Update joints
            foreach (var joint in joints)
            {
                joint.UpdateJoint();
            }
        }

        public void RegisterSoftBody(ISoftBodyObject softBody)
        {
            if (!softBodyObjects.Contains(softBody))
            {
                softBodyObjects.Add(softBody);
            }
        }

        public void UnregisterSoftBody(ISoftBodyObject softBody)
        {
            softBodyObjects.Remove(softBody);
        }

        public void RegisterJoint(IJoint joint)
        {
            if (!joints.Contains(joint))
            {
                joints.Add(joint);
            }
        }

        public void UnregisterJoint(IJoint joint)
        {
            joints.Remove(joint);
        }

        // Add this new method
        private void ApplyGlobalLateralDamping()
        {
            foreach (var softBody in softBodyObjects)
            {
                // Skip if the soft body isn't using pressure (like spring-based bodies)
                if (!(softBody is PressureBasedBody))
                    continue;

                List<IPointMass> points = softBody.GetPoints();
                Vector2 center = softBody.GetCenter();

                foreach (IPointMass point in points)
                {
                    // Get velocity
                    Vector2 velocity = point.Position - point.PreviousPosition;

                    // Apply stronger damping to x component (horizontal)
                    Vector2 dampedVelocity = new Vector2(velocity.x * lateralDampingFactor, velocity.y);

                    // Update previous position to reflect damped velocity
                    point.PreviousPosition = point.Position - dampedVelocity;
                }
            }
        }
    }
}
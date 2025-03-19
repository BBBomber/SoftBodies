using System.Collections.Generic;
using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Base class for all soft body objects
    /// </summary>
    public abstract class SoftBodyObject : MonoBehaviour, ISoftBodyObject
    {
        [Header("Physics Settings")]
        public bool isCollidable = true;
        public int collisionLayer = 0;
        public float mass = 1f;
        public float linearDamping = 0.99f;

        [Header("Solver Settings")]
        public int constraintIterations = 10;

        protected List<IPointMass> points = new List<IPointMass>();
        protected List<IConstraint> constraints = new List<IConstraint>();

        protected virtual void Awake()
        {
            Initialize();
        }

        protected virtual void Start()
        {
            // Register with physics manager
            if (SoftBodyPhysicsManager.Instance != null)
            {
                SoftBodyPhysicsManager.Instance.RegisterSoftBody(this);
            }
            else
            {
                Debug.LogWarning("No SoftBodyPhysicsManager found in the scene. Physics won't be updated globally.");
            }
        }

        protected virtual void OnDestroy()
        {
            // Unregister from physics manager
            if (SoftBodyPhysicsManager.Instance != null)
            {
                SoftBodyPhysicsManager.Instance.UnregisterSoftBody(this);
            }
        }

        protected abstract void Initialize();

        public bool IsCollidable => isCollidable;
        public int CollisionLayer => collisionLayer;

        public virtual void UpdatePhysics(float deltaTime)
        {
            // Apply gravity
            if (SoftBodyPhysicsManager.Instance != null)
            {
                Vector2 gravity = SoftBodyPhysicsManager.Instance.gravityDirection * SoftBodyPhysicsManager.Instance.gravity;
                foreach (var point in points)
                {
                    point.ApplyForce((gravity * point.Mass) * 200);
                }
            }

            // Integrate points
            foreach (var point in points)
            {
                point.VerletIntegrate(deltaTime, linearDamping);
            }

            // Solve constraints multiple times
            for (int i = 0; i < constraintIterations; i++)
            {
                foreach (var constraint in constraints)
                {
                    constraint.Solve();
                }
            }

            // Update transform position based on center
            transform.position = GetCenter();
        }

        public abstract Vector2 GetCenter();

        public virtual Bounds GetBounds()
        {
            if (points.Count == 0) return new Bounds();

            Vector2 min = points[0].Position;
            Vector2 max = points[0].Position;

            for (int i = 1; i < points.Count; i++)
            {
                min = Vector2.Min(min, points[i].Position);
                max = Vector2.Max(max, points[i].Position);
            }

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        public virtual List<IPointMass> GetPoints()
        {
            return points;
        }

        public abstract void HandleCollision(ISoftBodyObject other);

        protected float CalculateArea()
        {
            // Calculate area using shoelace formula
            float area = 0;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p1 = points[i].Position;
                Vector2 p2 = points[(i + 1) % points.Count].Position;
                area += (p1.x * p2.y - p2.x * p1.y);
            }
            return Mathf.Abs(area) / 2f;
        }

        // Helper methods for visualization
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || points.Count == 0) return;

            // Draw points
            Gizmos.color = Color.yellow;
            foreach (var point in points)
            {
                Gizmos.DrawSphere(point.Position, 0.1f);
            }

            // Draw connections
            Gizmos.color = Color.cyan;
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
using System.Collections.Generic;
using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Slime body component - ported from your existing Blob class
    /// </summary>
    public class SlimeBody : PressureBasedBody
    {
        [Header("Slime Parameters")]
        public int numPoints = 16;
        public float radius = 1f;
        public float puffiness = 1.5f;
        public float bounceFactor = 0.8f;
        public float jellyStrength = 0.1f;
        public float maxVelocity = 3f;

        [Header("Slime Features")]
        public bool enableSprings = true;
        public bool enablePressure = true;
        public bool enableJellyEffect = false;
        public bool enableSpringDragging = true;
        public bool enableCollisions = true;
        public bool enableSoftBodyCollisions = true;

        [Header("Visualization")]
        public Color slimeColor = new Color(0.3f, 0.8f, 1f, 0.7f);
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh slimeMesh;

        public List<SlimePoint> SlimePoints { get; private set; } = new List<SlimePoint>();

        private float circumference;
        private float chordLength;
        private Dictionary<Collider2D, Vector2> lastColliderPositions = new Dictionary<Collider2D, Vector2>();

        [SerializeField] private List<Collider2D> solidObjects = new List<Collider2D>();
        [SerializeField] private List<SlimeBody> otherSlimes = new List<SlimeBody>();

        protected override void Awake()
        {
            // Find solid objects in the scene
            solidObjects.AddRange(FindObjectsByType<Collider2D>(FindObjectsSortMode.None));

            // Initialize the slime
            base.Awake();
        }



        protected override void Initialize()
        {
            // Calculate slime properties
            circumference = radius * 2 * Mathf.PI;
            chordLength = circumference / numPoints;
            targetArea = radius * radius * Mathf.PI * puffiness;

            // Create points
            SlimePoints.Clear();
            points.Clear();

            for (int i = 0; i < numPoints; i++)
            {
                float angle = 2 * Mathf.PI * i / numPoints - Mathf.PI / 2;
                Vector2 offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                SlimePoint point = new SlimePoint(
                    (Vector2)transform.position + offset,
                    maxVelocity
                );

                SlimePoints.Add(point);
                points.Add(point);
            }
        }

        public override void UpdatePhysics(float deltaTime)
        {
            // Don't call base.UpdatePhysics since we're replacing it with our custom implementation

            // Get mouse input - in a real implementation, this would be handled elsewhere
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            bool isRightMousePressed = Input.GetMouseButton(1);
            bool isRightMouseReleased = Input.GetMouseButtonUp(1);

            // Get screen bounds - in a real implementation, this would be from CameraController
            Bounds screenBounds = new Bounds(
                Camera.main.transform.position,
                new Vector3(
                    Camera.main.orthographicSize * Camera.main.aspect * 2,
                    Camera.main.orthographicSize * 2,
                    0
                )
            );

            foreach (SlimePoint point in SlimePoints)
            {
                point.VerletIntegrate(deltaTime, linearDamping);
                point.ApplyGravity(SoftBodyPhysicsManager.Instance?.gravity ?? 9.8f);
            }

            // Apply internal forces with multiple substeps
            int substeps = constraintIterations;
            for (int j = 0; j < substeps; j++)
            {
                if (enableSprings) ApplySpringForces();
                if (enablePressure) ApplyPressureForce();
                if (enableJellyEffect) ApplyJellyConstraints();

                foreach (SlimePoint point in SlimePoints)
                {
                    point.ApplyDisplacement();
                    if (enableSpringDragging)
                        point.HandleMouseInteraction(mousePosition, 1f, isRightMousePressed, isRightMouseReleased);
                    point.KeepInBounds(screenBounds);
                }

                if (enableCollisions) HandleCollisions(solidObjects);
                if (enableSoftBodyCollisions) HandleSlimeCollisions(otherSlimes);
            }

            // Update transform position based on center
            transform.position = GetCenter();
        }

        private void ApplySpringForces()
        {
            for (int i = 0; i < SlimePoints.Count; i++)
            {
                SlimePoint cur = SlimePoints[i];
                SlimePoint next = SlimePoints[i == SlimePoints.Count - 1 ? 0 : i + 1];

                Vector2 diff = next.Position - cur.Position;
                float distance = diff.magnitude;

                if (distance > chordLength)
                {
                    float error = (distance - chordLength) / 2f;
                    Vector2 offset = diff.normalized * error;
                    cur.ApplyDisplacement(offset);
                    next.ApplyDisplacement(-offset);
                }
            }
        }

        private void ApplyJellyConstraints()
        {
            Vector2 center = GetCenter();
            foreach (SlimePoint point in SlimePoints)
            {
                Vector2 diff = center - point.Position;
                point.ApplyDisplacement(diff * jellyStrength);
            }
        }

        public override Vector2 GetCenter()
        {
            Vector2 sum = Vector2.zero;
            foreach (SlimePoint point in SlimePoints)
            {
                sum += point.Position;
            }
            return sum / SlimePoints.Count;
        }

        public Vector2 GetPreviousCenter()
        {
            Vector2 sum = Vector2.zero;
            foreach (SlimePoint point in SlimePoints)
            {
                sum += point.PreviousPosition;
            }
            return sum / SlimePoints.Count;
        }

        public void HandleCollisions(List<Collider2D> solidObjects)
        {
            // Track which colliders we're in contact with this frame
            HashSet<Collider2D> currentColliders = new HashSet<Collider2D>();

            foreach (SlimePoint point in SlimePoints)
            {
                foreach (Collider2D collider in solidObjects)
                {
                    // Get the closest point on the collider's surface
                    Vector2 closestPoint = collider.ClosestPoint(point.Position);
                    float distance = Vector2.Distance(point.Position, closestPoint);

                    // If the point is near or inside the collider
                    if (distance < 0.1f || collider.OverlapPoint(point.Position))
                    {
                        // Add to our tracking set
                        currentColliders.Add(collider);

                        // Calculate normal direction
                        Vector2 normal = (point.Position - closestPoint).normalized;

                        // If normal is zero (point is inside), use direction from center
                        if (normal.magnitude < 0.001f)
                        {
                            normal = (point.Position - (Vector2)collider.bounds.center).normalized;

                            // If still zero, use a default direction
                            if (normal.magnitude < 0.001f)
                            {
                                normal = Vector2.up;
                            }
                        }

                        // Push the point out
                        point.Position = closestPoint + normal * 0.1f;

                        // Reflect velocity for bouncing
                        Vector2 velocity = point.Position - point.PreviousPosition;
                        Vector2 reflectedVelocity = Vector2.Reflect(velocity, normal) * bounceFactor;
                        point.PreviousPosition = point.Position - reflectedVelocity;

                        // If on top of the collider, apply platform movement
                        if (normal.y > 0.7f)
                        {
                            ApplyPlatformMovement(point, collider);
                        }
                    }
                }
            }

            // Update last positions for next frame
            foreach (Collider2D collider in currentColliders)
            {
                lastColliderPositions[collider] = collider.bounds.center;
            }

            // Clean up colliders we're no longer in contact with
            List<Collider2D> toRemove = new List<Collider2D>();
            foreach (var kvp in lastColliderPositions)
            {
                if (!currentColliders.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var collider in toRemove)
            {
                lastColliderPositions.Remove(collider);
            }
        }

        private void ApplyPlatformMovement(SlimePoint point, Collider2D platform)
        {
            // If we have a previous position for this platform
            if (lastColliderPositions.TryGetValue(platform, out Vector2 lastPos))
            {
                // Calculate how much the platform moved
                Vector2 platformDelta = (Vector2)platform.bounds.center - lastPos;

                // Apply that movement to the blob point
                point.Position += platformDelta;
                point.PreviousPosition += platformDelta;
            }
            else
            {
                // First contact with this platform, just record its position
                lastColliderPositions[platform] = platform.bounds.center;
            }
        }

        private void HandleSlimeCollisions(List<SlimeBody> otherSlimes)
        {
            foreach (SlimeBody other in otherSlimes)
            {
                if (other == this) continue; // Don't collide with itself

                foreach (SlimePoint p1 in SlimePoints)
                {
                    foreach (SlimePoint p2 in other.SlimePoints)
                    {
                        float dist = Vector2.Distance(p1.Position, p2.Position);
                        if (dist < chordLength * 0.8f)
                        { // Prevent overlap
                            Vector2 pushDir = (p1.Position - p2.Position).normalized;
                            float penetration = (chordLength * 0.8f - dist) / 2f;
                            p1.ApplyDisplacement(pushDir * penetration);
                            p2.ApplyDisplacement(-pushDir * penetration);
                        }
                    }
                }
            }
        }

        public override void HandleCollision(ISoftBodyObject other)
        {
            // Handle collisions with other soft bodies
            foreach (SlimePoint p1 in SlimePoints)
            {
                foreach (IPointMass p2 in other.GetPoints())
                {
                    float dist = Vector2.Distance(p1.Position, p2.Position);

                    if (dist < chordLength * 0.8f)
                    {
                        Vector2 pushDir = (p1.Position - p2.Position).normalized;
                        float penetration = (chordLength * 0.8f - dist) / 2f;

                        p1.ApplyDisplacement(pushDir * penetration);
                        p2.ApplyDisplacement(-pushDir * penetration);
                    }
                }
            }
        }

        // Helper methods for setup and adjustment
        public void UpdateParameters(float dampening, float gravity, float newRadius, float puffiness,
                                     float maxDisplacement, float newMaxVelocity, bool recreatePoints = false)
        {
            linearDamping = dampening;
            radius = newRadius;
            this.puffiness = puffiness;
            maxVelocity = newMaxVelocity;

            // Update radius-dependent parameters
            circumference = radius * 2 * Mathf.PI;
            chordLength = circumference / numPoints;
            targetArea = radius * radius * Mathf.PI * puffiness;

            // Update max velocity for all points
            foreach (SlimePoint point in SlimePoints)
            {
                point.UpdateParameters(maxVelocity);
            }

            // Recreate points if needed (e.g., if numPoints changed)
            if (recreatePoints)
            {
                Initialize();
            }
        }

        public void ResetPosition()
        {
            Vector2 currentCenter = GetCenter();
            Vector2 offset = (Vector2)transform.position - currentCenter;

            foreach (SlimePoint point in SlimePoints)
            {
                point.Position += offset;
                point.PreviousPosition += offset;
            }

            // Re-initialize the slime shape
            for (int i = 0; i < SlimePoints.Count; i++)
            {
                float angle = 2 * Mathf.PI * i / SlimePoints.Count - Mathf.PI / 2;
                Vector2 newPos = (Vector2)transform.position + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                SlimePoints[i].Position = newPos;
                SlimePoints[i].PreviousPosition = newPos;
            }
        }

    }

}
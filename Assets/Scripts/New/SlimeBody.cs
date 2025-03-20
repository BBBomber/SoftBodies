using System.Collections.Generic;
using Unity.VisualScripting;
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
        public Color fillColor = new Color(0.3f, 0.8f, 1f, 0.7f);
        public float lineWidth = 0.1f;
        private LineRenderer lineRenderer;

        public List<SlimePoint> SlimePoints { get; private set; } = new List<SlimePoint>();

        private float circumference;
        private float chordLength;
        private Dictionary<Collider2D, Vector2> lastColliderPositions = new Dictionary<Collider2D, Vector2>();

        [SerializeField] private List<Collider2D> solidObjects = new List<Collider2D>();
        [SerializeField] private List<SlimeBody> otherSlimes = new List<SlimeBody>();

        private Bounds slimeBounds = new Bounds();

        protected override void Awake()
        {
            // Find solid objects in the scene
            solidObjects.AddRange(FindObjectsByType<Collider2D>(FindObjectsSortMode.None));
            otherSlimes.AddRange(FindObjectsByType<SlimeBody>(FindObjectsSortMode.None));
            // Initialize the slime
            base.Awake();
        }



        private void LateUpdate()
        {
            if (lineRenderer != null)
            {
                // Update line renderer positions to follow the slime points
                for (int i = 0; i < SlimePoints.Count; i++)
                {
                    // Important: Set the correct world position
                    lineRenderer.SetPosition(i, SlimePoints[i].Position);
                }
                // Close the loop by connecting back to the first point
                lineRenderer.SetPosition(SlimePoints.Count, SlimePoints[0].Position);

                // Optional debug
                if (Time.frameCount % 60 == 0) // Only log once every 60 frames
                {
                    Debug.Log("Updated SlimeBody outline, first point: " + lineRenderer.GetPosition(0));
                }
            }
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

            if (lineRenderer == null) // Protect against multiple initialization
            {
                // Create a new child GameObject for the outline
                GameObject outlineObj = new GameObject("SlimeOutline");
                outlineObj.transform.SetParent(transform);
                outlineObj.transform.localPosition = Vector3.zero;

                // Add the LineRenderer to the child object
                lineRenderer = outlineObj.AddComponent<LineRenderer>();
                lineRenderer.positionCount = numPoints + 1; // +1 to close the loop
                lineRenderer.startWidth = 0.2f;
                lineRenderer.endWidth = 0.2f;
                lineRenderer.loop = true;
                lineRenderer.useWorldSpace = true; // Important to keep this true

                // Set the material and color
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = fillColor;
                lineRenderer.endColor = fillColor;

                // Set Z position to be slightly in front
                lineRenderer.transform.position = new Vector3(transform.position.x, transform.position.y, -0.1f);

                Debug.Log("SlimeBody LineRenderer created with " + lineRenderer.positionCount + " points");
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
                    point.ApplyDisplacement(); //applies stored displacement values
                    if (enableSpringDragging)
                        point.HandleMouseInteraction(mousePosition, 1f, isRightMousePressed, isRightMouseReleased);
                    point.KeepInBounds(screenBounds);
                }

                if (enableCollisions) HandleCollisions(solidObjects);
                if (enableSoftBodyCollisions) HandleSlimeCollisions(otherSlimes);
            }

            // Update transform position based on center
            transform.position = GetCenter();
            UpdateBounds();
        }

        private void ApplySpringForces() //stores the displacement amount for each point
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

       /* private void HandleSlimeCollisions(List<SlimeBody> otherSlimes)
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
        }*/

        // Replace your current HandleSlimeCollisions with this improved version
        private void HandleSlimeCollisions(List<SlimeBody> otherSlimes)
        {
            // Update our bounds first
            UpdateBounds();

            foreach (SlimeBody otherSlime in otherSlimes)
            {
                if (otherSlime == this) continue; // Don't collide with itself

                // Update other slime's bounds
                otherSlime.UpdateBounds();

                // Quick bounds check first (optimization)
                if (!slimeBounds.Intersects(otherSlime.slimeBounds))
                    continue;

                // Check each point of this slime against the other slime
                foreach (SlimePoint point in SlimePoints)
                {
                    if (IsPointInsideSlime(point.Position, otherSlime))
                    {
                        // Find the closest edge and resolve collision
                        ResolvePointSlimeCollision(point, otherSlime);
                    }
                }

                // Check points of the other slime against this slime (for symmetry)
                foreach (SlimePoint point in otherSlime.SlimePoints)
                {
                    if (IsPointInsideSlime(point.Position, this))
                    {
                        // Find the closest edge and resolve collision
                        ResolvePointSlimeCollision(point, this);
                    }
                }
            }
        }

        // Determines if a point is inside another slime using the even-odd rule
        private bool IsPointInsideSlime(Vector2 testPoint, SlimeBody slime)
        {
            // First do a quick bounds check
            if (!slime.slimeBounds.Contains(testPoint))
                return false;

            // Use the ray casting algorithm (even-odd rule)
            bool inside = false;

            // Get a point guaranteed to be outside the slime
            Vector2 outsidePoint = new Vector2(slime.slimeBounds.max.x + 1.0f, testPoint.y);

            int pointCount = slime.SlimePoints.Count;
            for (int i = 0, j = pointCount - 1; i < pointCount; j = i++)
            {
                Vector2 vertI = slime.SlimePoints[i].Position;
                Vector2 vertJ = slime.SlimePoints[j].Position;

                // Check if the edge intersects with the horizontal ray
                if (((vertI.y > testPoint.y) != (vertJ.y > testPoint.y)) &&
                    (testPoint.x < (vertJ.x - vertI.x) * (testPoint.y - vertI.y) / (vertJ.y - vertI.y) + vertI.x))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        // Resolves collision by finding the closest edge and pushing the point out
        private void ResolvePointSlimeCollision(SlimePoint point, SlimeBody slime)
        {
            // Find the closest edge of the slime
            float minDistance = float.MaxValue;
            int closestEdgeIndex = 0;
            Vector2 closestEdgePoint = Vector2.zero;

            int pointCount = slime.SlimePoints.Count;
            for (int i = 0; i < pointCount; i++)
            {
                int j = (i + 1) % pointCount;

                Vector2 edgeStarte = slime.SlimePoints[i].Position;
                Vector2 edgeEnde = slime.SlimePoints[j].Position;

                Vector2 edgePoint = ClosestPointOnLineSegment(point.Position, edgeStarte, edgeEnde);
                float distance = Vector2.Distance(point.Position, edgePoint);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestEdgeIndex = i;
                    closestEdgePoint = edgePoint;
                }
            }

            // Calculate how close we are to each vertex of the edge
            int nextIndex = (closestEdgeIndex + 1) % pointCount;
            Vector2 edgeStart = slime.SlimePoints[closestEdgeIndex].Position;
            Vector2 edgeEnd = slime.SlimePoints[nextIndex].Position;

            float totalEdgeLength = Vector2.Distance(edgeStart, edgeEnd);
            float ratioToStart = Vector2.Distance(closestEdgePoint, edgeStart) / totalEdgeLength;
            float ratioToEnd = 1.0f - ratioToStart;

            // Calculate the normal direction (away from the slime)
            Vector2 edgeDirection = (edgeEnd - edgeStart).normalized;
            Vector2 edgeNormal = new Vector2(-edgeDirection.y, edgeDirection.x);

            // Make sure the normal points outward from the slime
            Vector2 slimeCenter = slime.GetCenter();
            if (Vector2.Dot(edgeNormal, closestEdgePoint - slimeCenter) < 0)
            {
                edgeNormal = -edgeNormal;
            }

            // Calculate penetration depth
            float penetration = minDistance + 0.05f; // Add a small buffer

            // Calculate effective mass
            float pointMass = point.Mass;
            float edgeStartMass = slime.SlimePoints[closestEdgeIndex].Mass;
            float edgeEndMass = slime.SlimePoints[nextIndex].Mass;
            float totalMass = pointMass + (edgeStartMass * ratioToStart) + (edgeEndMass * ratioToEnd);

            // Calculate displacement proportions based on mass
            float pointProportion = pointMass / totalMass;
            float edgeStartProportion = (edgeStartMass * ratioToStart) / totalMass;
            float edgeEndProportion = (edgeEndMass * ratioToEnd) / totalMass;

            // Apply displacements
            point.ApplyDisplacement(edgeNormal * penetration * (1.0f - pointProportion));
            slime.SlimePoints[closestEdgeIndex].ApplyDisplacement(-edgeNormal * penetration * edgeStartProportion);
            slime.SlimePoints[nextIndex].ApplyDisplacement(-edgeNormal * penetration * edgeEndProportion);

            // Handle velocity reflection
            Vector2 pointVelocity = point.Position - point.PreviousPosition;

            // Calculate the velocity of the edge point
            Vector2 edgeVelocity =
                (slime.SlimePoints[closestEdgeIndex].Position - slime.SlimePoints[closestEdgeIndex].PreviousPosition) * ratioToStart +
                (slime.SlimePoints[nextIndex].Position - slime.SlimePoints[nextIndex].PreviousPosition) * ratioToEnd;

            // Calculate relative velocity
            Vector2 relativeVelocity = pointVelocity - edgeVelocity;

            // Calculate reflection
            float normalDot = Vector2.Dot(relativeVelocity, edgeNormal);
            if (normalDot < 0) // Only reflect if moving toward the edge
            {
                Vector2 reflectionVector = -2 * normalDot * edgeNormal;
                Vector2 reflectedVelocity = relativeVelocity + reflectionVector;

                // Apply bounciness
                reflectedVelocity *= slime.bounceFactor;

                // Update point's previous position to affect velocity
                point.PreviousPosition = point.Position - reflectedVelocity;

                // Apply reaction force to edge points
                Vector2 reactionForce = -reflectedVelocity * pointMass / 2.0f; // Divide by 2 to dampen reaction

                // Distribute the reaction force to edge points
                Vector2 edgeStartForce = reactionForce * ratioToStart;
                Vector2 edgeEndForce = reactionForce * ratioToEnd;

                slime.SlimePoints[closestEdgeIndex].PreviousPosition += edgeStartForce / edgeStartMass;
                slime.SlimePoints[nextIndex].PreviousPosition += edgeEndForce / edgeEndMass;
            }
        }

        // Helper method to find closest point on a line segment
        private Vector2 ClosestPointOnLineSegment(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector2 lineDirection = lineEnd - lineStart;
            float lineLength = lineDirection.magnitude;
            lineDirection.Normalize();

            float projectLength = Mathf.Clamp(Vector2.Dot(point - lineStart, lineDirection), 0, lineLength);
            return lineStart + (lineDirection * projectLength);
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

        // Call this method at the end of UpdatePhysics
        private void UpdateBounds()
        {
            // Calculate the bounding box for this slime
            Vector2 min = SlimePoints[0].Position;
            Vector2 max = SlimePoints[0].Position;

            foreach (var point in SlimePoints)
            {
                min.x = Mathf.Min(min.x, point.Position.x);
                min.y = Mathf.Min(min.y, point.Position.y);
                max.x = Mathf.Max(max.x, point.Position.x);
                max.y = Mathf.Max(max.y, point.Position.y);
            }

            // Add a small margin
            min -= Vector2.one * 0.1f;
            max += Vector2.one * 0.1f;

            slimeBounds.SetMinMax(min, max);
        }

    }

}
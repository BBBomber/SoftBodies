using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Simple camera controller to follow a target
    /// </summary>
    public class NewCameraController : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.3f;
        public Vector2 offset = new Vector2(0, 2);
        public bool boundCamera = true;
        public Vector2 minBounds = new Vector2(-10, -5);
        public Vector2 maxBounds = new Vector2(10, 10);

        private Vector3 velocity = Vector3.zero;

        private void LateUpdate()
        {
            if (target == null) return;

            // Target position with offset
            Vector3 targetPosition = new Vector3(
                target.position.x + offset.x,
                target.position.y + offset.y,
                transform.position.z
            );

            // Smoothly move the camera towards the target
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime
            );

            // Apply bounds if needed
            if (boundCamera)
            {
                transform.position = new Vector3(
                    Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x),
                    Mathf.Clamp(transform.position.y, minBounds.y, maxBounds.y),
                    transform.position.z
                );
            }
        }

        /// <summary>
        /// Gets the screen bounds in world space
        /// </summary>
        public Bounds GetScreenBounds()
        {
            Camera cam = GetComponent<Camera>();
            if (cam == null) return new Bounds();

            float height = 2f * cam.orthographicSize;
            float width = height * cam.aspect;

            return new Bounds(
                transform.position,
                new Vector3(width, height, 0)
            );
        }

        private void OnDrawGizmos()
        {
            // Draw camera bounds
            if (boundCamera)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Vector2 size = maxBounds - minBounds;
                Vector2 center = (maxBounds + minBounds) / 2;
                Gizmos.DrawCube(new Vector3(center.x, center.y, 0), new Vector3(size.x, size.y, 0.1f));
            }

            // Draw screen bounds
            Bounds screenBounds = GetScreenBounds();
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawCube(screenBounds.center, screenBounds.size);
        }
    }
}
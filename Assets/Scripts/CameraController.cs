using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("General Settings")]
    public bool useFollowCamera = false; // Toggle between zoom and follow camera

    [Header("Zoom Camera Settings")]
    public float zoomSpeed = 0.5f;
    public float minZoom = 1f;
    public float maxZoom = 5f;

    [Header("Follow Camera Settings")]
    public Transform target;         // Target to follow
    public float followZoom = 5f;    // Zoom level in follow mode
    public bool lockX = false;       // Lock X-axis movement
    public bool lockY = false;       // Lock Y-axis movement
    public float followSmoothness = 0.1f; // Camera follow speed
    public float yFollowOffset = 5f;

    private Camera mainCamera;
    public float startingOrthoSize;
    private Vector2 originalScreenSize;
    private Vector3 velocity = Vector3.zero; // For smooth damp

    // Variables for touch-based zoom
    private float initialDistanceBetweenTouches;
    private float initialOrthoSize;

    void Start()
    {
        mainCamera = GetComponent<Camera>() ?? Camera.main;
        startingOrthoSize = mainCamera.orthographicSize;
        originalScreenSize = new Vector2(Screen.width, Screen.height);
        mainCamera.orthographicSize = startingOrthoSize * maxZoom;
    }

    void LateUpdate()
    {
        if (useFollowCamera)
        {
            FollowTarget();
        }
        else
        {
            HandleZoom();
        }
    }

    private void HandleZoom()
    {
        // Handle touch-based zoom for mobile devices
        if (Input.touchCount == 2) // Two-finger pinch gesture
        {
            /*Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch2.phase == TouchPhase.Began)
            {
                // Store the initial distance between touches and the current orthographic size
                initialDistanceBetweenTouches = Vector2.Distance(touch1.position, touch2.position);
                initialOrthoSize = mainCamera.orthographicSize;
            }
            else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
            {
                // Calculate the current distance between touches
                float currentDistanceBetweenTouches = Vector2.Distance(touch1.position, touch2.position);

                // Calculate the zoom factor based on the change in distance
                float zoomFactor = initialDistanceBetweenTouches / currentDistanceBetweenTouches;

                // Apply the zoom factor to the camera's orthographic size
                float newOrthoSize = initialOrthoSize * zoomFactor;

                // Clamp the zoom level within the specified range
                newOrthoSize = Mathf.Clamp(newOrthoSize, startingOrthoSize * minZoom, startingOrthoSize * maxZoom);

                // Smoothly adjust the camera's orthographic size
                mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, newOrthoSize, zoomSpeed * Time.deltaTime);
            }*/
        }
        else
        {
            // Fallback to mouse scroll for testing in the editor
            float scrollDelta = Input.mouseScrollDelta.y;

            if (scrollDelta != 0)
            {
                float zoomChange = -scrollDelta * zoomSpeed;
                float newOrthoSize = mainCamera.orthographicSize + zoomChange;
                newOrthoSize = Mathf.Clamp(newOrthoSize, startingOrthoSize * minZoom, startingOrthoSize * maxZoom);
                mainCamera.orthographicSize = newOrthoSize;
            }
        }
    }

    private void FollowTarget()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position;

        if (lockX) targetPosition.x = transform.position.x;
        if (lockY) targetPosition.y = transform.position.y;

        transform.position = Vector3.SmoothDamp(transform.position, new Vector3(targetPosition.x, targetPosition.y + yFollowOffset, transform.position.z), ref velocity, followSmoothness);
        mainCamera.orthographicSize = followZoom;
    }

    public float GetZoomScale()
    {
        return mainCamera.orthographicSize / startingOrthoSize;
    }

    public Bounds GetScreenBounds()
    {
        Vector2 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector2(0, 0));
        Vector2 topRight = mainCamera.ViewportToWorldPoint(new Vector2(1, 1));

        Vector3 center = (bottomLeft + topRight) / 2;
        Vector3 size = topRight - bottomLeft;

        return new Bounds(center, size);
    }
}
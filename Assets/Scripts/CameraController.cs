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

    private Camera mainCamera;
    private float startingOrthoSize;
    private Vector2 originalScreenSize;
    private Vector3 velocity = Vector3.zero; // For smooth damp

    void Start()
    {
        mainCamera = GetComponent<Camera>() ?? Camera.main;
        startingOrthoSize = mainCamera.orthographicSize;
        originalScreenSize = new Vector2(Screen.width, Screen.height);
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
        float scrollDelta = Input.mouseScrollDelta.y;

        if (scrollDelta != 0)
        {
            float zoomChange = -scrollDelta * zoomSpeed;
            float newOrthoSize = mainCamera.orthographicSize + zoomChange;
            newOrthoSize = Mathf.Clamp(newOrthoSize, startingOrthoSize, startingOrthoSize * maxZoom);
            mainCamera.orthographicSize = newOrthoSize;
        }
    }

    private void FollowTarget()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position;

        if (lockX) targetPosition.x = transform.position.x;
        if (lockY) targetPosition.y = transform.position.y;

        transform.position = Vector3.SmoothDamp(transform.position, new Vector3(targetPosition.x, targetPosition.y, transform.position.z), ref velocity, followSmoothness);
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

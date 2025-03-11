using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float zoomSpeed = 0.5f;
    public float minZoom = 1f;  // Minimum zoom level (most zoomed in, default screen size)
    public float maxZoom = 5f;  // Maximum zoom level (most zoomed out)

    private Camera mainCamera;
    private float startingOrthoSize;
    private Vector2 originalScreenSize;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Store the initial orthographic size as our base reference
        startingOrthoSize = mainCamera.orthographicSize;

        // Store the original screen dimensions
        originalScreenSize = new Vector2(Screen.width, Screen.height);
    }

    void Update()
    {
        // Detect mouse scroll input
        float scrollDelta = Input.mouseScrollDelta.y;

        if (scrollDelta != 0)
        {
            // Apply zoom change based on scroll direction
            float zoomChange = -scrollDelta * zoomSpeed;
            float newOrthoSize = mainCamera.orthographicSize + zoomChange;

            // Clamp the zoom level between min and max
            newOrthoSize = Mathf.Clamp(newOrthoSize, startingOrthoSize, startingOrthoSize * maxZoom);

            // Apply the new orthographic size
            mainCamera.orthographicSize = newOrthoSize;
        }
    }

    // Get the current zoom scale factor relative to the original view
    public float GetZoomScale()
    {
        return mainCamera.orthographicSize / startingOrthoSize;
    }

    // Get the current screen boundaries in world coordinates
    public Bounds GetScreenBounds()
    {
        Vector2 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector2(0, 0));
        Vector2 topRight = mainCamera.ViewportToWorldPoint(new Vector2(1, 1));

        Vector3 center = (bottomLeft + topRight) / 2;
        Vector3 size = topRight - bottomLeft;

        return new Bounds(center, size);
    }
}
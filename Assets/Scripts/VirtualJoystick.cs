using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public Image joystickBackground; // Joystick background reference
    public Image joystickHandle; // Joystick handle reference
    private Vector2 inputVector = Vector2.zero; // Stores input direction
    private RectTransform backgroundRectTransform;
    private RectTransform handleRectTransform;
    private Canvas canvas;
    private Camera canvasCamera;
    private int touchId = -1; // Tracks the specific touch controlling the joystick
    private float backgroundRadius; // Radius of the joystick background
    public bool isMovementJoystick = false;

    private void Start()
    {
        // Cache RectTransform components
        backgroundRectTransform = joystickBackground.GetComponent<RectTransform>();
        handleRectTransform = joystickHandle.GetComponent<RectTransform>();

        // Get reference to the canvas and its render mode
        canvas = GetComponentInParent<Canvas>();
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
            canvasCamera = canvas.worldCamera;

        // Calculate background radius (half of width)
        backgroundRadius = backgroundRectTransform.rect.width * 0.5f;

        if(isMovementJoystick) 
        {
            // Ensure the joystick is positioned at the bottom left
            PositionJoystickAtBottomLeft();
        }
       

        // Make sure the anchor is centered
        backgroundRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Make sure handle anchor is centered
        handleRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        handleRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        handleRectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Ensure this GameObject and all child components can receive touch/raycasts
        EnsureRaycastTarget();
    }

    private void EnsureRaycastTarget()
    {
        // Make sure all images can receive raycasts
        Image bgImage = joystickBackground.GetComponent<Image>();
        if (bgImage != null) bgImage.raycastTarget = true;

        Image handleImage = joystickHandle.GetComponent<Image>();
        if (handleImage != null) handleImage.raycastTarget = true;

        // Also check if there's an image on this gameObject
        Image thisImage = GetComponent<Image>();
        if (thisImage != null) thisImage.raycastTarget = true;
    }

    private void PositionJoystickAtBottomLeft()
    {
        // Get the canvas rect
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // Calculate the size of the joystick background
        float backgroundWidth = backgroundRectTransform.rect.width;
        float backgroundHeight = backgroundRectTransform.rect.height;

        // Calculate the position that will place the joystick at bottom left
        // with padding equal to half the radius to ensure no part is outside the screen
        float padding = backgroundRadius * 0.5f;

        // Calculate the position in canvas space
        float xPos = -canvasRect.rect.width / 2 + backgroundWidth / 2 + padding;
        float yPos = -canvasRect.rect.height / 2 + backgroundHeight / 2 + padding;

        // Set the position
        backgroundRectTransform.anchoredPosition = new Vector2(xPos, yPos);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (touchId == -1) // Only register a new touch if no active touch is assigned
        {
            touchId = eventData.pointerId; // Assign touch ID
            OnDrag(eventData); // Start dragging immediately
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // For touch input on mobile, make sure we're tracking the correct pointer
        if (eventData.pointerId != touchId && touchId != -1)
            return; // Ignore other touches if we're already tracking one

        // For cases where touch might not have gone through OnPointerDown first
        if (touchId == -1)
            touchId = eventData.pointerId;

        // Convert screen position to local position relative to the background
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            backgroundRectTransform,
            eventData.position,
            eventData.pressEventCamera, // Use the eventData camera directly
            out localPoint))
        {
            // Calculate normalized direction and magnitude
            Vector2 direction = localPoint;
            float magnitude = direction.magnitude;

            // Clamp the handle position within the background's radius
            if (magnitude > backgroundRadius)
                direction = direction.normalized * backgroundRadius;

            // Set handle position
            handleRectTransform.anchoredPosition = direction;

            // Calculate input vector (normalized -1 to 1)
            inputVector = direction / backgroundRadius;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == touchId || touchId == -1) // Reset if this is our tracked touch or if we somehow missed tracking
        {
            ResetJoystick();
        }
    }

    private void Update()
    {
        // Extra safety for mobile: If all touches have ended but we're still tracking one, reset
        if (touchId != -1 && Input.touchCount == 0 && !Input.GetMouseButton(0))
        {
            ResetJoystick();
        }
    }

    private void ResetJoystick()
    {
        inputVector = Vector2.zero;
        handleRectTransform.anchoredPosition = Vector2.zero;
        touchId = -1; // Reset tracked touch ID
    }

    public float Horizontal() { return inputVector.x; }
    public float Vertical() { return inputVector.y; }
    public Vector2 GetInputVector() { return inputVector; }
}
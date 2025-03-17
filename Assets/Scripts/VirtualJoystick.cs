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

        // Ensure the joystick is positioned at the bottom left
        PositionJoystickAtBottomLeft();

        // Make sure the anchor is centered
        backgroundRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Make sure handle anchor is centered
        handleRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        handleRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        handleRectTransform.pivot = new Vector2(0.5f, 0.5f);
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
        if (eventData.pointerId != touchId) return; // Ignore other touches

        // Convert screen position to world and then to local position of background
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            backgroundRectTransform,
            eventData.position,
            canvasCamera,
            out localPoint);

        // Calculate normalized direction and magnitude
        Vector2 direction = localPoint;
        float magnitude = Mathf.Min(direction.magnitude, backgroundRadius);

        // Clamp the handle position within the background's radius
        Vector2 clampedDirection = direction.normalized * magnitude;

        // Set handle position
        handleRectTransform.anchoredPosition = clampedDirection;

        // Calculate input vector (normalized -1 to 1)
        inputVector = clampedDirection / backgroundRadius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == touchId) // Only reset if the releasing touch is the tracked one
        {
            ResetJoystick();
        }
    }

    private void Update()
    {
        if (touchId != -1) // Only check if there's an active touch
        {
            if (Input.touchCount > 0) // For mobile/touch input
            {
                bool touchFound = false;
                foreach (Touch touch in Input.touches)
                {
                    if (touch.fingerId == touchId)
                    {
                        touchFound = true;
                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            ResetJoystick();
                        }
                        break;
                    }
                }

                // If tracked touch is no longer found, reset
                if (!touchFound)
                {
                    ResetJoystick();
                }
            }
            else if (!Input.GetMouseButton(0)) // For mouse input (only one click at a time)
            {
                ResetJoystick();
            }
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

    // Make the joystick handle also detect events
    private void OnEnable()
    {
        // Add event triggers to the handle if not already present
        AddEventTriggerToHandle();
    }

    private void AddEventTriggerToHandle()
    {
        // Get or add EventTrigger component to the handle
        GameObject handleObj = joystickHandle.gameObject;
        EventTrigger trigger = handleObj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = handleObj.AddComponent<EventTrigger>();

        // Clear existing entries to avoid duplicates
        if (trigger.triggers != null)
            trigger.triggers.Clear();

        // Add pointer down event
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => {
            OnPointerDown((PointerEventData)data);
        });
        trigger.triggers.Add(pointerDown);

        // Add drag event
        EventTrigger.Entry drag = new EventTrigger.Entry();
        drag.eventID = EventTriggerType.Drag;
        drag.callback.AddListener((data) => {
            OnDrag((PointerEventData)data);
        });
        trigger.triggers.Add(drag);

        // Add pointer up event
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => {
            OnPointerUp((PointerEventData)data);
        });
        trigger.triggers.Add(pointerUp);
    }
}
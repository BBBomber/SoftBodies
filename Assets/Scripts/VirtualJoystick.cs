using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public Image joystickBackground; // Reference to the joystick background image
    public Image joystickHandle; // Reference to the joystick handle image
    private Vector2 inputVector; // Stores the input direction

    private RectTransform backgroundRectTransform;
    private RectTransform handleRectTransform;

    private void Start()
    {
        // Cache the RectTransform components for better performance
        backgroundRectTransform = joystickBackground.GetComponent<RectTransform>();
        handleRectTransform = joystickHandle.GetComponent<RectTransform>();
    }

    // Called when the joystick is dragged
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        // Convert screen point to local position within the joystick background
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(backgroundRectTransform, eventData.position, eventData.pressEventCamera, out pos))
        {
            // Normalize the position relative to the joystick background size
            pos.x = (pos.x / backgroundRectTransform.sizeDelta.x);
            pos.y = (pos.y / backgroundRectTransform.sizeDelta.y);

            // Calculate the input vector
            inputVector = new Vector2(pos.x * 2, pos.y * 2);

            // Normalize the input vector to ensure circular movement
            if (inputVector.magnitude > 1.0f)
            {
                inputVector = inputVector.normalized;
            }

            // Move the joystick handle within the background bounds
            handleRectTransform.anchoredPosition = new Vector2(
                inputVector.x * (backgroundRectTransform.sizeDelta.x / 2),
                inputVector.y * (backgroundRectTransform.sizeDelta.y / 2)
            );
        }
    }

    // Called when the joystick is pressed
    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData); // Start dragging immediately
    }

    // Called when the joystick is released
    public void OnPointerUp(PointerEventData eventData)
    {
        // Reset the input vector and snap the handle back to the center
        inputVector = Vector2.zero;
        handleRectTransform.anchoredPosition = Vector2.zero;
    }

    // Returns the horizontal input value (-1 to 1)
    public float Horizontal()
    {
        return inputVector.x;
    }

    // Returns the vertical input value (-1 to 1)
    public float Vertical()
    {
        return inputVector.y;
    }

    // Returns the raw input vector
    public Vector2 GetInputVector()
    {
        return inputVector;
    }
}
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public SlimeCharacterController slimeController;

    // Called when the button is pressed (mouse click or touch)
    public void OnPointerDown(PointerEventData eventData)
    {
        slimeController.OnJumpButtonPressed();
    }

    // Called when the button is released (mouse click or touch)
    public void OnPointerUp(PointerEventData eventData)
    {
        slimeController.OnJumpButtonReleased();
    }
}
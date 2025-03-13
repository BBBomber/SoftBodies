using UnityEngine;

public class SideToSideMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public Transform leftPoint;  // Set in Inspector
    public Transform rightPoint; // Set in Inspector
    public float cycleTime = 2f; // Time to complete one left-right cycle
    public bool easeInOut = true; // Enable ease-in-ease-out movement
    public bool isMoving = true;  // Can be toggled to pause/resume movement

    private float elapsedTime = 0f;
    private Vector3 startPos;
    private Vector3 endPos;

    void Start()
    {
        if (leftPoint == null || rightPoint == null)
        {
            Debug.LogError("Left and Right Points must be assigned.");
            return;
        }
        startPos = leftPoint.position;
        endPos = rightPoint.position;
    }

    void Update()
    {
        if (!isMoving) return; // Stop moving when paused

        elapsedTime += Time.deltaTime;
        float t = (elapsedTime % cycleTime) / cycleTime;
        float movementFactor = Mathf.PingPong(t * 2f, 1f); // Creates a yoyo effect

        if (easeInOut)
        {
            movementFactor = Mathf.SmoothStep(0f, 1f, movementFactor); // Apply ease in/ease out
        }

        transform.position = Vector3.Lerp(startPos, endPos, movementFactor);
    }

    // Function to Pause movement
    public void PauseMovement()
    {
        isMoving = false;
    }

    // Function to Resume movement
    public void ResumeMovement()
    {
        isMoving = true;
    }

    // Toggle movement state
    public void ToggleMovement()
    {
        isMoving = !isMoving;
    }
}

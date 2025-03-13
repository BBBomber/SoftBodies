using UnityEngine;
using System.Collections.Generic;

public class SlimeCharacterController : MonoBehaviour
{
    // Reference to the blob being controlled
    private Blob controlledBlob;

    [Header("Jump Settings")]
    [Tooltip("Base jump force")]
    [Range(1f, 60f)]
    public float baseJumpForce = 40f;

    [Tooltip("Maximum jump charge multiplier")]
    [Range(1f, 5f)]
    public float maxChargeMultiplier = 3f;

    [Tooltip("How fast the jump charges")]
    [Range(0.5f, 5f)]
    public float chargeSpeed = 2f;

    [Tooltip("How much the slime squishes when charging")]
    [Range(0.1f, 1f)]
    public float squishFactor = 0.5f;

    [Tooltip("How much the slime stretches horizontally when squishing")]
    [Range(0f, 2f)]
    public float stretchFactor = 1.2f;

    [Tooltip("Cooldown between jumps")]
    [Range(0f, 2f)]
    public float jumpCooldown = 0.5f;

    [Header("Ground Detection")]
    [Tooltip("Layers considered as ground")]
    public LayerMask groundLayer;

    [Tooltip("Distance to check for ground")]
    [Range(0.05f, 1f)]
    public float groundCheckDistance = 0.2f;

    [Tooltip("Consider screen boundaries as ground")]
    public bool useScreenBoundariesAsGround = true;

    [Tooltip("Distance threshold from screen edge to consider as ground")]
    [Range(0.01f, 0.5f)]
    public float screenEdgeThreshold = 0.1f;

    [Header("Movement Settings")]
    [Tooltip("Horizontal movement speed")]
    [Range(1f, 10f)]
    public float moveSpeed = 5f;

    [Tooltip("Air control factor (0-1)")]
    [Range(0f, 1f)]
    public float airControlFactor = 0.5f;

    // Private variables
    private float currentChargeTime = 0f;
    private bool isCharging = false;
    private bool isGrounded = false;
    private float jumpCooldownTimer = 0f;
    private Vector2 jumpDirection = Vector2.up;
    private Vector2 originalScale;
    private Vector2 originalRadius;
    private float originalPuffy;
    private Bounds screenBounds;
    private CameraController cameraController;

    // Visual feedback
    private Color normalColor;
    private Color chargeColor = new Color(1f, 0.7f, 0.3f);
    private LineRenderer lineRenderer;


    [Header("Stuck Mechanics")]
    [Range(0.5f, 5f)]
    public float stuckLaunchForce = 2.5f;
    [Range(0.5f, 3f)]
    public float maxStuckCompressionFactor = 0.3f;
    public Color stuckChargeColor = new Color(1f, 0.3f, 0.3f); // Bright red

    private bool isStuck = false;
    private float stuckChargeTime = 0f;

    [Tooltip("Radius to check for nearby colliders")]
    [Range(0.5f, 10f)]
    public float colliderCheckRadius = 2f; // Adjust this based on your blob's size


    void Start()
    {
        
        
    }

    public void Initialize(BlobTest blob)
    {
        controlledBlob = blob.blob;
        Debug.Log("setting blob ref");
        if (blob != null)
        {
            

            // Store original properties
            originalRadius = new Vector2(controlledBlob.Radius, controlledBlob.Radius);
            originalPuffy = SoftBodySimulator.Instance.blobParams.puffy;
            normalColor = SoftBodySimulator.Instance.blobColor;
            colliderCheckRadius = controlledBlob.Radius;
            // Get camera controller for screen bounds
            cameraController = Camera.main.GetComponent<CameraController>();
            if (cameraController == null)
            {
                Debug.LogWarning("No CameraController found. Screen boundaries won't be used as ground.");
                useScreenBoundariesAsGround = false;
            }

            // Get or add line renderer for jump direction indicator
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.1f;
                lineRenderer.endWidth = 0.1f;
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = Color.white;
                lineRenderer.endColor = Color.white;
                lineRenderer.positionCount = 2;
            }
            lineRenderer.enabled = false;
        }
        else
        {
            Debug.LogError("SlimeCharacterController: No BlobTest found in the scene!");
        }
    }

    void Update()
    {
        if (controlledBlob == null) return;

        // Update screen bounds
        if (cameraController != null)
        {
            screenBounds = cameraController.GetScreenBounds();
        }

        // Update timers
        if (jumpCooldownTimer > 0)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }

        // Check if grounded
        CheckGrounded();

        // Handle horizontal movement
        HandleMovement();

        // Handle jump charging
        HandleJumpCharge();

        HandleStuckState();

        // Update visual feedback
        UpdateVisuals();
    }

    private void CheckGrounded()
    {
        // Check if any bottom points are touching ground
        int groundedPoints = 0;
        Vector2 center = controlledBlob.GetCenter();

        foreach (BlobPoint point in controlledBlob.Points)
        {
            bool pointIsGrounded = false;

            // Check for ground layer collisions
            RaycastHit2D hit = Physics2D.Raycast(
                point.Position,
                Vector2.down,
                groundCheckDistance,
                groundLayer
            );

            if (hit.collider != null)
            {
                pointIsGrounded = true;
            }

            // Check for screen boundaries if enabled
            if (useScreenBoundariesAsGround && !pointIsGrounded && screenBounds.size.magnitude > 0)
            {
                // Check bottom edge
                if (Mathf.Abs(point.Position.y - screenBounds.min.y) < screenEdgeThreshold)
                {
                    pointIsGrounded = true;
                    // Change jump direction to up for bottom edge
                    if (jumpDirection.y < 0 && !isCharging)
                    {
                        jumpDirection = Vector2.up;
                    }
                }

                // Check left edge
                if (Mathf.Abs(point.Position.x - screenBounds.min.x) < screenEdgeThreshold)
                {
                    pointIsGrounded = true;
                    // Change jump direction to right for left edge
                    if (jumpDirection.x < 0 && !isCharging)
                    {
                        jumpDirection = Vector2.right;
                    }
                }

                // Check right edge
                if (Mathf.Abs(point.Position.x - screenBounds.max.x) < screenEdgeThreshold)
                {
                    pointIsGrounded = true;
                    // Change jump direction to left for right edge
                    if (jumpDirection.x > 0 && !isCharging)
                    {
                        jumpDirection = Vector2.left;
                    }
                }

                // Check top edge (optional, typically not needed)
                if (Mathf.Abs(point.Position.y - screenBounds.max.y) < screenEdgeThreshold)
                {
                    pointIsGrounded = true;
                    // Change jump direction to down for top edge
                    if (jumpDirection.y > 0 && !isCharging)
                    {
                        jumpDirection = Vector2.down;
                    }
                }
            }

            if (pointIsGrounded)
            {
                groundedPoints++;
            }
        }

        // Consider grounded if at least 4 points are touching
        isGrounded = groundedPoints >= 4;
    }

    private void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");

        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            if(!isCharging)
            {
                // Apply horizontal force to all points
                float moveFactor = isGrounded ? 1f : airControlFactor;
                Vector2 moveForce = new Vector2(horizontalInput * moveSpeed * moveFactor * Time.deltaTime, 0);

                foreach (BlobPoint point in controlledBlob.Points)
                {
                    point.Position += moveForce;
                }
            }
            
        }
    }

    private void HandleJumpCharge()
    {
        // Get jump direction from arrow keys
        Vector2 dirInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (dirInput.magnitude > 0.1f)
        {
            jumpDirection = dirInput.normalized;
        }
        else if (!isCharging)
        {
           
        }

        // Jump charge (Space bar)
        if (Input.GetKey(KeyCode.LeftShift) && isGrounded && jumpCooldownTimer <= 0)
        {
            isCharging = true;
            currentChargeTime += Time.deltaTime * chargeSpeed;

            // Squish the slime vertically and stretch horizontally
            float squishAmount = Mathf.Lerp(1f, squishFactor, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));
            float stretchAmount = Mathf.Lerp(1f, stretchFactor, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));

            // Apply squish by modifying blob parameters
            ModifyBlobShape(squishAmount, stretchAmount);

            // Change color while charging
            SoftBodySimulator.Instance.blobColor = Color.Lerp(normalColor, chargeColor, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));
        }

        // Jump release
        if (Input.GetKeyUp(KeyCode.LeftShift) && isCharging)
        {
            // Calculate jump force
            float jumpMultiplier = Mathf.Clamp(currentChargeTime, 1f, maxChargeMultiplier);
            Vector2 jumpForce = jumpDirection * baseJumpForce * jumpMultiplier;

            // Apply jump force to all points
            foreach (BlobPoint point in controlledBlob.Points)
            {
                point.Position += jumpForce * Time.deltaTime;
                point.PreviousPosition = point.Position - jumpForce * Time.deltaTime; // Apply as velocity
            }

            // Reset charging state
            isCharging = false;
            currentChargeTime = 0f;
            jumpCooldownTimer = jumpCooldown;

            // Reset blob shape
            ResetBlobShape();

            // Reset color
            SoftBodySimulator.Instance.blobColor = normalColor;
        }
    }

    private void HandleStuckState()
    {
        // Check if stuck (contact with small colliders)
        isStuck = CheckIfStuck();

        if (isStuck)
        {
            // Get jump direction from arrow keys
            Vector2 dirInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            if (dirInput.magnitude > 0.1f)
            {
                jumpDirection = dirInput.normalized;
            }
            else if (!isCharging)
            {

            }

            // Jump charge (Space bar)
            if (Input.GetKey(KeyCode.LeftShift) && isStuck && jumpCooldownTimer <= 0)
            {
                isCharging = true;
                currentChargeTime += Time.deltaTime * chargeSpeed;

                // Squish the slime vertically and stretch horizontally
                float squishAmount = Mathf.Lerp(1f, squishFactor, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));
                float stretchAmount = Mathf.Lerp(1f, stretchFactor, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));

                // Apply squish by modifying blob parameters
                ModifyBlobShape(squishAmount, stretchAmount);

                // Change color while charging
                SoftBodySimulator.Instance.blobColor = Color.Lerp(normalColor, chargeColor, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));
            }

            // Jump release
            if (Input.GetKeyUp(KeyCode.LeftShift) && isCharging)
            {
                // Calculate jump force
                float jumpMultiplier = Mathf.Clamp(currentChargeTime, 1f, maxChargeMultiplier);
                Vector2 jumpForce = jumpDirection * baseJumpForce * jumpMultiplier;

                // Apply jump force to all points
                foreach (BlobPoint point in controlledBlob.Points)
                {
                    point.Position += jumpForce * Time.deltaTime;
                    point.PreviousPosition = point.Position - jumpForce * Time.deltaTime; // Apply as velocity
                }

                // Reset charging state
                isCharging = false;
                currentChargeTime = 0f;
                jumpCooldownTimer = jumpCooldown;

                // Reset blob shape
                ResetBlobShape();

                // Reset color
                SoftBodySimulator.Instance.blobColor = normalColor;
            }
        }
    }

    private bool CheckIfStuck()
    {
        if (controlledBlob == null) return false;
        if (isGrounded)
        {
            return false;
        }
        // Get the blob's center and radius
        Vector2 blobCenter = controlledBlob.GetCenter();
        float blobRadius = controlledBlob.Radius;

        // Get all colliders within the check radius
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(blobCenter, blobRadius, groundLayer);

        // Check if any collider is inside the blob
        foreach (Collider2D collider in nearbyColliders)
        {
            if (IsColliderInsideBlob(collider))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsColliderInsideBlob(Collider2D collider)
    {
        if (controlledBlob == null || collider == null) return false;

        // Get the blob's center and radius
        Vector2 blobCenter = controlledBlob.GetCenter();
        float blobRadius = controlledBlob.Radius;

        // Get the closest point on the collider to the blob's center
        Vector2 closestPointOnCollider = collider.ClosestPoint(blobCenter);

        // Check if the closest point is inside the blob's radius
        float distanceToCollider = Vector2.Distance(blobCenter, closestPointOnCollider);
        return distanceToCollider < blobRadius;
    }

    private void ModifyBlobShape(float verticalScale, float horizontalScale)
    {
        // Temporarily modify the blob's physical properties
        SoftBodySimulator softBodySim = SoftBodySimulator.Instance;

        // Squish by modifying point positions
        Vector2 center = controlledBlob.GetCenter();

        for (int i = 0; i < controlledBlob.Points.Count; i++)
        {
            BlobPoint point = controlledBlob.Points[i];
            Vector2 dirFromCenter = point.Position - center;

            // Apply vertical squish and horizontal stretch
            dirFromCenter.y *= verticalScale;
            dirFromCenter.x *= horizontalScale;

            point.Position = center + dirFromCenter;
        }

        // Also modify the blob's internal pressure by changing the puffiness
        softBodySim.blobParams.puffy = originalPuffy * verticalScale;
    }

    private void ResetBlobShape()
    {
        // Reset to original pressure
        SoftBodySimulator.Instance.blobParams.puffy = originalPuffy;
    }

    private void UpdateVisuals()
    {
        // Show jump direction indicator when charging
        if (isCharging)
        {
            lineRenderer.enabled = true;
            Vector2 center = controlledBlob.GetCenter();
            float indicatorLength = controlledBlob.Radius * (1 + Mathf.Clamp01(currentChargeTime / maxChargeMultiplier) * 2);

            lineRenderer.SetPosition(0, center);
            lineRenderer.SetPosition(1, center + jumpDirection * indicatorLength);

            // Change color based on charge amount
            lineRenderer.startColor = Color.Lerp(Color.white, Color.red, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));
            lineRenderer.endColor = Color.Lerp(Color.white, Color.yellow, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    void OnGUI()
    {
        // Debug information
        GUILayout.BeginArea(new Rect(10, 10, 200, 150));
        GUILayout.Label("Grounded: " + isGrounded);
        if (isCharging)
        {
            GUILayout.Label("Charge: " + (currentChargeTime / maxChargeMultiplier * 100).ToString("F0") + "%");
        }
        GUILayout.Label("Jump Direction: " + jumpDirection.ToString());
        GUILayout.Label("Stuck: " + isStuck);
        GUILayout.EndArea();
    }
}
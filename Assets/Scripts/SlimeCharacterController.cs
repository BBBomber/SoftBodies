using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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
    public float groundCheckDistance = 0.3f;

    [Tooltip("Consider screen boundaries as ground")]
    public bool useScreenBoundariesAsGround = true;

    [Tooltip("Distance threshold from screen edge to consider as ground")]
    [Range(0.01f, 0.5f)]
    public float screenEdgeThreshold = 0.1f;

    [Header("Movement Settings")]
    [Tooltip("Horizontal movement speed")]
    [Range(0f, 10f)]
    public float moveSpeed = 0.5f;

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

    // Input variables moved from FixedUpdate to Update
    private float horizontalInput;
    private Vector2 dirInput;
    private bool jumpKeyPressed;
    private bool jumpKeyReleased;
    private bool shootTentaclePressed;
    private bool breakTentaclePressed;
    private Vector2 mouseWorldPos;

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

    [Header("Tentacle Grapple")]
    public float tentacleMaxLength = 5f;
    public float tentaclePullForce = 10f;
    private bool isTentacleActive = false;
    private Vector2 tentacleTarget;
    private LineRenderer tentacleRenderer;
    private GameObject tentacleVisuals;

    [Header("Tentacle Swing Settings")]
    [Tooltip("Maximum duration the tentacle stays active")]
    public float maxTentacleDuration = 3f;

    [Tooltip("Minimum distance from the grapple point before the tentacle breaks")]
    public float minTentacleDistance = 0.5f;

    [Tooltip("Force applied to keep the slime within the tentacle's length")]
    public float swingConstraintForce = 10f;


    [Header("Rope Physics Settings")]
    [Tooltip("Maximum stretch allowed for the rope (as a multiplier of the initial length)")]
    [Range(1f, 2f)]
    public float maxStretchFactor = 1.2f; // 20% stretch

    [Tooltip("Strength of the spring force pulling the slime back to the rope's length")]
    [Range(1f, 100f)]
    public float springForce = 50f;

    [Tooltip("Damping factor to reduce oscillations in the rope")]
    [Range(0f, 1f)]
    public float dampingFactor = 0.1f;

    private float fixedTentacleLength;


    private Collider2D grappledObject; // Store the grappled object's Rigidbody2D
    private Vector2 localGrapplePoint; // Store the local position of the grapple point relative to the grappled object

    // Add these variables to your class
    [Header("Mobile Input")]
    public VirtualJoystick moveJoystick;
    public VirtualJoystick aimJoystick; // For tentacle aiming
    public bool useMobileControls = false;

    // Add this variable to track if the jump was released this frame
    private bool jumpReleasedThisFrame = false;



    void Start()
    {
        // Create a child object for the tentacle visuals
        tentacleVisuals = new GameObject("TentacleVisuals");
        tentacleVisuals.transform.SetParent(transform); // Make it a child of the slime
        tentacleVisuals.transform.localPosition = Vector3.zero; // Reset position

        // Add LineRenderer to the child object
        tentacleRenderer = tentacleVisuals.AddComponent<LineRenderer>();
        tentacleRenderer.startWidth = 0.1f;
        tentacleRenderer.endWidth = 0.05f;
        tentacleRenderer.material = new Material(Shader.Find("Sprites/Default"));
        tentacleRenderer.positionCount = 2;
        tentacleRenderer.enabled = false; // Disable by default

      

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
        // Gather all inputs in Update
        GatherInputs();
        // Update screen bounds
        if (cameraController != null)
        {
            screenBounds = cameraController.GetScreenBounds();
        }

        // Check if grounded
        CheckGrounded();
        // Handle tentacle grapple
        HandleTentacleGrappleInput();

        // Update timers
        if (jumpCooldownTimer > 0)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }

        // Update jump direction based on input direction
        UpdateJumpDirection();

        // Handle jump input
        HandleJumpInput();

        // Handle stuck state input
        HandleStuckStateInput();

        // Apply jump and jump charging effects
        ApplyJumpEffects();

        // Apply stuck state effects
        ApplyStuckStateEffects();
    }

    private void GatherInputs()
    {
        if (useMobileControls && moveJoystick != null)
        {
            // Mobile controls
            horizontalInput = moveJoystick.Horizontal();

            // Get directional input from joystick
            dirInput = new Vector2(moveJoystick.Horizontal(), moveJoystick.Vertical());

            // jumpKeyPressed and jumpKeyReleased are set by the JumpButton methods

            // Use a second joystick for tentacle aiming if available
            if (aimJoystick != null)
            {
                Vector2 aimInput = new Vector2(aimJoystick.Horizontal(), aimJoystick.Vertical());
                if (aimInput.magnitude > 0.1f)
                {
                    // Convert joystick direction to world position relative to the blob
                    Vector2 aimDirection = aimInput.normalized;
                    mouseWorldPos = (Vector2)controlledBlob.GetCenter() + aimDirection * tentacleMaxLength;

                    // Detect if the tentacle should be shot or released
                    if (aimInput.magnitude > 0.8f && !isTentacleActive)
                    {
                        shootTentaclePressed = true;
                    }
                    else if (aimInput.magnitude < 0.3f && isTentacleActive)
                    {
                        breakTentaclePressed = true;
                    }
                    else
                    {
                        shootTentaclePressed = false;
                        breakTentaclePressed = false;
                    }
                }
                else
                {
                    shootTentaclePressed = false;
                    breakTentaclePressed = true;
                }
            }
        }
        else
        {
            // Desktop controls (original)
            horizontalInput = Input.GetAxis("Horizontal");
            dirInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            jumpKeyPressed = Input.GetKey(KeyCode.LeftShift);
            jumpKeyReleased = Input.GetKeyUp(KeyCode.LeftShift);
            shootTentaclePressed = Input.GetMouseButtonDown(0);
            breakTentaclePressed = Input.GetMouseButtonUp(0);
            mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    private void UpdateJumpDirection()
    {
        if (dirInput.magnitude > 0.1f)
        {
            jumpDirection = dirInput.normalized;
        }
    }

    private void FixedUpdate()
    {
        if (controlledBlob == null) return;

        

        // Handle horizontal movement using the input we gathered in Update
        HandleMovement();

        

        // Apply rope physics if tentacle is active
        if (isTentacleActive)
        {
            ApplyRopePhysics();
        }

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
        isGrounded = groundedPoints >= 2;
    }

    private void HandleMovement()
    {
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            if (!isCharging)
            {
                // Apply horizontal force to all points
                float moveFactor = isGrounded ? 1f : airControlFactor;
                Vector2 moveForce = new Vector2(horizontalInput * moveSpeed * moveFactor * Time.fixedDeltaTime, 0);

                foreach (BlobPoint point in controlledBlob.Points)
                {
                    point.Position += moveForce;
                }
            }
        }
    }

    private void HandleJumpInput()
    {
        // Handle jump charging input
        if (jumpKeyPressed && isGrounded && jumpCooldownTimer <= 0)
        {
            isCharging = true;
            currentChargeTime += Time.deltaTime * chargeSpeed;
        }

        // Handle jump release input - don't reset jumpKeyReleased here
        // Let the ApplyJumpEffects method handle the reset after applying the force
        if (jumpKeyReleased && isCharging)
        {
            // Don't reset isCharging here
            jumpCooldownTimer = jumpCooldown;
        }
    }

    private void ApplyJumpEffects()
    {
        if (isCharging)
        {
            // Squish the slime vertically and stretch horizontally
            float squishAmount = Mathf.Lerp(1f, squishFactor, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));
            float stretchAmount = Mathf.Lerp(1f, stretchFactor, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));

            // Apply squish by modifying blob parameters
            ModifyBlobShape(squishAmount, stretchAmount);

            // Change color while charging
            SoftBodySimulator.Instance.blobColor = Color.Lerp(normalColor, chargeColor, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));
        }

        // Apply jump force if we just released the jump key
        if (jumpKeyReleased && currentChargeTime > 0)
        {
            Debug.Log("Jump released! Applying force with multiplier: " + Mathf.Clamp(currentChargeTime, 1f, maxChargeMultiplier));

            // Calculate jump force
            float jumpMultiplier = Mathf.Clamp(currentChargeTime, 1f, maxChargeMultiplier);
            Vector2 jumpForce = jumpDirection * baseJumpForce * jumpMultiplier;

            // Apply jump force to all points
            foreach (BlobPoint point in controlledBlob.Points)
            {
                point.Position += jumpForce * Time.fixedDeltaTime;
                point.PreviousPosition = point.Position - jumpForce * Time.fixedDeltaTime; // Apply as velocity
            }

            // Reset charging state
            currentChargeTime = 0f;
            isCharging = false;

            // Reset blob shape
            ResetBlobShape();

            // Reset color
            SoftBodySimulator.Instance.blobColor = normalColor;

            // Reset the jump key released flag after applying the jump
            jumpKeyReleased = false;
        }
    }

    private void HandleStuckStateInput()
    {
        // Check if stuck (contact with small colliders)
        isStuck = CheckIfStuck();

        if (isStuck)
        {
            // Handle jump charging input when stuck
            if (jumpKeyPressed && jumpCooldownTimer <= 0)
            {
                isCharging = true;
                currentChargeTime += Time.deltaTime * chargeSpeed;
            }

            // Handle jump release input when stuck
            if (jumpKeyReleased && isCharging)
            {
                isCharging = false;
                jumpCooldownTimer = jumpCooldown;
            }
        }
    }

    private void ApplyStuckStateEffects()
    {
        if (isStuck && isCharging)
        {
            // Squish the slime vertically and stretch horizontally
            float squishAmount = Mathf.Lerp(1f, squishFactor, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));
            float stretchAmount = Mathf.Lerp(1f, stretchFactor, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));

            // Apply squish by modifying blob parameters
            ModifyBlobShape(squishAmount, stretchAmount);

            // Change color while charging
            SoftBodySimulator.Instance.blobColor = Color.Lerp(normalColor, stuckChargeColor, Mathf.Clamp01(currentChargeTime / maxChargeMultiplier));
        }

        // Apply jump force if we just released the jump key while stuck
        // Apply jump force if we just released the jump key while stuck
        if (isStuck && jumpKeyReleased && currentChargeTime > 0)
        {
            Debug.Log("Stuck jump released! Applying force with multiplier: " + Mathf.Clamp(currentChargeTime, 1f, maxChargeMultiplier));

            // Calculate jump force
            float jumpMultiplier = Mathf.Clamp(currentChargeTime, 1f, maxChargeMultiplier);
            Vector2 jumpForce = jumpDirection * baseJumpForce * jumpMultiplier;

            // Apply jump force to all points
            foreach (BlobPoint point in controlledBlob.Points)
            {
                point.Position += jumpForce * Time.fixedDeltaTime;
                point.PreviousPosition = point.Position - jumpForce * Time.fixedDeltaTime; // Apply as velocity
            }

            // Reset charging state
            currentChargeTime = 0f;
            isCharging = false;

            // Reset blob shape
            ResetBlobShape();

            // Reset color
            SoftBodySimulator.Instance.blobColor = normalColor;

            // Reset the jump key released flag after applying the jump
            jumpKeyReleased = false;
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
        float blobRadius = controlledBlob.Radius + 5;

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

        // Check if the collider's bounds overlap with the blob's radius
        Bounds colliderBounds = collider.bounds;
        Vector2 closestPoint = colliderBounds.ClosestPoint(blobCenter);
        return Vector2.Distance(blobCenter, closestPoint) < blobRadius;
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

    private void HandleTentacleGrappleInput()
    {
        // Handle shooting tentacle
        if (shootTentaclePressed)
        {
            // Reset for next frame
            shootTentaclePressed = false;

            // Cast a ray to see if there's something to grapple onto
            RaycastHit2D hit = Physics2D.Raycast(
                controlledBlob.GetCenter(),
                mouseWorldPos - controlledBlob.GetCenter(),
                tentacleMaxLength,
                groundLayer
            );

            if (hit.collider != null)
            {
                // Found something to grapple!
                isTentacleActive = true;
                tentacleTarget = hit.point;

                // Store the grappled object's Collider2D
                grappledObject = hit.collider;

                // Calculate the local grapple point relative to the grappled object
                if (grappledObject != null)
                {
                    localGrapplePoint = grappledObject.transform.InverseTransformPoint(tentacleTarget);
                }
                else
                {
                    localGrapplePoint = tentacleTarget; // Fallback for static colliders
                }

                // Store the initial length of the tentacle
                fixedTentacleLength = Vector2.Distance(controlledBlob.GetCenter(), tentacleTarget);
                moveSpeed = 1.5f;

                // Enable the tentacle visuals
                if (tentacleRenderer != null)
                {
                    tentacleRenderer.enabled = true;
                }
            }
        }

        // Handle breaking tentacle
        if (breakTentaclePressed)
        {
            // Reset for next frame
            breakTentaclePressed = false;

            moveSpeed = 0.5f;
            isTentacleActive = false;
            grappledObject = null; // Clear the grappled object reference

            // Disable the tentacle visuals
            if (tentacleRenderer != null)
            {
                tentacleRenderer.enabled = false;
            }
        }

        // Update tentacle visuals and target position if active
        if (isTentacleActive)
        {
            // Update the tentacle target position if the grappled object is moving
            if (grappledObject != null)
            {
                tentacleTarget = grappledObject.transform.TransformPoint(localGrapplePoint);
            }

            // Update the tentacle visuals
            if (tentacleRenderer != null)
            {
                tentacleRenderer.SetPosition(0, controlledBlob.GetCenter());
                tentacleRenderer.SetPosition(1, tentacleTarget);
            }
        }
    }

    private void ApplyRopePhysics()
    {
        // Get the slime's center and the direction to the grapple point
        Vector2 slimeCenter = controlledBlob.GetCenter();
        Vector2 directionToGrapple = (tentacleTarget - slimeCenter).normalized;
        float currentDistance = Vector2.Distance(slimeCenter, tentacleTarget);

        // Calculate the stretch ratio
        float stretchRatio = currentDistance / fixedTentacleLength;

        // If the rope is stretched beyond its limit, apply a spring force
        if (stretchRatio > 1f)
        {
            // Calculate the spring force (Hooke's Law: F = k * x)
            float stretchAmount = currentDistance - fixedTentacleLength;
            Vector2 springForceVector = directionToGrapple * stretchAmount * springForce;

            // Apply damping to reduce oscillations
            Vector2 slimeVelocity = (slimeCenter - controlledBlob.GetPreviousCenter()) / Time.fixedDeltaTime;
            Vector2 dampingForce = -slimeVelocity * dampingFactor;

            // Apply the net force to all blob points
            Vector2 netForce = springForceVector + dampingForce;
            foreach (BlobPoint point in controlledBlob.Points)
            {
                point.Position += springForceVector * Time.fixedDeltaTime;
            }
        }
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

    // First, add these methods to handle jump button events from your JumpButton script
    public void OnJumpButtonPressed()
    {
        // Simulate jump key press
        jumpKeyPressed = true;
        jumpKeyReleased = false;
    }

    public void OnJumpButtonReleased()
    {
        jumpKeyPressed = false;
        jumpKeyReleased = true;

        // We don't need to reset this via coroutine, let the jump logic handle it
        // This ensures the jump happens before the flag is cleared
    }


    private IEnumerator ResetJumpKeyReleased()
    {
        yield return null; // Wait one frame
        jumpKeyReleased = false;
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
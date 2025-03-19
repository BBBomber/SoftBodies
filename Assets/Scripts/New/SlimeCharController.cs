using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Controller for the slime character - based on your SlimeCharacterController
    /// </summary>
    public class SlimeCharController : MonoBehaviour
    {
        public SlimeBody slimeBody;

        [Header("Jump Settings")]
        [Range(1f, 60f)]
        public float baseJumpForce = 40f;
        [Range(1f, 5f)]
        public float maxChargeMultiplier = 3f;
        [Range(0.5f, 5f)]
        public float chargeSpeed = 2f;
        [Range(0.1f, 1f)]
        public float squishFactor = 0.5f;
        [Range(0f, 2f)]
        public float stretchFactor = 1.2f;
        [Range(0f, 2f)]
        public float jumpCooldown = 0.5f;

        [Header("Ground Detection")]
        public LayerMask groundLayer;
        [Range(0.05f, 1f)]
        public float groundCheckDistance = 0.3f;
        public bool useScreenBoundariesAsGround = true;
        [Range(0.01f, 0.5f)]
        public float screenEdgeThreshold = 0.1f;

        [Header("Movement Settings")]
        [Range(0f, 10f)]
        public float moveSpeed = 0.5f;
        [Range(0f, 1f)]
        public float airControlFactor = 0.5f;

        // Private variables
        private float currentChargeTime = 0f;
        private bool isCharging = false;
        private bool isGrounded = false;
        private float jumpCooldownTimer = 0f;
        private Vector2 jumpDirection = Vector2.up;
        private Vector2 originalScale;
        private Bounds screenBounds;

        // Input variables
        private float horizontalInput;
        private Vector2 dirInput;
        private bool jumpKeyPressed;
        private bool jumpKeyReleased;
        private Vector2 mouseWorldPos;

        // Visual feedback
        private Color normalColor;
        private Color chargeColor = new Color(1f, 0.7f, 0.3f);
        private LineRenderer lineRenderer;

        private void Start()
        {
            if (slimeBody == null)
            {
                Debug.LogError("SlimeCharacterController: No SlimeBody assigned!");
                return;
            }

            // Store original properties
            originalScale = new Vector2(slimeBody.radius, slimeBody.radius);
            normalColor = Color.white; // Default color

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

        private void Update()
        {
            if (slimeBody == null) return;

            // Gather all inputs
            GatherInputs();

            // Update screen bounds
            UpdateScreenBounds();

            // Check if grounded
            CheckGrounded();

            // Update timers
            if (jumpCooldownTimer > 0)
            {
                jumpCooldownTimer -= Time.deltaTime;
            }

            // Update jump direction based on input direction
            UpdateJumpDirection();

            // Handle jump input
            HandleJumpInput();

            // Apply jump charging effects
            ApplyJumpEffects();

            // Update visual feedback
            UpdateVisuals();
        }

        private void FixedUpdate()
        {
            if (slimeBody == null) return;

            // Handle horizontal movement using the input we gathered in Update
            HandleMovement();
        }

        private void GatherInputs()
        {
            // Get inputs
            horizontalInput = Input.GetAxis("Horizontal");
            dirInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            jumpKeyPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.Space);
            jumpKeyReleased = Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.Space);
            mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        private void UpdateScreenBounds()
        {
            // Update screen bounds
            screenBounds = new Bounds(
                Camera.main.transform.position,
                new Vector3(
                    Camera.main.orthographicSize * Camera.main.aspect * 2,
                    Camera.main.orthographicSize * 2,
                    0
                )
            );
        }

        private void CheckGrounded()
        {
            // Check if any bottom points are touching ground
            int groundedPoints = 0;
            Vector2 center = slimeBody.GetCenter();

            foreach (IPointMass point in slimeBody.GetPoints())
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

            // Consider grounded if at least 2 points are touching
            isGrounded = groundedPoints >= 2;
        }

        private void UpdateJumpDirection()
        {
            if (dirInput.magnitude > 0.1f)
            {
                jumpDirection = dirInput.normalized;
            }
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

                    foreach (IPointMass point in slimeBody.GetPoints())
                    {
                        if (!point.IsFixed)
                        {
                            point.Position += moveForce;
                        }
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
                currentChargeTime = Mathf.Min(currentChargeTime, maxChargeMultiplier);
            }

            // Handle jump release input
            if (jumpKeyReleased && isCharging)
            {
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

                // Apply squish by modifying blob point positions
                ModifySlimeShape(squishAmount, stretchAmount);
            }

            // Apply jump force if we just released the jump key
            if (jumpKeyReleased && currentChargeTime > 0)
            {
                // Calculate jump force
                float jumpMultiplier = Mathf.Clamp(currentChargeTime, 1f, maxChargeMultiplier);
                Vector2 jumpForce = jumpDirection * baseJumpForce * jumpMultiplier;

                // Apply jump force to all points
                foreach (IPointMass point in slimeBody.GetPoints())
                {
                    if (!point.IsFixed)
                    {
                        point.Position += jumpForce * Time.fixedDeltaTime;
                        point.PreviousPosition = point.Position - jumpForce * Time.fixedDeltaTime; // Apply as velocity
                    }
                }

                // Reset charging state
                currentChargeTime = 0f;
                isCharging = false;

                // Reset slime shape
                ResetSlimeShape();
            }
        }

        private void ModifySlimeShape(float verticalScale, float horizontalScale)
        {
            // Squish by modifying point positions
            Vector2 center = slimeBody.GetCenter();

            foreach (IPointMass point in slimeBody.GetPoints())
            {
                if (!point.IsFixed)
                {
                    Vector2 dirFromCenter = point.Position - center;

                    // Apply vertical squish and horizontal stretch
                    dirFromCenter.y *= verticalScale;
                    dirFromCenter.x *= horizontalScale;

                    point.Position = center + dirFromCenter;
                }
            }
        }

        private void ResetSlimeShape()
        {
            // This would be handled automatically by the pressure force
            // in the next frame, so we don't need to do anything special here
        }

        private void UpdateVisuals()
        {
            // Show jump direction indicator when charging
            if (isCharging)
            {
                lineRenderer.enabled = true;
                Vector2 center = slimeBody.GetCenter();
                float indicatorLength = slimeBody.radius * (1 + Mathf.Clamp01(currentChargeTime / maxChargeMultiplier) * 2);

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

        private void OnGUI()
        {
            // Debug information
            GUILayout.BeginArea(new Rect(10, 10, 200, 150));
            GUILayout.Label("Grounded: " + isGrounded);
            if (isCharging)
            {
                GUILayout.Label("Charge: " + (currentChargeTime / maxChargeMultiplier * 100).ToString("F0") + "%");
            }
            GUILayout.Label("Jump Direction: " + jumpDirection.ToString());
            GUILayout.EndArea();
        }
    }
}
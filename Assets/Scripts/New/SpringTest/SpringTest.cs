using SoftBodyPhysics;
using UnityEngine;

public class SpringTest : MonoBehaviour
{
    public Transform pointATransform;
    public Transform pointBTransform;

    private PointMass massA;
    private PointMass massB;
    private SpringConstraint spring;

    [Range(0f, 1f)]
    public float springStiffness = 0.8f;
    [Range(0f, 1f)]
    public float springDamping = 0.1f;

    public float restLength = 1.0f;
    public bool fixPointA = false;
    public bool fixPointB = false;
    public bool applyGravity = true;
    public float gravityStrength = 9.8f;

    // Add this to allow manual dragging in editor
    [Tooltip("Enable to drag objects in edit mode")]
    public bool allowEditorDragging = true;

    private LineRenderer lineRenderer;
    private Vector3 lastPointAPosition;
    private Vector3 lastPointBPosition;
    private bool simulationStarted = false;

    void Start()
    {
        simulationStarted = true;

        // Make sure we have transform references
        if (pointATransform == null || pointBTransform == null)
        {
            Debug.LogError("Please assign pointATransform and pointBTransform in the inspector!");
            enabled = false;
            return;
        }

        // Initialize point masses
        massA = new PointMass(pointATransform.position, 1f);
        massB = new PointMass(pointBTransform.position, 1f);

        // Set fixed property based on inspector settings
        massA.IsFixed = fixPointA;
        massB.IsFixed = fixPointB;

        // Create the spring with current distance as rest length
        if (restLength <= 0)
            restLength = Vector2.Distance(massA.Position, massB.Position);

        spring = new SpringConstraint(massA, massB, restLength, springStiffness, springDamping);

        // Set up line renderer for visualization
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Store initial positions
        lastPointAPosition = pointATransform.position;
        lastPointBPosition = pointBTransform.position;
    }

    void Update()
    {
        // If simulation hasn't started, just draw the line between transforms
        if (!simulationStarted)
        {
            if (lineRenderer != null && pointATransform != null && pointBTransform != null)
            {
                lineRenderer.SetPosition(0, pointATransform.position);
                lineRenderer.SetPosition(1, pointBTransform.position);
            }
            return;
        }

        // Make sure all references exist before proceeding
        if (massA == null || massB == null || pointATransform == null || pointBTransform == null || lineRenderer == null)
            return;

        // Check if transforms were moved in the editor
        bool pointAMoved = Vector3.Distance(pointATransform.position, lastPointAPosition) > 0.001f;
        bool pointBMoved = Vector3.Distance(pointBTransform.position, lastPointBPosition) > 0.001f;

        // Handle manual movement during runtime
        if (pointAMoved)
        {
            // If it's fixed OR if we're manually dragging, update point position
            if (fixPointA || allowEditorDragging)
            {
                massA.Position = pointATransform.position;
                massA.PreviousPosition = pointATransform.position; // No velocity when manually moved
            }
            lastPointAPosition = pointATransform.position;
        }

        if (pointBMoved)
        {
            if (fixPointB || allowEditorDragging)
            {
                massB.Position = pointBTransform.position;
                massB.PreviousPosition = pointBTransform.position;
            }
            lastPointBPosition = pointBTransform.position;
        }

        // Only update transforms from physics if they weren't manually moved
        if (!pointAMoved && !fixPointA)
            pointATransform.position = massA.Position;

        if (!pointBMoved && !fixPointB)
            pointBTransform.position = massB.Position;

        // Update the line renderer
        lineRenderer.SetPosition(0, massA.Position);
        lineRenderer.SetPosition(1, massB.Position);
    }

    void FixedUpdate()
    {
        if (!simulationStarted) return;

        float deltaTime = Time.fixedDeltaTime / 5.0f; // Smaller timestep for stability

        // Run 5 substeps with both constraint solving AND integration
        for (int i = 0; i < 5; i++)
        {
            // Apply gravity
            if (applyGravity)
            {
                Vector2 gravityForce = Vector2.down * gravityStrength;
                if (!massA.IsFixed)
                    massA.ApplyForce(gravityForce * massA.Mass);
                if (!massB.IsFixed)
                    massB.ApplyForce(gravityForce * massB.Mass);
            }

            // Solve spring constraint
            spring.Solve();

            // Integrate positions right after solving
            if (!massA.IsFixed)
                massA.VerletIntegrate(deltaTime, 0.99f);

            if (!massB.IsFixed)
                massB.VerletIntegrate(deltaTime, 0.99f);
        }

        // Log spring state
        if (Time.frameCount % 60 == 0)
        {
            float currentDistance = Vector2.Distance(massA.Position, massB.Position);
            Debug.Log($"Spring: Current distance={currentDistance:F3}, Rest distance={restLength:F3}, Error={currentDistance - restLength:F3}");
        }
    }

    // Allow runtime changes to spring parameters
    public void UpdateSpringParameters()
    {
        if (massA != null && massB != null)
        {
            spring = new SpringConstraint(massA, massB, restLength, springStiffness, springDamping);

            // Update fixed status in case it changed in the inspector
            massA.IsFixed = fixPointA;
            massB.IsFixed = fixPointB;
        }
    }

    // This allows you to see changes in the inspector immediately affect the spring
    void OnValidate()
    {
        if (Application.isPlaying && simulationStarted)
        {
            UpdateSpringParameters();
        }
    }

    // Initialize the line renderer even in edit mode
    void OnEnable()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.positionCount = 2;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    // Draw in editor mode too
    void OnDrawGizmos()
    {
        if (pointATransform != null && pointBTransform != null)
        {
            // Draw points
            Gizmos.color = fixPointA ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(pointATransform.position, 0.1f);

            Gizmos.color = fixPointB ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(pointBTransform.position, 0.1f);

            // Draw the spring line
            Gizmos.color = Color.white;
            Gizmos.DrawLine(pointATransform.position, pointBTransform.position);

            // Draw rest length visualization
            if (restLength > 0)
            {
                Gizmos.color = Color.green;
                Vector3 center = (pointATransform.position + pointBTransform.position) / 2;
                Gizmos.DrawWireSphere(center, restLength / 2);
            }
        }
    }
}
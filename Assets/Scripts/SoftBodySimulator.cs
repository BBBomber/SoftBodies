using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlobParameters
{
    [Tooltip("Damping factor for blob movement (0-1)")]
    [Range(0.8f, 0.999f)]
    public float dampening = 0.99f;

    [Tooltip("Gravity force applied to blob")]
    [Range(-5f, 5f)]
    public float gravity = 1f;

    [Tooltip("Initial radius of the blob")]
    [Range(0.5f, 5f)]
    public float radius = 1f;

    [Tooltip("Puffiness factor for the blob")]
    [Range(1f, 3f)]
    public float puffy = 1.5f;

    [Tooltip("Number of points in the blob")]
    [Range(8, 64)]
    public int points = 16;

    [Tooltip("Maximum allowed displacement per physics step")]
    [Range(-10f, 1f)]
    public float maxDisplacement = 0.1f;

    [Tooltip("Collision radius for mouse interaction")]
    [Range(0.1f, 2f)]
    public float mouseInteractionRadius = 1f;

    [Tooltip("Maximum velocity magnitude")]
    [Range(1f, 10f)]
    public float maxVelocity = 3f;
}

[System.Serializable]
public class BlobFeatures
{
    [Tooltip("Enable spring forces between points")]
    public bool enableSprings = true;

    [Tooltip("Enable collisions with solid objects")]
    public bool enableCollisions = true;

    [Tooltip("Enable blob morphing")]
    public bool enableMorphing = false;

    [Tooltip("Enable pressure-based expansion")]
    public bool enablePressure = true;

    [Tooltip("Enable spring dragging with mouse")]
    public bool enableSpringDragging = true;

    [Tooltip("Enable collisions between soft bodies")]
    public bool enableSoftBodyCollisions = false;

    [Tooltip("Enable jelly-like effect")]
    public bool enableJellyEffect = false;

    [Tooltip("Enable reset on Space key")]
    public bool enableResetOnSpace = true;

    [Tooltip("Jelly constraint strength")]
    [Range(0.01f, 0.5f)]
    public float jellyStrength = 0.1f;

    [Tooltip("Collision bounce factor")]
    [Range(0.1f, 1f)]
    public float bounceFactor = 0.8f;
}

public class SoftBodySimulator : MonoBehaviour
{
    public static SoftBodySimulator Instance { get; private set; }

    [Header("Blob Parameters")]
    public BlobParameters blobParams = new BlobParameters();

    [Header("Blob Features")]
    public BlobFeatures features = new BlobFeatures();

    [Header("Rendering")]
    [Tooltip("Number of points between control points for smooth rendering")]
    [Range(1, 20)]
    public int splineResolution = 10;

    [Tooltip("Line renderer width")]
    [Range(0.01f, 0.5f)]
    public float lineWidth = 0.1f;

    [Tooltip("Blob color")]
    public Color blobColor = Color.green;

    public enum SimulationType
    {
        BlobOnly
    }

    public SimulationType simulationType = SimulationType.BlobOnly;

    // References to test objects
    private GameObject blobTestObject;
    private BlobTest activeBlobTest;

    // Cache for parameter comparison
    private BlobParameters lastParams;
    private BlobFeatures lastFeatures;

    private void Awake()
    {
        // Ensure only one instance of SoftBodySimulator exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize cache
            lastParams = CloneParameters(blobParams);
            lastFeatures = CloneFeatures(features);
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    private BlobParameters CloneParameters(BlobParameters source)
    {
        BlobParameters clone = new BlobParameters();
        clone.dampening = source.dampening;
        clone.gravity = source.gravity;
        clone.radius = source.radius;
        clone.puffy = source.puffy;
        clone.points = source.points;
        clone.maxDisplacement = source.maxDisplacement;
        clone.mouseInteractionRadius = source.mouseInteractionRadius;
        clone.maxVelocity = source.maxVelocity;
        return clone;
    }

    private BlobFeatures CloneFeatures(BlobFeatures source)
    {
        BlobFeatures clone = new BlobFeatures();
        clone.enableSprings = source.enableSprings;
        clone.enableCollisions = source.enableCollisions;
        clone.enableMorphing = source.enableMorphing;
        clone.enablePressure = source.enablePressure;
        clone.enableSpringDragging = source.enableSpringDragging;
        clone.enableSoftBodyCollisions = source.enableSoftBodyCollisions;
        clone.enableJellyEffect = source.enableJellyEffect;
        clone.enableResetOnSpace = source.enableResetOnSpace;
        clone.jellyStrength = source.jellyStrength;
        clone.bounceFactor = source.bounceFactor;
        return clone;
    }

    void Start()
    {
        TestBlob();
    }

    void Update()
    {
        // Check if parameters have changed
        bool paramsChanged = !AreParametersEqual(blobParams, lastParams);
        bool featuresChanged = !AreFeaturesEqual(features, lastFeatures);

        // Update any active blob test if parameters change
        if (activeBlobTest != null && (paramsChanged || featuresChanged))
        {
            activeBlobTest.UpdateParameters(paramsChanged && blobParams.points != lastParams.points);

            // Update cache
            lastParams = CloneParameters(blobParams);
            lastFeatures = CloneFeatures(features);
        }
    }

    private bool AreParametersEqual(BlobParameters a, BlobParameters b)
    {
        return a.dampening == b.dampening &&
               a.gravity == b.gravity &&
               a.radius == b.radius &&
               a.puffy == b.puffy &&
               a.points == b.points &&
               a.maxDisplacement == b.maxDisplacement &&
               a.mouseInteractionRadius == b.mouseInteractionRadius &&
               a.maxVelocity == b.maxVelocity;
    }

    private bool AreFeaturesEqual(BlobFeatures a, BlobFeatures b)
    {
        return a.enableSprings == b.enableSprings &&
               a.enableCollisions == b.enableCollisions &&
               a.enableMorphing == b.enableMorphing &&
               a.enablePressure == b.enablePressure &&
               a.enableSpringDragging == b.enableSpringDragging &&
               a.enableSoftBodyCollisions == b.enableSoftBodyCollisions &&
               a.enableJellyEffect == b.enableJellyEffect &&
               a.enableResetOnSpace == b.enableResetOnSpace &&
               a.jellyStrength == b.jellyStrength &&
               a.bounceFactor == b.bounceFactor;
    }

    private void TestBlob()
    {
        blobTestObject = new GameObject("BlobTest");
        activeBlobTest = blobTestObject.AddComponent<BlobTest>();
    }
}

public class BlobTest : MonoBehaviour
{
    private Blob blob;
    private LineRenderer lineRenderer;
    private CameraController cameraController;
    private SoftBodySimulator simulator;
    private Vector2 initialCenter;

    private List<Collider2D> solidObjects = new List<Collider2D>();
    private List<Blob> allBlobs = new List<Blob>(); // For soft-body collisions

    void Start()
    {
        simulator = SoftBodySimulator.Instance;

        // Find or create camera controller
        cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController == null)
        {
            cameraController = Camera.main.gameObject.AddComponent<CameraController>();
        }

        // Use the camera's viewport center to get world coordinates
        Vector2 center = Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0.5f));
        initialCenter = center;

        // Create blob with parameters from the simulator
        blob = CreateBlob(center);
        allBlobs.Add(blob);

        // Setup renderer
        SetupRenderer();

        // Find all colliders in the scene
        solidObjects.AddRange(FindObjectsByType<Collider2D>(FindObjectsSortMode.None));
    }

    private Blob CreateBlob(Vector2 center)
    {
        return new Blob(
            center,
            simulator.blobParams.points,
            simulator.blobParams.radius,
            simulator.blobParams.puffy,
            simulator.blobParams.dampening,
            simulator.blobParams.gravity,
            simulator.blobParams.maxDisplacement,
            simulator.blobParams.maxVelocity,
            simulator.features
        );
    }

    private void SetupRenderer()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = simulator.lineWidth;
        lineRenderer.endWidth = simulator.lineWidth;
        lineRenderer.positionCount = blob.Points.Count * simulator.splineResolution;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = simulator.blobColor;
        lineRenderer.endColor = simulator.blobColor;
        lineRenderer.loop = true; // Ensure the line forms a closed loop
    }

    public void UpdateParameters(bool recreateBlob = false)
    {
        if (recreateBlob)
        {
            // Recreate blob with new point count
            Vector2 currentCenter = blob.GetCenter();
            blob = CreateBlob(currentCenter);
            allBlobs.Clear();
            allBlobs.Add(blob);

            // Update renderer
            lineRenderer.positionCount = blob.Points.Count * simulator.splineResolution;
        }
        else
        {
            // Update simple parameters without recreating
            blob.UpdateParameters(
                simulator.blobParams.dampening,
                simulator.blobParams.gravity,
                simulator.blobParams.radius,
                simulator.blobParams.puffy,
                simulator.blobParams.maxDisplacement,
                simulator.blobParams.maxVelocity,
                simulator.features
            );
        }

        // Update renderer settings
        lineRenderer.startWidth = simulator.lineWidth;
        lineRenderer.endWidth = simulator.lineWidth;
        lineRenderer.startColor = simulator.blobColor;
        lineRenderer.endColor = simulator.blobColor;
    }

    void Update()
    {
        // Get mouse input
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool isRightMousePressed = Input.GetMouseButton(1); // Right mouse button
        bool isRightMouseReleased = Input.GetMouseButtonUp(1); // Right mouse button released

        // Get current screen bounds from camera controller
        Bounds screenBounds = cameraController.GetScreenBounds();

        // Update blob physics
        blob.Update(
            mousePosition,
            isRightMousePressed,
            isRightMouseReleased,
            screenBounds,
            solidObjects,
            allBlobs,
            simulator.blobParams.mouseInteractionRadius
        );

        // Update rendering using Catmull-Rom splines
        DrawBlobWithSplines();
    }

    private void DrawBlobWithSplines()
    {
        List<Vector2> points = new List<Vector2>();
        for (int i = 0; i < blob.Points.Count; i++)
        {
            points.Add(blob.Points[i].Position);
        }

        // Close the loop by adding the first few points at the end
        points.Add(blob.Points[0].Position);
        points.Add(blob.Points[1 % blob.Points.Count].Position);
        points.Add(blob.Points[2 % blob.Points.Count].Position);

        // Calculate Catmull-Rom spline points
        int index = 0;
        for (int i = 0; i < blob.Points.Count; i++)
        {
            Vector2 p0 = points[i];
            Vector2 p1 = points[(i + 1) % points.Count];
            Vector2 p2 = points[(i + 2) % points.Count];
            Vector2 p3 = points[(i + 3) % points.Count];

            for (int j = 0; j < simulator.splineResolution; j++)
            {
                float t = j / (float)simulator.splineResolution;
                Vector2 splinePoint = SplineHelper.CatmullRom(p0, p1, p2, p3, t);
                lineRenderer.SetPosition(index++, splinePoint);
            }
        }
    }
}

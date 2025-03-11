using System.Collections.Generic;
using UnityEngine;

public class SoftBodySimulator : MonoBehaviour
{
    public static SoftBodySimulator Instance { get; private set; }
    public float dampening = 0.99f;
    public float gravity = 1f;
    public float area = 2f;
    public float puffy = 1.5f;
    public int points = 16;
    public float maxDisplacement = 0.1f;
    public enum SimulationType
    {
        BlobOnly,

    }

    public SimulationType simulationType = SimulationType.BlobOnly;

    // References to test objects
    private GameObject blobTestObject;

    private void Awake()
    {
        // Ensure only one instance of SoftBodySimulator exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }
    void Start()
    {

        TestBlob();

    }

    private void TestBlob()
    {
        blobTestObject = new GameObject("BlobTest");
        blobTestObject.AddComponent<BlobTest>();
    }



  
}

public class BlobTest : MonoBehaviour
{
    private Blob blob;
    private LineRenderer lineRenderer;
    private CameraController cameraController;
    [SerializeField] private int splineResolution = 10; // Number of points between control points

    private List<Collider2D> solidObjects = new List<Collider2D>();
    private List<Blob> allBlobs = new List<Blob>(); // For soft-body collisions

    void Start()
    {
        // Find or create camera controller
        cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController == null)
        {
            cameraController = Camera.main.gameObject.AddComponent<CameraController>();
        }

        // Use the camera's viewport center to get world coordinates
        Vector2 center = Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0.5f));
        SoftBodySimulator sim = SoftBodySimulator.Instance;
        blob = new Blob(center, sim.points, sim.area, sim.puffy, sim.dampening, sim.gravity);

        allBlobs.Add(blob);

        // Setup renderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f; // Smaller width for better visibility
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = blob.Points.Count * splineResolution; // Increase resolution for smoothness
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;

        solidObjects.AddRange(FindObjectsByType<Collider2D>(FindObjectsSortMode.None));
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
        blob.Update(mousePosition, isRightMousePressed, isRightMouseReleased, screenBounds, solidObjects, allBlobs);

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
        points.Add(blob.Points[1].Position);
        points.Add(blob.Points[2].Position);

        // Calculate Catmull-Rom spline points
        int index = 0;
        for (int i = 0; i < blob.Points.Count; i++)
        {
            Vector2 p0 = points[i];
            Vector2 p1 = points[(i + 1) % points.Count];
            Vector2 p2 = points[(i + 2) % points.Count];
            Vector2 p3 = points[(i + 3) % points.Count];

            for (int j = 0; j < splineResolution; j++)
            {
                float t = j / (float)splineResolution;
                Vector2 splinePoint = SplineHelper.CatmullRom(p0, p1, p2, p3, t);
                lineRenderer.SetPosition(index++, splinePoint);
            }
        }
    }
}



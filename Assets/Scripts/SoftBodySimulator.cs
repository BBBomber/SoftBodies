using UnityEngine;

public class SoftBodySimulator : MonoBehaviour
{
    public static SoftBodySimulator Instance { get; private set; }
    public float dampening = 0.99f;
    public float gravity = 1f;
    public float area = 2f;
    public float puffy = 1.5f;
    public int points = 16;
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



    void Start()
    {
        SoftBodySimulator sim = SoftBodySimulator.Instance;
        // Use the camera's viewport center to get world coordinates
        Vector2 center = Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0.5f));
        blob = new Blob(center,sim.points , sim.area, sim.puffy, sim.dampening, sim.gravity); // Smaller radius for testing

        // Setup renderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f; // Smaller width for better visibility
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = blob.Points.Count + 1; // +1 to close the loop
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
    }

    void Update()
    {
        // Get mouse input
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool isRightMousePressed = Input.GetMouseButton(1); // Right mouse button
        bool isRightMouseReleased = Input.GetMouseButtonUp(1); // Right mouse button released

        // Update blob physics
        blob.Update(mousePosition, isRightMousePressed, isRightMouseReleased, Screen.width, Screen.height);

        // Update rendering
        for (int i = 0; i < blob.Points.Count; i++)
        {
            lineRenderer.SetPosition(i, blob.Points[i].Position);
        }
        // Close the loop
        lineRenderer.SetPosition(blob.Points.Count, blob.Points[0].Position);
    }
}



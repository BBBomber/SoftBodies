using UnityEngine;
using SoftBodyPhysics;

public class SpringBodyTest : MonoBehaviour
{
    [Header("Circle Parameters")]
    public float circleRadius = 1f;
    public int pointCount = 12;
    public float springStiffness = 0.7f;
    public float springDamping = 0.1f;
    public bool createPerimeterSprings = true;
    public bool createInternalSprings = true;

    [Header("Enhanced Structure")]
    public float structuralStiffness = 0.9f;
    public bool createRadialSprings = true;
    public bool createCrossSprings = true;

    [Header("Pressure")]
    public bool useGasPressure = true;
    public float pressureAmount = 1.2f;
    public Color fillColor = Color.white;
    public Vector2 position = new Vector2(4, 4);

    [Header("Runtime Updates")]
    public bool recreateOnParameterChange = false;
    public KeyCode increaseRadiusKey = KeyCode.P;
    public KeyCode decreaseRadiusKey = KeyCode.O;
    public KeyCode increasePressureKey = KeyCode.L;
    public KeyCode decreasePressureKey = KeyCode.K;
    public KeyCode increaseStiffnessKey = KeyCode.M;
    public KeyCode decreaseStiffnessKey = KeyCode.N;
    public KeyCode increaseStructuralStiffnessKey = KeyCode.J;
    public KeyCode decreaseStructuralStiffnessKey = KeyCode.H;
    public float changeAmount = 0.1f;

    private CircularSpringBody circleBody;
    private GameObject circleObj;

    // Previous values to detect changes
    private float prevRadius;
    private int prevPointCount;
    private float prevStiffness;
    private float prevDamping;
    private bool prevPerimeterSprings;
    private bool prevInternalSprings;
    private bool prevRadialSprings;
    private bool prevCrossSprings;
    private float prevStructuralStiffness;
    private bool prevUseGas;
    private float prevPressure;
    private Color prevColor;
    private Vector2 prevPosition;

    public void Start()
    {
        CreateCircleBody();
        StoreCurrentValues();
    }

    private void CreateCircleBody()
    {
        // Create a circular spring body
        if (circleObj != null)
        {
            Destroy(circleObj);
        }

        circleObj = new GameObject("SpringCircle");
        circleBody = circleObj.AddComponent<CircularSpringBody>();

        // Configure the circle
        circleBody.radius = circleRadius;
        circleBody.numPoints = pointCount;
        circleBody.springStiffness = springStiffness;
        circleBody.springDamping = springDamping;
        circleBody.createPerimeterSprings = createPerimeterSprings;
        circleBody.createInternalSprings = createInternalSprings;

        // Configure enhanced structure
        circleBody.structuralStiffness = structuralStiffness;
        circleBody.createRadialSprings = createRadialSprings;
        circleBody.createCrossSprings = createCrossSprings;

        // Configure pressure
        circleBody.useGasPressure = useGasPressure;
        circleBody.pressureAmount = pressureAmount;
        circleBody.fillColor = fillColor;

        // Position it
        circleObj.transform.position = position;
    }

    private void StoreCurrentValues()
    {
        prevRadius = circleRadius;
        prevPointCount = pointCount;
        prevStiffness = springStiffness;
        prevDamping = springDamping;
        prevPerimeterSprings = createPerimeterSprings;
        prevInternalSprings = createInternalSprings;
        prevRadialSprings = createRadialSprings;
        prevCrossSprings = createCrossSprings;
        prevStructuralStiffness = structuralStiffness;
        prevUseGas = useGasPressure;
        prevPressure = pressureAmount;
        prevColor = fillColor;
        prevPosition = position;
    }

    private bool HaveValuesChanged()
    {
        return prevRadius != circleRadius ||
               prevPointCount != pointCount ||
               prevStiffness != springStiffness ||
               prevDamping != springDamping ||
               prevPerimeterSprings != createPerimeterSprings ||
               prevInternalSprings != createInternalSprings ||
               prevRadialSprings != createRadialSprings ||
               prevCrossSprings != createCrossSprings ||
               prevStructuralStiffness != structuralStiffness ||
               prevUseGas != useGasPressure ||
               prevPressure != pressureAmount ||
               prevColor != fillColor ||
               prevPosition != position;
    }

    void Update()
    {
        // Handle keyboard input for parameter changes
        if (Input.GetKey(increaseRadiusKey)) circleRadius += changeAmount * Time.deltaTime;
        if (Input.GetKey(decreaseRadiusKey)) circleRadius = Mathf.Max(0.1f, circleRadius - changeAmount * Time.deltaTime);

        if (Input.GetKey(increasePressureKey)) pressureAmount += changeAmount * Time.deltaTime;
        if (Input.GetKey(decreasePressureKey)) pressureAmount = Mathf.Max(0.1f, pressureAmount - changeAmount * Time.deltaTime);

        if (Input.GetKey(increaseStiffnessKey)) springStiffness = Mathf.Min(1f, springStiffness + changeAmount * 0.5f * Time.deltaTime);
        if (Input.GetKey(decreaseStiffnessKey)) springStiffness = Mathf.Max(0.01f, springStiffness - changeAmount * 0.5f * Time.deltaTime);

        if (Input.GetKey(increaseStructuralStiffnessKey)) structuralStiffness = Mathf.Min(1f, structuralStiffness + changeAmount * 0.5f * Time.deltaTime);
        if (Input.GetKey(decreaseStructuralStiffnessKey)) structuralStiffness = Mathf.Max(0.1f, structuralStiffness - changeAmount * 0.5f * Time.deltaTime);

        // Check if values have changed
        if (HaveValuesChanged())
        {
            if (recreateOnParameterChange)
            {
                // Recreate the body completely
                CreateCircleBody();
            }
            else if (circleBody != null)
            {
                // Update parameters without recreating
                if (circleRadius != prevRadius ||
                    pointCount != prevPointCount ||
                    createRadialSprings != prevRadialSprings ||
                    createCrossSprings != prevCrossSprings)
                {
                    // These require recreation
                    CreateCircleBody();
                }
                else
                {
                    // These can be updated directly
                    circleBody.springStiffness = springStiffness;
                    circleBody.springDamping = springDamping;
                    circleBody.structuralStiffness = structuralStiffness;
                    circleBody.useGasPressure = useGasPressure;
                    circleBody.pressureAmount = pressureAmount;
                    circleBody.fillColor = fillColor;
                    circleObj.transform.position = position;
                }
            }

            // Store the current values
            StoreCurrentValues();
        }
    }

    void OnGUI()
    {
        // Display current parameters
        GUILayout.BeginArea(new Rect(10, 160, 250, 300));
        GUILayout.Label("Circle Parameters:");
        GUILayout.Label($"Radius: {circleRadius:F2} (P/O)");
        GUILayout.Label($"Pressure: {pressureAmount:F2} (L/K)");
        GUILayout.Label($"Spring Stiffness: {springStiffness:F2} (M/N)");
        GUILayout.Label($"Structural Stiffness: {structuralStiffness:F2} (J/H)");
        GUILayout.Label($"Radial Springs: {createRadialSprings}");
        GUILayout.Label($"Cross Springs: {createCrossSprings}");
        GUILayout.EndArea();
    }
}
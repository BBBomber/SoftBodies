using System.Collections.Generic;
using UnityEngine;


// Enums for different facial feature types
public enum EyeType { Circle, Oval, Rectangle, Angry, Sleepy, Wink, Surprised }
public enum MouthType { Curve, Line, Zigzag, CircleShape, Square, OpenSmile, Frown }
public enum EyebrowType { None, Straight, Angled, Curved, Worried, Angry }


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



[System.Serializable]
public class BlobFace
{
    [Header("Face Toggle")]
    [Tooltip("Enable facial features")]
    public bool enableFace = true;

    [Header("Eye Settings")]
    [Tooltip("Type of eyes to display")]
    public EyeType eyeType = EyeType.Circle;

    [Tooltip("Eye size relative to blob radius")]
    [Range(0.05f, 0.3f)]
    public float eyeSize = 0.15f;

    [Tooltip("Distance between eyes relative to blob radius")]
    [Range(0.1f, 1f)]
    public float eyeDistance = 0.5f;

    [Tooltip("Eye height relative to blob center")]
    [Range(-0.5f, 0.5f)]
    public float eyeHeight = 0.2f;

    [Tooltip("Eye color")]
    public Color eyeColor = Color.black;

    [Tooltip("Eye pupil color (if applicable)")]
    public Color pupilColor = Color.white;

    [Tooltip("Eye blink rate (0 = no blinking)")]
    [Range(0f, 10f)]
    public float blinkRate = 3f;

    [Header("Mouth Settings")]
    [Tooltip("Type of mouth to display")]
    public MouthType mouthType = MouthType.Curve;

    [Tooltip("Mouth width relative to blob radius")]
    [Range(0.1f, 1f)]
    public float mouthWidth = 0.4f;

    [Tooltip("Mouth height relative to blob radius")]
    [Range(-0.5f, 0.2f)]
    public float mouthHeight = -0.2f;

    [Tooltip("Mouth curvature (0 = flat, 1 = happy, -1 = sad)")]
    [Range(-1f, 1f)]
    public float mouthCurvature = 0.3f;

    [Tooltip("Mouth color")]
    public Color mouthColor = Color.black;

    [Tooltip("Enable tongue")]
    public bool enableTongue = false;

    [Tooltip("Tongue color")]
    public Color tongueColor = new Color(1f, 0.5f, 0.5f);

    [Tooltip("Tongue size relative to mouth")]
    [Range(0.1f, 0.9f)]
    public float tongueSize = 0.5f;

    [Header("Eyebrow Settings")]
    [Tooltip("Type of eyebrows")]
    public EyebrowType eyebrowType = EyebrowType.None;

    [Tooltip("Eyebrow thickness relative to line width")]
    [Range(0.5f, 3f)]
    public float eyebrowThickness = 1.5f;

    [Tooltip("Eyebrow color")]
    public Color eyebrowColor = Color.black;

    [Tooltip("Eyebrow height relative to eyes")]
    [Range(0.01f, 0.3f)]
    public float eyebrowHeight = 0.1f;

    [Tooltip("Eyebrow angle (-1 = angry, 0 = neutral, 1 = surprised)")]
    [Range(-1f, 1f)]
    public float eyebrowAngle = 0f;

    [Header("Accessories")]
    [Tooltip("Enable glasses")]
    public bool enableGlasses = false;

    [Tooltip("Glasses style (0 = round, 1 = square, 2 = hipster)")]
    [Range(0, 2)]
    public int glassesStyle = 0;

    [Tooltip("Glasses color")]
    public Color glassesColor = new Color(0.1f, 0.1f, 0.1f);

    [Tooltip("Glasses size relative to eye distance")]
    [Range(1f, 2f)]
    public float glassesSize = 1.2f;

    [Tooltip("Enable dimples")]
    public bool enableDimples = false;

    [Tooltip("Dimple size relative to blob radius")]
    [Range(0.01f, 0.1f)]
    public float dimpleSize = 0.05f;

    [Tooltip("Dimple color")]
    public Color dimpleColor = new Color(1f, 0.8f, 0.8f);

    [Tooltip("Enable mustache")]
    public bool enableMustache = false;

    [Tooltip("Mustache style (0 = normal, 1 = handlebar, 2 = thin)")]
    [Range(0, 2)]
    public int mustacheStyle = 0;

    [Tooltip("Mustache color")]
    public Color mustacheColor = Color.black;

    [Tooltip("Mustache size relative to mouth width")]
    [Range(0.5f, 1.5f)]
    public float mustacheSize = 1f;

    [Header("Animation")]
    [Tooltip("Enable idle animation")]
    public bool enableIdleAnimation = true;

    [Tooltip("Idle animation speed")]
    [Range(0.1f, 5f)]
    public float idleAnimationSpeed = 1f;

    [Tooltip("Expression change frequency (0 = static expression)")]
    [Range(0f, 1f)]
    public float expressionChangeFrequency = 0f;
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

    [Header("Facial Features")]
    public BlobFace face = new BlobFace();



    private float lastLineWidth;
    private Color lastBlobColor;
    private int lastSplineResolution;

    public enum SimulationType
    {
        BlobOnly
    }

    public SimulationType simulationType = SimulationType.BlobOnly;

    // References to test objects
    private GameObject blobTestObject;
    private  BlobTest activeBlobTest;

    // Cache for parameter comparison
    private BlobParameters lastParams;
    private BlobFeatures lastFeatures;


     public SlimeCharacterController controller;

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
            lastLineWidth = lineWidth;
            lastBlobColor = blobColor;
            lastSplineResolution = splineResolution;
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
        // Check if rendering properties changed
        bool renderingChanged = lineWidth != lastLineWidth ||
                                blobColor != lastBlobColor ||
                                splineResolution != lastSplineResolution;
        // Update any active blob test if parameters change
        if (activeBlobTest != null && (paramsChanged || featuresChanged || renderingChanged))
        {
            activeBlobTest.UpdateParameters(paramsChanged && blobParams.points != lastParams.points);

            // Update cache
            lastParams = CloneParameters(blobParams);
            lastFeatures = CloneFeatures(features);
            lastLineWidth = lineWidth;
            lastBlobColor = blobColor;
            lastSplineResolution = splineResolution;
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
    public Blob blob;
    private LineRenderer lineRenderer;
    private CameraController cameraController;
    private SoftBodySimulator simulator;
    private Vector2 initialCenter;

    private List<Collider2D> solidObjects = new List<Collider2D>();
    private List<Blob> allBlobs = new List<Blob>(); // For soft-body collisions

    // Base facial feature objects
    private GameObject leftEye;
    private GameObject rightEye;
    private GameObject mouth;
    private GameObject leftEyebrow;
    private GameObject rightEyebrow;
    private GameObject glasses;
    private GameObject leftDimple;
    private GameObject rightDimple;
    private GameObject mustache;
    private GameObject tongue;

    // Renderers for facial features
    private SpriteRenderer leftEyeRenderer;
    private SpriteRenderer rightEyeRenderer;
    private LineRenderer mouthRenderer;
    private LineRenderer leftEyebrowRenderer;
    private LineRenderer rightEyebrowRenderer;
    private LineRenderer glassesRenderer;
    private SpriteRenderer leftDimpleRenderer;
    private SpriteRenderer rightDimpleRenderer;
    private LineRenderer mustacheRenderer;
    private SpriteRenderer tongueRenderer;

    // Animation variables
    private float blinkTimer = 0f;
    private bool isBlinking = false;
    private float expressionTimer = 0f;
    private float idleAnimationTime = 0f;

    // Feature cache to detect changes
    private EyeType lastEyeType;
    private MouthType lastMouthType;
    private EyebrowType lastEyebrowType;
    private bool lastGlassesEnabled;
    private int lastGlassesStyle;
    private bool lastDimplesEnabled;
    private bool lastMustacheEnabled;
    private int lastMustacheStyle;
    private bool lastTongueEnabled;

    CameraController cam;


    void Start()
    {
        simulator = SoftBodySimulator.Instance;
        cam = Camera.main?.GetComponent<CameraController>();
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
        SoftBodySimulator.Instance.controller.Initialize(this);
        allBlobs.Add(blob);
        
        // Setup renderer
        SetupRenderer();
        SetupFacialFeatures();
        // Find all colliders in the scene
        solidObjects.AddRange(FindObjectsByType<Collider2D>(FindObjectsSortMode.None));
        cam.target = this.mouth.transform;
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

        // Always update rendering properties
        lineRenderer.startWidth = simulator.lineWidth;
        lineRenderer.endWidth = simulator.lineWidth;
        lineRenderer.startColor = simulator.blobColor;
        lineRenderer.endColor = simulator.blobColor;
        lineRenderer.positionCount = blob.Points.Count * simulator.splineResolution;


    }

    void Update()
    {
        // Get mouse input
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool isRightMousePressed = Input.GetMouseButton(1); // Right mouse button
        bool isRightMouseReleased = Input.GetMouseButtonUp(1); // Right mouse button released


        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeRandomExpression(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChangeRandomExpression(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChangeRandomExpression(3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ChangeRandomExpression(4);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ChangeRandomExpression(5);
        }

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
        UpdateFacialFeatures();
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



    // Call this from your Start method after creating the blob
    private void SetupFacialFeatures()
    {
        // Create eye objects
        leftEye = new GameObject("LeftEye");
        rightEye = new GameObject("RightEye");
        leftEye.transform.parent = transform;
        rightEye.transform.parent = transform;

        leftEyeRenderer = leftEye.AddComponent<SpriteRenderer>();
        rightEyeRenderer = rightEye.AddComponent<SpriteRenderer>();

        // Create mouth object
        mouth = new GameObject("Mouth");
        mouth.transform.parent = transform;
        mouthRenderer = mouth.AddComponent<LineRenderer>();
        mouthRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Create eyebrow objects
        leftEyebrow = new GameObject("LeftEyebrow");
        rightEyebrow = new GameObject("RightEyebrow");
        leftEyebrow.transform.parent = transform;
        rightEyebrow.transform.parent = transform;

        leftEyebrowRenderer = leftEyebrow.AddComponent<LineRenderer>();
        rightEyebrowRenderer = rightEyebrow.AddComponent<LineRenderer>();
        leftEyebrowRenderer.material = new Material(Shader.Find("Sprites/Default"));
        rightEyebrowRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Create glasses object
        glasses = new GameObject("Glasses");
        glasses.transform.parent = transform;
        glassesRenderer = glasses.AddComponent<LineRenderer>();
        glassesRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Create dimple objects
        leftDimple = new GameObject("LeftDimple");
        rightDimple = new GameObject("RightDimple");
        leftDimple.transform.parent = transform;
        rightDimple.transform.parent = transform;

        leftDimpleRenderer = leftDimple.AddComponent<SpriteRenderer>();
        rightDimpleRenderer = rightDimple.AddComponent<SpriteRenderer>();

        // Create mustache object
        mustache = new GameObject("Mustache");
        mustache.transform.parent = transform;
        mustacheRenderer = mustache.AddComponent<LineRenderer>();
        mustacheRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Create tongue object
        tongue = new GameObject("Tongue");
        tongue.transform.parent = transform;
        tongueRenderer = tongue.AddComponent<SpriteRenderer>();

        // Initialize feature cache
        lastEyeType = simulator.face.eyeType;
        lastMouthType = simulator.face.mouthType;
        lastEyebrowType = simulator.face.eyebrowType;
        lastGlassesEnabled = simulator.face.enableGlasses;
        lastGlassesStyle = simulator.face.glassesStyle;
        lastDimplesEnabled = simulator.face.enableDimples;
        lastMustacheEnabled = simulator.face.enableMustache;
        lastMustacheStyle = simulator.face.mustacheStyle;
        lastTongueEnabled = simulator.face.enableTongue;

        // Initial setup of all features
        UpdateEyeType();
        UpdateMouthType();
        UpdateEyebrowType();
        UpdateGlasses();
        UpdateDimples();
        UpdateMustache();
        UpdateTongue();

        if (leftEye != null)
        {
            leftEyeRenderer.color = simulator.face.eyeColor;
            rightEyeRenderer.color = simulator.face.eyeColor;
            mouthRenderer.startColor = simulator.face.mouthColor;
            mouthRenderer.endColor = simulator.face.mouthColor;
        }
    }

    // Update facial features position - call this from your Update method
    private void UpdateFacialFeatures()
    {
        if (!simulator.face.enableFace)
            return;

        Vector2 blobCenter = blob.GetCenter();
        float blobRadius = blob.Radius;

        // Check if feature types have changed
        if (lastEyeType != simulator.face.eyeType)
        {
            lastEyeType = simulator.face.eyeType;
            UpdateEyeType();
        }

        if (lastMouthType != simulator.face.mouthType)
        {
            lastMouthType = simulator.face.mouthType;
            UpdateMouthType();
        }

        if (lastEyebrowType != simulator.face.eyebrowType)
        {
            lastEyebrowType = simulator.face.eyebrowType;
            UpdateEyebrowType();
        }

        if (lastGlassesEnabled != simulator.face.enableGlasses ||
            lastGlassesStyle != simulator.face.glassesStyle)
        {
            lastGlassesEnabled = simulator.face.enableGlasses;
            lastGlassesStyle = simulator.face.glassesStyle;
            UpdateGlasses();
        }

        if (lastDimplesEnabled != simulator.face.enableDimples)
        {
            lastDimplesEnabled = simulator.face.enableDimples;
            UpdateDimples();
        }

        if (lastMustacheEnabled != simulator.face.enableMustache ||
            lastMustacheStyle != simulator.face.mustacheStyle)
        {
            lastMustacheEnabled = simulator.face.enableMustache;
            lastMustacheStyle = simulator.face.mustacheStyle;
            UpdateMustache();
        }

        if (lastTongueEnabled != simulator.face.enableTongue)
        {
            lastTongueEnabled = simulator.face.enableTongue;
            UpdateTongue();
        }

        // Update animation timers
        UpdateAnimationTimers();

        // Calculate common variables
        float eyeOffset = blobRadius * simulator.face.eyeDistance * 0.5f;
        float eyeY = blobCenter.y + blobRadius * simulator.face.eyeHeight;

        // Eye positioning
        leftEye.transform.position = new Vector3(blobCenter.x - eyeOffset, eyeY, -0.1f);
        rightEye.transform.position = new Vector3(blobCenter.x + eyeOffset, eyeY, -0.1f);

        // Eye scaling based on blob deformation
        float currentArea = blob.GetArea();
        float targetArea = blob.TargetArea;
        float areaRatio = currentArea / targetArea;
        float eyeScale = blobRadius * simulator.face.eyeSize * Mathf.Sqrt(areaRatio);

        leftEye.transform.localScale = new Vector3(eyeScale, isBlinking ? eyeScale * 0.2f : eyeScale, 1);
        rightEye.transform.localScale = new Vector3(eyeScale, isBlinking ? eyeScale * 0.2f : eyeScale, 1);

        // Update all other features
        UpdateMouthPosition(blobCenter, blobRadius);
        UpdateEyebrowPosition(blobCenter, blobRadius, eyeOffset, eyeY, eyeScale);
        UpdateGlassesPosition(blobCenter, blobRadius, eyeOffset, eyeY, eyeScale);
        UpdateDimplePosition(blobCenter, blobRadius);
        UpdateMustachePosition(blobCenter, blobRadius);
        UpdateTonguePosition(blobCenter, blobRadius);
    }



    private void UpdateAnimationTimers()
    {
        if (simulator.face.enableIdleAnimation)
        {
            idleAnimationTime += Time.deltaTime * simulator.face.idleAnimationSpeed;
        }

        // Handle blinking
        if (simulator.face.blinkRate > 0)
        {
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0)
            {
                if (!isBlinking)
                {
                    isBlinking = true;
                    blinkTimer = 0.1f; // Blink duration
                }
                else
                {
                    isBlinking = false;
                    blinkTimer = Random.Range(3f, 8f) / simulator.face.blinkRate; // Random time until next blink
                }
            }
        }

        /*// Handle expression changes
        if (simulator.face.expressionChangeFrequency > 0)
        {
            expressionTimer -= Time.deltaTime;
            if (expressionTimer <= 0)
            {
                // Change to a random expression
                ChangeRandomExpression();
                expressionTimer = Random.Range(5f, 15f) / simulator.face.expressionChangeFrequency;
            }
        }*/
    }

    private void ChangeRandomExpression(int expressionType)
    {
        // Randomly change face elements for a new expression
        //int expressionType = Random.Range(0, 5);

        switch (expressionType)
        {
            case 0: // Happy
                simulator.face.mouthCurvature = Random.Range(0.5f, 0.9f);
                simulator.face.eyebrowAngle = Random.Range(0f, 0.5f);
                simulator.face.eyeDistance = 0.5f;
                break;
            case 1: // Sad
                simulator.face.mouthCurvature = Random.Range(-0.9f, -0.4f);
                simulator.face.eyebrowAngle = Random.Range(-0.5f, 0f);
                simulator.face.eyeDistance = 0.5f;
                break;
            case 2: // Surprised
                simulator.face.mouthType = MouthType.CircleShape;
                simulator.face.eyeType = EyeType.Surprised;
                simulator.face.eyebrowAngle = Random.Range(0.6f, 1f);
                simulator.face.eyeDistance = 0.5f;
                break;
            case 3: // Angry
                simulator.face.mouthType = MouthType.Square;
                simulator.face.eyeType = EyeType.Angry;
                simulator.face.eyebrowAngle = Random.Range(-1f, -0.5f);
                simulator.face.eyeDistance = 0.5f;
                break;
            case 4: // Normal
                simulator.face.mouthType = MouthType.Curve;
                simulator.face.eyeType = EyeType.Circle;
                simulator.face.mouthCurvature = Random.Range(-0.2f, 0.2f);
                simulator.face.eyebrowAngle = 0f;
                simulator.face.eyeDistance = 0.5f;
                break;
            case 5:
                simulator.face.mouthType = MouthType.Zigzag;
                simulator.face.eyeType = EyeType.Sleepy;
                simulator.face.eyeDistance = 0.8f;
                simulator.face.mouthCurvature = 0.6f;
                break;
        }

        // Update the feature cache to reflect these changes
        lastMouthType = simulator.face.mouthType;
        lastEyeType = simulator.face.eyeType;

        // Update the changed features
        UpdateEyeType();
        UpdateMouthType();
        UpdateEyebrowType();
    }

    private void UpdateEyeType()
    {
        // Create a texture based on eye type
        Texture2D eyeTexture;
        switch (simulator.face.eyeType)
        {
            case EyeType.Circle:
                eyeTexture = CreateCircleTexture(32, simulator.face.eyeColor, simulator.face.pupilColor);
                break;
            case EyeType.Oval:
                eyeTexture = CreateOvalTexture(32, 24, simulator.face.eyeColor, simulator.face.pupilColor);
                break;
            case EyeType.Rectangle:
                eyeTexture = CreateRectangleTexture(24, 16, simulator.face.eyeColor);
                break;
            case EyeType.Angry:
                eyeTexture = CreateAngryEyeTexture(32, simulator.face.eyeColor, simulator.face.pupilColor);
                break;
            case EyeType.Sleepy:
                eyeTexture = CreateSleepyEyeTexture(32, simulator.face.eyeColor, simulator.face.pupilColor);
                break;
            case EyeType.Wink:
                // Create different textures for left and right eyes
                Texture2D leftEyeTex = CreateCircleTexture(32, simulator.face.eyeColor, simulator.face.pupilColor);
                Texture2D rightEyeTex = CreateSleepyEyeTexture(32, simulator.face.eyeColor, simulator.face.pupilColor);

                // Apply textures separately
                Sprite leftSprite = Sprite.Create(leftEyeTex, new Rect(0, 0, leftEyeTex.width, leftEyeTex.height),
                                               new Vector2(0.5f, 0.5f), 100);
                Sprite rightSprite = Sprite.Create(rightEyeTex, new Rect(0, 0, rightEyeTex.width, rightEyeTex.height),
                                                new Vector2(0.5f, 0.5f), 100);

                leftEyeRenderer.sprite = leftSprite;
                rightEyeRenderer.sprite = rightSprite;
                return;
            case EyeType.Surprised:
                eyeTexture = CreateSurprisedEyeTexture(32, simulator.face.eyeColor, simulator.face.pupilColor);
                break;
            default:
                eyeTexture = CreateCircleTexture(32, simulator.face.eyeColor, simulator.face.pupilColor);
                break;
        }

        // Create sprite from texture
        Sprite eyeSprite = Sprite.Create(eyeTexture, new Rect(0, 0, eyeTexture.width, eyeTexture.height),
                                         new Vector2(0.5f, 0.5f), 100);

        // Apply to both eyes
        leftEyeRenderer.sprite = eyeSprite;
        rightEyeRenderer.sprite = eyeSprite;
    }

    private void UpdateMouthType()
    {
        switch (simulator.face.mouthType)
        {
            case MouthType.Curve:
                mouthRenderer.positionCount = 10;
                mouthRenderer.startWidth = simulator.lineWidth * 0.8f;
                mouthRenderer.endWidth = simulator.lineWidth * 0.8f;
                break;
            case MouthType.Line:
                mouthRenderer.positionCount = 2;
                mouthRenderer.startWidth = simulator.lineWidth * 0.8f;
                mouthRenderer.endWidth = simulator.lineWidth * 0.8f;
                break;
            case MouthType.Zigzag:
                mouthRenderer.positionCount = 6;
                mouthRenderer.startWidth = simulator.lineWidth * 0.6f;
                mouthRenderer.endWidth = simulator.lineWidth * 0.6f;
                break;
            case MouthType.CircleShape:
                mouthRenderer.positionCount = 24;
                mouthRenderer.startWidth = simulator.lineWidth * 0.7f;
                mouthRenderer.endWidth = simulator.lineWidth * 0.7f;
                break;
            case MouthType.Square:
                mouthRenderer.positionCount = 5;
                mouthRenderer.startWidth = simulator.lineWidth * 0.7f;
                mouthRenderer.endWidth = simulator.lineWidth * 0.7f;
                break;
            case MouthType.OpenSmile:
                mouthRenderer.positionCount = 16;
                mouthRenderer.startWidth = simulator.lineWidth * 0.8f;
                mouthRenderer.endWidth = simulator.lineWidth * 0.8f;
                break;
            case MouthType.Frown:
                mouthRenderer.positionCount = 10;
                mouthRenderer.startWidth = simulator.lineWidth * 0.8f;
                mouthRenderer.endWidth = simulator.lineWidth * 0.8f;
                break;
        }

        mouthRenderer.startColor = simulator.face.mouthColor;
        mouthRenderer.endColor = simulator.face.mouthColor;
    }



    private void UpdateMouthPosition(Vector2 blobCenter, float blobRadius)
    {
        float mouthY = blobCenter.y + blobRadius * simulator.face.mouthHeight;
        float mouthWidth = blobRadius * simulator.face.mouthWidth;
        float mouthCurvature = simulator.face.mouthCurvature;

        switch (simulator.face.mouthType)
        {
            case MouthType.Curve:
                // Curved mouth
                for (int i = 0; i < mouthRenderer.positionCount; i++)
                {
                    float t = i / (float)(mouthRenderer.positionCount - 1);
                    float x = blobCenter.x - mouthWidth / 2 + mouthWidth * t;
                    float curveY = mouthCurvature * Mathf.Sin(Mathf.PI * t) * blobRadius * 0.2f;

                    // Add idle animation
                    if (simulator.face.enableIdleAnimation)
                    {
                        curveY += Mathf.Sin(idleAnimationTime * 2f + t * Mathf.PI) * blobRadius * 0.02f;
                    }

                    mouthRenderer.SetPosition(i, new Vector3(x, mouthY + curveY, -0.1f));
                }
                break;

            case MouthType.Line:
                // Straight line
                mouthRenderer.SetPosition(0, new Vector3(blobCenter.x - mouthWidth / 2, mouthY, -0.1f));
                mouthRenderer.SetPosition(1, new Vector3(blobCenter.x + mouthWidth / 2, mouthY, -0.1f));
                break;

            case MouthType.Zigzag:
                // Zigzag pattern
                for (int i = 0; i < mouthRenderer.positionCount; i++)
                {
                    float t = i / (float)(mouthRenderer.positionCount - 1);
                    float x = blobCenter.x - mouthWidth / 2 + mouthWidth * t;
                    float zigzagY = (i % 2 == 0) ? 0 : mouthCurvature * blobRadius * 0.1f;
                    mouthRenderer.SetPosition(i, new Vector3(x, mouthY + zigzagY, -0.1f));
                }
                break;

            case MouthType.CircleShape:
                // Circle-shaped mouth
                float mouthRadius = mouthWidth * 0.3f;
                for (int i = 0; i < mouthRenderer.positionCount; i++)
                {
                    float angle = 2f * Mathf.PI * i / mouthRenderer.positionCount;
                    float x = blobCenter.x + Mathf.Cos(angle) * mouthRadius;
                    float y = mouthY + Mathf.Sin(angle) * mouthRadius * (0.8f + mouthCurvature * 0.4f);
                    mouthRenderer.SetPosition(i, new Vector3(x, y, -0.1f));
                }
                break;

            case MouthType.Square:
                // Square-shaped mouth
                float halfWidth = mouthWidth * 0.3f;
                float halfHeight = halfWidth * (0.6f + mouthCurvature * 0.4f);
                mouthRenderer.SetPosition(0, new Vector3(blobCenter.x - halfWidth, mouthY - halfHeight, -0.1f));
                mouthRenderer.SetPosition(1, new Vector3(blobCenter.x + halfWidth, mouthY - halfHeight, -0.1f));
                mouthRenderer.SetPosition(2, new Vector3(blobCenter.x + halfWidth, mouthY + halfHeight, -0.1f));
                mouthRenderer.SetPosition(3, new Vector3(blobCenter.x - halfWidth, mouthY + halfHeight, -0.1f));
                mouthRenderer.SetPosition(4, new Vector3(blobCenter.x - halfWidth, mouthY - halfHeight, -0.1f));
                break;

            case MouthType.OpenSmile:
                // Open smile mouth (semi-circle)
                float openMouthWidth = mouthWidth * 0.6f;
                float openMouthHeight = mouthWidth * 0.4f * (mouthCurvature > 0 ? mouthCurvature : 0.3f);

                for (int i = 0; i < mouthRenderer.positionCount; i++)
                {
                    float t = i / (float)(mouthRenderer.positionCount - 1);
                    float angle = Mathf.PI * t;
                    float x = blobCenter.x + Mathf.Cos(angle) * openMouthWidth;
                    float y = mouthY + Mathf.Sin(angle) * openMouthHeight;
                    mouthRenderer.SetPosition(i, new Vector3(x, y, -0.1f));
                }
                break;

            case MouthType.Frown:
                // Frown mouth (inverse curve)
                for (int i = 0; i < mouthRenderer.positionCount; i++)
                {
                    float t = i / (float)(mouthRenderer.positionCount - 1);
                    float x = blobCenter.x - mouthWidth / 2 + mouthWidth * t;
                    float curveY = (mouthCurvature < 0 ? mouthCurvature : -0.5f) * Mathf.Sin(Mathf.PI * t) * blobRadius * 0.2f;
                    mouthRenderer.SetPosition(i, new Vector3(x, mouthY + curveY, -0.1f));
                }
                break;
        }
    }

    private void UpdateEyebrowType()
    {
        leftEyebrowRenderer.enabled = simulator.face.eyebrowType != EyebrowType.None;
        rightEyebrowRenderer.enabled = simulator.face.eyebrowType != EyebrowType.None;

        if (simulator.face.eyebrowType == EyebrowType.None)
            return;

        leftEyebrowRenderer.startWidth = simulator.lineWidth * simulator.face.eyebrowThickness;
        leftEyebrowRenderer.endWidth = simulator.lineWidth * simulator.face.eyebrowThickness;
        rightEyebrowRenderer.startWidth = simulator.lineWidth * simulator.face.eyebrowThickness;
        rightEyebrowRenderer.endWidth = simulator.lineWidth * simulator.face.eyebrowThickness;

        leftEyebrowRenderer.startColor = simulator.face.eyebrowColor;
        leftEyebrowRenderer.endColor = simulator.face.eyebrowColor;
        rightEyebrowRenderer.startColor = simulator.face.eyebrowColor;
        rightEyebrowRenderer.endColor = simulator.face.eyebrowColor;

        switch (simulator.face.eyebrowType)
        {
            case EyebrowType.Straight:
                leftEyebrowRenderer.positionCount = 2;
                rightEyebrowRenderer.positionCount = 2;
                break;
            case EyebrowType.Angled:
                leftEyebrowRenderer.positionCount = 2;
                rightEyebrowRenderer.positionCount = 2;
                break;
            case EyebrowType.Curved:
            case EyebrowType.Worried:
            case EyebrowType.Angry:
                leftEyebrowRenderer.positionCount = 5;
                rightEyebrowRenderer.positionCount = 5;
                break;
        }
    }

    private void UpdateEyebrowPosition(Vector2 blobCenter, float blobRadius, float eyeOffset, float eyeY, float eyeScale)
    {
        if (simulator.face.eyebrowType == EyebrowType.None)
            return;

        float eyebrowY = eyeY + eyeScale + blobRadius * simulator.face.eyebrowHeight;
        float eyebrowWidth = eyeScale * 1.3f;
        float angle = simulator.face.eyebrowAngle;

        // Calculate positions for left and right eyebrows
        Vector2 leftEyebrowStart = new Vector2(blobCenter.x - eyeOffset - eyebrowWidth / 2, eyebrowY);
        Vector2 leftEyebrowEnd = new Vector2(blobCenter.x - eyeOffset + eyebrowWidth / 2, eyebrowY);
        Vector2 rightEyebrowStart = new Vector2(blobCenter.x + eyeOffset - eyebrowWidth / 2, eyebrowY);
        Vector2 rightEyebrowEnd = new Vector2(blobCenter.x + eyeOffset + eyebrowWidth / 2, eyebrowY);

        // Apply angle to the eyebrows
        leftEyebrowStart.y += angle * blobRadius * 0.1f;
        leftEyebrowEnd.y -= angle * blobRadius * 0.1f;
        rightEyebrowStart.y += angle * blobRadius * 0.1f;
        rightEyebrowEnd.y -= angle * blobRadius * 0.1f;

        // Update positions based on eyebrow type
        switch (simulator.face.eyebrowType)
        {
            case EyebrowType.Straight:
                leftEyebrowRenderer.SetPosition(0, leftEyebrowStart);
                leftEyebrowRenderer.SetPosition(1, leftEyebrowEnd);
                rightEyebrowRenderer.SetPosition(0, rightEyebrowStart);
                rightEyebrowRenderer.SetPosition(1, rightEyebrowEnd);
                break;

            case EyebrowType.Angled:
                leftEyebrowRenderer.SetPosition(0, leftEyebrowStart);
                leftEyebrowRenderer.SetPosition(1, new Vector2(leftEyebrowEnd.x, leftEyebrowEnd.y + angle * blobRadius * 0.2f));
                rightEyebrowRenderer.SetPosition(0, rightEyebrowStart);
                rightEyebrowRenderer.SetPosition(1, new Vector2(rightEyebrowEnd.x, rightEyebrowEnd.y + angle * blobRadius * 0.2f));
                break;

            case EyebrowType.Curved:
            case EyebrowType.Worried:
            case EyebrowType.Angry:
                // Curved eyebrows
                for (int i = 0; i < leftEyebrowRenderer.positionCount; i++)
                {
                    float t = i / (float)(leftEyebrowRenderer.positionCount - 1);
                    float curveY = Mathf.Sin(t * Mathf.PI) * angle * blobRadius * 0.1f;

                    Vector2 leftPoint = Vector2.Lerp(leftEyebrowStart, leftEyebrowEnd, t);
                    leftPoint.y += curveY;
                    leftEyebrowRenderer.SetPosition(i, leftPoint);

                    Vector2 rightPoint = Vector2.Lerp(rightEyebrowStart, rightEyebrowEnd, t);
                    rightPoint.y += curveY;
                    rightEyebrowRenderer.SetPosition(i, rightPoint);
                }
                break;
        }
    }

    private void UpdateGlasses()
    {
        glassesRenderer.enabled = simulator.face.enableGlasses;

        if (!simulator.face.enableGlasses)
            return;

        glassesRenderer.startWidth = simulator.lineWidth * 0.5f;
        glassesRenderer.endWidth = simulator.lineWidth * 0.5f;
        glassesRenderer.startColor = simulator.face.glassesColor;
        glassesRenderer.endColor = simulator.face.glassesColor;

        switch (simulator.face.glassesStyle)
        {
            case 0: // Round
                glassesRenderer.positionCount = 24;
                break;
            case 1: // Square
                glassesRenderer.positionCount = 5;
                break;
            case 2: // Hipster
                glassesRenderer.positionCount = 16;
                break;
        }
    }

    private void UpdateGlassesPosition(Vector2 blobCenter, float blobRadius, float eyeOffset, float eyeY, float eyeScale)
    {
        if (!simulator.face.enableGlasses)
            return;

        float glassesWidth = eyeOffset * 2 * simulator.face.glassesSize;
        float glassesY = eyeY + eyeScale * 0.5f;

        switch (simulator.face.glassesStyle)
        {
            case 0: // Round
                float glassesRadius = glassesWidth * 0.3f;
                for (int i = 0; i < glassesRenderer.positionCount; i++)
                {
                    float angle = 2f * Mathf.PI * i / glassesRenderer.positionCount;
                    float x = blobCenter.x + Mathf.Cos(angle) * glassesRadius;
                    float y = glassesY + Mathf.Sin(angle) * glassesRadius;
                    glassesRenderer.SetPosition(i, new Vector3(x, y, -0.1f));
                }
                break;

            case 1: // Square
                float halfWidth = glassesWidth * 0.3f;
                float halfHeight = halfWidth * 0.6f;
                glassesRenderer.SetPosition(0, new Vector3(blobCenter.x - halfWidth, glassesY - halfHeight, -0.1f));
                glassesRenderer.SetPosition(1, new Vector3(blobCenter.x + halfWidth, glassesY - halfHeight, -0.1f));
                glassesRenderer.SetPosition(2, new Vector3(blobCenter.x + halfWidth, glassesY + halfHeight, -0.1f));
                glassesRenderer.SetPosition(3, new Vector3(blobCenter.x - halfWidth, glassesY + halfHeight, -0.1f));
                glassesRenderer.SetPosition(4, new Vector3(blobCenter.x - halfWidth, glassesY - halfHeight, -0.1f));
                break;

            case 2: // Hipster
                float hipsterWidth = glassesWidth * 0.4f;
                for (int i = 0; i < glassesRenderer.positionCount; i++)
                {
                    float t = i / (float)(glassesRenderer.positionCount - 1);
                    float x = blobCenter.x - hipsterWidth / 2 + hipsterWidth * t;
                    float y = glassesY + Mathf.Sin(t * Mathf.PI) * hipsterWidth * 0.2f;
                    glassesRenderer.SetPosition(i, new Vector3(x, y, -0.1f));
                }
                break;
        }
    }

    private void UpdateDimples()
    {
        leftDimpleRenderer.enabled = simulator.face.enableDimples;
        rightDimpleRenderer.enabled = simulator.face.enableDimples;

        if (!simulator.face.enableDimples)
            return;

        Texture2D dimpleTexture = CreateCircleTexture(16, simulator.face.dimpleColor);
        Sprite dimpleSprite = Sprite.Create(dimpleTexture, new Rect(0, 0, dimpleTexture.width, dimpleTexture.height), new Vector2(0.5f, 0.5f), 100);

        leftDimpleRenderer.sprite = dimpleSprite;
        rightDimpleRenderer.sprite = dimpleSprite;
    }

    private void UpdateDimplePosition(Vector2 blobCenter, float blobRadius)
    {
        if (!simulator.face.enableDimples)
            return;

        float dimpleOffset = blobRadius * 0.6f;
        float dimpleY = blobCenter.y - blobRadius * 0.3f;

        leftDimple.transform.position = new Vector3(blobCenter.x - dimpleOffset, dimpleY, -0.1f);
        rightDimple.transform.position = new Vector3(blobCenter.x + dimpleOffset, dimpleY, -0.1f);

        float dimpleScale = blobRadius * simulator.face.dimpleSize;
        leftDimple.transform.localScale = new Vector3(dimpleScale, dimpleScale, 1);
        rightDimple.transform.localScale = new Vector3(dimpleScale, dimpleScale, 1);
    }

    private void UpdateMustache()
    {
        mustacheRenderer.enabled = simulator.face.enableMustache;

        if (!simulator.face.enableMustache)
            return;

        mustacheRenderer.startWidth = simulator.lineWidth * 0.8f;
        mustacheRenderer.endWidth = simulator.lineWidth * 0.8f;
        mustacheRenderer.startColor = simulator.face.mustacheColor;
        mustacheRenderer.endColor = simulator.face.mustacheColor;

        switch (simulator.face.mustacheStyle)
        {
            case 0: // Normal
                mustacheRenderer.positionCount = 10;
                break;
            case 1: // Handlebar
                mustacheRenderer.positionCount = 16;
                break;
            case 2: // Thin
                mustacheRenderer.positionCount = 6;
                break;
        }
    }

    private void UpdateMustachePosition(Vector2 blobCenter, float blobRadius)
    {
        if (!simulator.face.enableMustache)
            return;

        float mustacheWidth = blobRadius * simulator.face.mouthWidth * simulator.face.mustacheSize;
        float mustacheY = blobCenter.y + blobRadius * simulator.face.mouthHeight - blobRadius * 0.1f;

        switch (simulator.face.mustacheStyle)
        {
            case 0: // Normal
                for (int i = 0; i < mustacheRenderer.positionCount; i++)
                {
                    float t = i / (float)(mustacheRenderer.positionCount - 1);
                    float x = blobCenter.x - mustacheWidth / 2 + mustacheWidth * t;
                    float y = mustacheY + Mathf.Sin(t * Mathf.PI) * blobRadius * 0.05f;
                    mustacheRenderer.SetPosition(i, new Vector3(x, y, -0.1f));
                }
                break;

            case 1: // Handlebar
                for (int i = 0; i < mustacheRenderer.positionCount; i++)
                {
                    float t = i / (float)(mustacheRenderer.positionCount - 1);
                    float x = blobCenter.x - mustacheWidth / 2 + mustacheWidth * t;
                    float y = mustacheY + Mathf.Sin(t * Mathf.PI * 2) * blobRadius * 0.1f;
                    mustacheRenderer.SetPosition(i, new Vector3(x, y, -0.1f));
                }
                break;

            case 2: // Thin
                for (int i = 0; i < mustacheRenderer.positionCount; i++)
                {
                    float t = i / (float)(mustacheRenderer.positionCount - 1);
                    float x = blobCenter.x - mustacheWidth / 2 + mustacheWidth * t;
                    float y = mustacheY + Mathf.Sin(t * Mathf.PI) * blobRadius * 0.02f;
                    mustacheRenderer.SetPosition(i, new Vector3(x, y, -0.1f));
                }
                break;
        }
    }

    private void UpdateTongue()
    {
        tongueRenderer.enabled = simulator.face.enableTongue;

        if (!simulator.face.enableTongue)
            return;

        Texture2D tongueTexture = CreateCircleTexture(16, simulator.face.tongueColor);
        Sprite tongueSprite = Sprite.Create(tongueTexture, new Rect(0, 0, tongueTexture.width, tongueTexture.height), new Vector2(0.5f, 0.5f), 100);

        tongueRenderer.sprite = tongueSprite;
    }

    private void UpdateTonguePosition(Vector2 blobCenter, float blobRadius)
    {
        if (!simulator.face.enableTongue)
            return;

        float tongueY = blobCenter.y + blobRadius * simulator.face.mouthHeight - blobRadius * 0.15f;
        float tongueScale = blobRadius * simulator.face.tongueSize;

        tongue.transform.position = new Vector3(blobCenter.x, tongueY, -0.1f);
        tongue.transform.localScale = new Vector3(tongueScale, tongueScale, 1);
    }

    // Helper method to create a circular texture for the eyes
    private Texture2D CreateCircleTexture(int size, Color color, Color pupilColor = default)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        float radius = size / 2f;
        float pupilRadius = radius * 0.4f; // Pupil size relative to the eye
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= pupilRadius)
                {
                    pixels[y * size + x] = pupilColor == default ? color : pupilColor;
                }
                else if (distance <= radius)
                {
                    pixels[y * size + x] = color;
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private Texture2D CreateOvalTexture(int width, int height, Color color, Color pupilColor = default)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        Vector2 center = new Vector2(width / 2f, height / 2f);
        float radiusX = width / 2f;
        float radiusY = height / 2f;
        float pupilRadiusX = radiusX * 0.4f;
        float pupilRadiusY = radiusY * 0.4f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalizedX = (x - center.x) / radiusX;
                float normalizedY = (y - center.y) / radiusY;
                float distance = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);

                if (distance <= 0.4f) // Pupil area
                {
                    pixels[y * width + x] = pupilColor == default ? color : pupilColor;
                }
                else if (distance <= 1f) // Eye area
                {
                    pixels[y * width + x] = color;
                }
                else
                {
                    pixels[y * width + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // Helper method to create a rectangular texture
    private Texture2D CreateRectangleTexture(int width, int height, Color color)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // Helper method to create an angry eye texture
    private Texture2D CreateAngryEyeTexture(int size, Color color, Color pupilColor = default)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        float radius = size / 2f;
        float pupilRadius = radius * 0.4f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= pupilRadius)
                {
                    pixels[y * size + x] = pupilColor == default ? color : pupilColor;
                }
                else if (distance <= radius)
                {
                    // Angry effect: Add a slant to the eye
                    if (Mathf.Abs(x - center.x) > Mathf.Abs(y - center.y) * 0.5f)
                    {
                        pixels[y * size + x] = color;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // Helper method to create a sleepy eye texture
    private Texture2D CreateSleepyEyeTexture(int size, Color color, Color pupilColor = default)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        float radius = size / 2f;
        float pupilRadius = radius * 0.4f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= pupilRadius)
                {
                    pixels[y * size + x] = pupilColor == default ? color : pupilColor;
                }
                else if (distance <= radius)
                {
                    // Sleepy effect: Only draw the bottom half of the eye
                    if (y > center.y)
                    {
                        pixels[y * size + x] = color;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    // Helper method to create a surprised eye texture
    private Texture2D CreateSurprisedEyeTexture(int size, Color color, Color pupilColor = default)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        float radius = size / 2f;
        float pupilRadius = radius * 0.4f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= pupilRadius)
                {
                    pixels[y * size + x] = pupilColor == default ? color : pupilColor;
                }
                else if (distance <= radius)
                {
                    // Surprised effect: Draw a larger pupil area
                    if (distance <= radius * 0.7f)
                    {
                        pixels[y * size + x] = color;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
}

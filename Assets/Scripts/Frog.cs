using UnityEngine;
using System.Collections.Generic;

public class Frog : MonoBehaviour
{
    
    // Blob properties
    public Blob Blob { get; private set; }
    public List<BlobPoint> BlobPoints => Blob?.Points; // Expose the blob's points

    // Limb properties
    public Limb LeftFrontLeg { get; private set; }
    public Limb RightFrontLeg { get; private set; }
    public Limb LeftHindLeg { get; private set; }
    public Limb RightHindLeg { get; private set; }

    // Rendering
    private LineRenderer blobOutlineRenderer;
    private List<LineRenderer> limbRenderers = new List<LineRenderer>();

    // Screen properties
    private float screenWidth;
    private float screenHeight;

    private void Start()
    {
        Vector2 origin = new Vector2(0, 0); // Center of the scene
        InitializeFrog(origin);

        // Setup rendering
        SetupRenderers();
    }

    private void InitializeFrog(Vector2 origin)
    {
        // Initialize the blob
        //Blob = new Blob(origin, 16, 30, 1.5f);

        // Initialize the limbs
        LeftFrontLeg = new Limb(origin - new Vector2(80, 0), 56, Mathf.PI / 4, Mathf.PI / 8, Mathf.PI / 5, -Mathf.PI / 4);
        RightFrontLeg = new Limb(origin - new Vector2(-80, 0), 56, Mathf.PI / 4, -Mathf.PI / 8, Mathf.PI / 5, Mathf.PI / 4);
        LeftHindLeg = new Limb(origin - new Vector2(100, 0), 100, 1.9f * Mathf.PI / 5, 2 * Mathf.PI / 5, 2 * Mathf.PI / 5, -2 * Mathf.PI / 5);
        RightHindLeg = new Limb(origin - new Vector2(-100, 0), 100, 1.9f * Mathf.PI / 5, -2 * Mathf.PI / 5, 2 * Mathf.PI / 5, 2 * Mathf.PI / 5);
    }

    private void SetupRenderers()
    {
        // Setup blob outline renderer
        blobOutlineRenderer = gameObject.AddComponent<LineRenderer>();
        blobOutlineRenderer.startWidth = 8f;
        blobOutlineRenderer.endWidth = 8f;
        blobOutlineRenderer.positionCount = Blob.Points.Count + 1; // +1 to close the loop
        blobOutlineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        blobOutlineRenderer.startColor = Color.black;
        blobOutlineRenderer.endColor = Color.black;

        // Setup limb renderers (4 legs)
        for (int i = 0; i < 4; i++)
        {
            LineRenderer lr = new GameObject($"Limb_{i}").AddComponent<LineRenderer>();
            lr.transform.parent = transform;
            lr.startWidth = 8f;
            lr.endWidth = 8f;
            lr.positionCount = 3; // anchor, elbow, foot
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.black;
            lr.endColor = Color.black;
            limbRenderers.Add(lr);
        }
    }

    private void Update()
    {
        // Get mouse input
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool isMousePressed = Input.GetMouseButton(0);

        // Update physics
        //Blob.Update(mousePosition, isMousePressed, screenWidth, screenHeight);
        UpdateLimbs();

        // Update rendering
        UpdateBlobRenderer();
        UpdateLimbRenderers();
    }

    private void UpdateLimbs()
    {
        Vector2 leftFront = Blob.Points[12].Position;
        Vector2 rightFront = Blob.Points[4].Position;
        Vector2 leftFrontAnchor = Vector2.Lerp(leftFront, rightFront, 0.25f) + new Vector2(0, 10);
        Vector2 rightFrontAnchor = Vector2.Lerp(leftFront, rightFront, 0.75f) + new Vector2(0, 10);

        Vector2 midSecant = (rightFront - leftFront).normalized * 64;
        float midNormal = Mathf.Atan2(midSecant.y, midSecant.x) - Mathf.PI / 2;

        Vector2 leftHindAnchor = Blob.Points[11].Position + midSecant + new Vector2(0, 16);
        Vector2 rightHindAnchor = Blob.Points[5].Position - midSecant + new Vector2(0, 16);

        LeftFrontLeg.Resolve(leftFrontAnchor, midNormal, screenWidth, screenHeight);
        RightFrontLeg.Resolve(rightFrontAnchor, midNormal, screenWidth, screenHeight);

        // A little hack to make sure the hind legs go back into position when approaching the ground
        if (screenHeight - LeftHindLeg.Foot.Position.y < 100)
        {
            LeftHindLeg.Elbow.Position.y -= 1.5f;
            LeftHindLeg.Foot.Position.x += 0.5f;
        }
        if (screenHeight - RightHindLeg.Foot.Position.y < 100)
        {
            RightHindLeg.Elbow.Position.y -= 1.5f;
            RightHindLeg.Foot.Position.x -= 0.5f;
        }

        LeftHindLeg.Resolve(leftHindAnchor, midNormal, screenWidth, screenHeight);
        RightHindLeg.Resolve(rightHindAnchor, midNormal, screenWidth, screenHeight);
    }

    private void UpdateBlobRenderer()
    {
        for (int i = 0; i < Blob.Points.Count; i++)
        {
            blobOutlineRenderer.SetPosition(i, Blob.Points[i].Position);
        }
        // Close the loop
        blobOutlineRenderer.SetPosition(Blob.Points.Count, Blob.Points[0].Position);
    }

    private void UpdateLimbRenderers()
    {
        // Front left leg
        Vector2 leftFront = Blob.Points[12].Position;
        Vector2 rightFront = Blob.Points[4].Position;
        Vector2 leftFrontAnchor = Vector2.Lerp(leftFront, rightFront, 0.25f) + new Vector2(0, 10);
        limbRenderers[0].SetPosition(0, leftFrontAnchor);
        limbRenderers[0].SetPosition(1, LeftFrontLeg.Elbow.Position);
        limbRenderers[0].SetPosition(2, LeftFrontLeg.Foot.Position);

        // Front right leg
        Vector2 rightFrontAnchor = Vector2.Lerp(leftFront, rightFront, 0.75f) + new Vector2(0, 10);
        limbRenderers[1].SetPosition(0, rightFrontAnchor);
        limbRenderers[1].SetPosition(1, RightFrontLeg.Elbow.Position);
        limbRenderers[1].SetPosition(2, RightFrontLeg.Foot.Position);

        // Hind legs
        Vector2 midSecant = (rightFront - leftFront).normalized * 64;
        Vector2 leftHindAnchor = Blob.Points[11].Position + midSecant + new Vector2(0, 16);
        Vector2 rightHindAnchor = Blob.Points[5].Position - midSecant + new Vector2(0, 16);

        limbRenderers[2].SetPosition(0, leftHindAnchor);
        limbRenderers[2].SetPosition(1, LeftHindLeg.Elbow.Position);
        limbRenderers[2].SetPosition(2, LeftHindLeg.Foot.Position);

        limbRenderers[3].SetPosition(0, rightHindAnchor);
        limbRenderers[3].SetPosition(1, RightHindLeg.Elbow.Position);
        limbRenderers[3].SetPosition(2, RightHindLeg.Foot.Position);
    }
}
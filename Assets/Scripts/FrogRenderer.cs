using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Frog))]
public class FrogRenderer : MonoBehaviour
{
    private Frog frog;

    // Body renderer
    private MeshFilter bodyMeshFilter;
    private MeshRenderer bodyMeshRenderer;
    private Mesh bodyMesh;

    // Head components
    private GameObject headObject;
    private SpriteRenderer headRenderer;
    private SpriteRenderer leftEyeRenderer;
    private SpriteRenderer rightEyeRenderer;

    // Leg components
    private List<LineRenderer> legRenderers = new List<LineRenderer>();
    private List<SpriteRenderer> toeRenderers = new List<SpriteRenderer>();

    // Colors
    private Color bodyColor = new Color(85f / 255f, 145f / 255f, 127f / 255f); // RGB: 85, 145, 127
    private Color outlineColor = Color.black;
    private Color eyeColor = new Color(240f / 255f, 153f / 255f, 91f / 255f); // RGB: 240, 153, 91

    void Start()
    {
        frog = GetComponent<Frog>();

        // Setup body mesh rendering
        CreateBodyRenderer();

        // Setup head rendering
        CreateHeadRenderer();

        // Setup leg rendering
        CreateLegRenderers();
    }

    void Update()
    {
        // Update body mesh based on blob points
        UpdateBodyMesh();

        // Update head position and orientation
        UpdateHead();

        // Update leg rendering
        UpdateLegs();
    }

    private void CreateBodyRenderer()
    {
        GameObject bodyObject = new GameObject("FrogBody");
        bodyObject.transform.parent = transform;

        bodyMeshFilter = bodyObject.AddComponent<MeshFilter>();
        bodyMeshRenderer = bodyObject.AddComponent<MeshRenderer>();

        bodyMesh = new Mesh();
        bodyMeshFilter.mesh = bodyMesh;

        Material bodyMaterial = new Material(Shader.Find("Sprites/Default"));
        bodyMaterial.color = bodyColor;
        bodyMeshRenderer.material = bodyMaterial;

        // Adjust the scale if necessary
        bodyObject.transform.localScale = Vector3.one * 0.1f; // Scale down if needed
    }

    private void CreateHeadRenderer()
    {
        headObject = new GameObject("FrogHead");
        headObject.transform.parent = transform;

        headRenderer = headObject.AddComponent<SpriteRenderer>();
        // Create a simple circle sprite for head placeholder
        // In a full implementation, you'd create a proper head sprite
        headRenderer.sprite = CreateCircleSprite(100);
        headRenderer.color = bodyColor;

        // Add eyes
        GameObject leftEye = new GameObject("LeftEye");
        leftEye.transform.parent = headObject.transform;
        leftEyeRenderer = leftEye.AddComponent<SpriteRenderer>();
        leftEyeRenderer.sprite = CreateCircleSprite(24);
        leftEyeRenderer.color = eyeColor;
        leftEye.transform.localPosition = new Vector3(-75, -10, -1);

        GameObject rightEye = new GameObject("RightEye");
        rightEye.transform.parent = headObject.transform;
        rightEyeRenderer = rightEye.AddComponent<SpriteRenderer>();
        rightEyeRenderer.sprite = CreateCircleSprite(24);
        rightEyeRenderer.color = eyeColor;
        rightEye.transform.localPosition = new Vector3(75, -10, -1);
    }

    private void CreateLegRenderers()
    {
        // For each leg (4 total), create a renderer
        for (int i = 0; i < 4; i++)
        {
            // Leg renderer
            GameObject legObj = new GameObject($"Leg_{i}");
            legObj.transform.parent = transform;

            LineRenderer lr = legObj.AddComponent<LineRenderer>();
            lr.startWidth = 34f; // Match the original's 34 pixels width
            lr.endWidth = 34f;
            lr.positionCount = 3; // Anchor, elbow, foot

            Material legMaterial = new Material(Shader.Find("Sprites/Default"));
            legMaterial.color = bodyColor;
            lr.material = legMaterial;

            legRenderers.Add(lr);

            // Toe renderers (4 toes per foot)
            for (int j = 0; j < 4; j++)
            {
                GameObject toeObj = new GameObject($"Toe_{i}_{j}");
                toeObj.transform.parent = legObj.transform;

                SpriteRenderer sr = toeObj.AddComponent<SpriteRenderer>();
                sr.sprite = CreateEllipseSprite(8, 27.5f); // Half the original's 16x55 size
                sr.color = bodyColor;

                toeRenderers.Add(sr);
            }
        }
    }

    private void UpdateBodyMesh()
    {
        if (frog == null || frog.BlobPoints == null || frog.BlobPoints.Count < 3)
            return;

        List<Vector2> points = new List<Vector2>();
        foreach (var point in frog.BlobPoints)
        {
            points.Add(point.Position);
        }

        Vector3[] vertices = new Vector3[points.Count + 1];
        int[] triangles = new int[points.Count * 3];

        // Add center point as first vertex
        Vector2 center = Vector2.zero;
        foreach (var point in points)
        {
            center += point;
        }
        center /= points.Count;
        vertices[0] = new Vector3(center.x, center.y, 0);

        // Add the blob points as remaining vertices
        for (int i = 0; i < points.Count; i++)
        {
            vertices[i + 1] = new Vector3(points[i].x, points[i].y, 0);
        }

        // Create triangles
        for (int i = 0; i < points.Count; i++)
        {
            int nextIndex = (i + 1) % points.Count;
            triangles[i * 3] = 0; // Center point
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = nextIndex + 1;
        }

        // Update mesh
        bodyMesh.Clear();
        bodyMesh.vertices = vertices;
        bodyMesh.triangles = triangles;
        bodyMesh.RecalculateNormals();
    }

    private void UpdateHead()
    {
        if (frog == null || frog.BlobPoints == null || frog.BlobPoints.Count < 3)
            return;

        // Position the head at the top of the blob
        Vector2 top = frog.BlobPoints[0].Position;
        headObject.transform.position = new Vector3(top.x, top.y, 0);

        // Rotate the head based on the normal of the top point
        Vector2 prev = frog.BlobPoints[frog.BlobPoints.Count - 2].Position;
        Vector2 next = frog.BlobPoints[2].Position;
        Vector2 normal = (next - prev).normalized;
        float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
        headObject.transform.rotation = Quaternion.Euler(0, 0, angle + 90);
    }

    private void UpdateLegs()
    {
        if (frog == null || frog.BlobPoints == null || frog.BlobPoints.Count < 3)
            return;

        // Update front legs
        Vector2 leftFront = frog.BlobPoints[12].Position;
        Vector2 rightFront = frog.BlobPoints[4].Position;
        Vector2 leftFrontAnchor = Vector2.Lerp(leftFront, rightFront, 0.25f) + new Vector2(0, 10);
        Vector2 rightFrontAnchor = Vector2.Lerp(leftFront, rightFront, 0.75f) + new Vector2(0, 10);

        // Update hind legs
        Vector2 midSecant = (rightFront - leftFront).normalized * 64;
        Vector2 leftHindAnchor = frog.BlobPoints[11].Position + midSecant + new Vector2(0, 16);
        Vector2 rightHindAnchor = frog.BlobPoints[5].Position - midSecant + new Vector2(0, 16);

        // Update leg renderers
        UpdateLegRenderer(0, leftFrontAnchor, frog.LeftFrontLeg);
        UpdateLegRenderer(1, rightFrontAnchor, frog.RightFrontLeg);
        UpdateLegRenderer(2, leftHindAnchor, frog.LeftHindLeg);
        UpdateLegRenderer(3, rightHindAnchor, frog.RightHindLeg);
    }

    private void UpdateLegRenderer(int index, Vector2 anchor, Limb limb)
    {
        LineRenderer lr = legRenderers[index];
        lr.SetPosition(0, anchor);
        lr.SetPosition(1, limb.Elbow.Position);
        lr.SetPosition(2, limb.Foot.Position);

        // Update toes
        for (int i = 0; i < 4; i++)
        {
            SpriteRenderer sr = toeRenderers[index * 4 + i];
            float angle = (i - 1.5f) * Mathf.PI / 6; // Spread toes
            Vector2 toeOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 16;
            sr.transform.position = limb.Foot.Position + toeOffset;
            sr.transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
        }
    }

    private Sprite CreateCircleSprite(float radius)
    {
        int textureSize = Mathf.CeilToInt(radius * 2);
        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] pixels = new Color[textureSize * textureSize];

        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 point = new Vector2(x, y);
                if (Vector2.Distance(point, center) <= radius)
                {
                    pixels[y * textureSize + x] = Color.white;
                }
                else
                {
                    pixels[y * textureSize + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), Vector2.one * 0.5f);
    }

    private Sprite CreateEllipseSprite(float width, float height)
    {
        int textureWidth = Mathf.CeilToInt(width * 2);
        int textureHeight = Mathf.CeilToInt(height * 2);
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];

        Vector2 center = new Vector2(textureWidth / 2f, textureHeight / 2f);
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                Vector2 point = new Vector2(x, y);
                Vector2 normalizedPoint = (point - center) / new Vector2(width, height);
                if (normalizedPoint.sqrMagnitude <= 1)
                {
                    pixels[y * textureWidth + x] = Color.white;
                }
                else
                {
                    pixels[y * textureWidth + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, textureWidth, textureHeight), Vector2.one * 0.5f);
    }
}
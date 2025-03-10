using UnityEngine;
using System.Collections.Generic;

public class Blob
{
    public List<BlobPoint> Points = new List<BlobPoint>();

    public float Radius;
    public float Area;
    public float Circumference;
    public float ChordLength;
    public float dampness;
    public float grav;

    SoftBodySimulator sim;

    public Blob(Vector2 origin, int numPoints, float radius, float puffiness, float dampening, float gravity)
    {
        Radius = radius;
        Area = radius * radius * Mathf.PI * puffiness;
        Circumference = radius * 2 * Mathf.PI;
        ChordLength = Circumference / numPoints;
        dampness = dampening;
        grav = gravity;
        Points = new List<BlobPoint>();
        for (int i = 0; i < numPoints; i++)
        {
            float angle = 2 * Mathf.PI * i / numPoints - Mathf.PI / 2;
            Vector2 offset = new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );
            Points.Add(new BlobPoint(origin + offset));
        }
        sim = SoftBodySimulator.Instance;
    }

    public void Update(Vector2 mousePosition, bool isRightMousePressed, bool isRightMouseReleased, float screenWidth, float screenHeight)
    {
        // Compute one time step of physics with Verlet integration
        foreach (BlobPoint point in Points)
        {
            point.VerletIntegrate(dampness);
            point.ApplyGravity(grav);
        }

        // Iterate multiple times to converge faster
        for (int j = 0; j < 10; j++)
        {
            // Accumulate the displacement caused by distance constraints
            for (int i = 0; i < Points.Count; i++)
            {
                BlobPoint cur = Points[i];
                BlobPoint next = Points[i == Points.Count - 1 ? 0 : i + 1];

                Vector2 diff = next.Position - cur.Position;
                float distance = diff.magnitude;

                if (distance > ChordLength)
                {
                    float errorr = (distance - ChordLength) / 2f;
                    Vector2 offsett = diff.normalized * errorr;
                    cur.AccumulateDisplacement(offsett);
                    next.AccumulateDisplacement(-offsett);
                }
            }

            // Accumulate the displacement caused by dilation
            float curArea = GetArea();
            float error = Area - curArea;
            float offset = error / Circumference;

            for (int i = 0; i < Points.Count; i++)
            {
                BlobPoint prev = Points[i == 0 ? Points.Count - 1 : i - 1];
                BlobPoint cur = Points[i];
                BlobPoint next = Points[i == Points.Count - 1 ? 0 : i + 1];

                Vector2 secant = next.Position - prev.Position;
                Vector2 normal = new Vector2(-secant.y, secant.x).normalized * offset;


                // Limit the displacement to prevent instability
                 // Adjust this value as needed
                if (normal.magnitude > sim.maxDisplacement)
                {
                    normal = normal.normalized * sim.maxDisplacement;
                }

                cur.AccumulateDisplacement(normal);
            }

            // Apply all the accumulated displacement
            foreach (BlobPoint point in Points)
            {
                point.ApplyDisplacement();
            }
            //PreventTwisting(); //cursed do not uncomment please
            // Handle mouse interaction for dragging and throwing
            foreach (BlobPoint point in Points)
            {
                point.HandleMouseInteraction(mousePosition, 1f, isRightMousePressed, isRightMouseReleased);
                point.KeepInBounds(screenWidth, screenHeight);
            }
        }
    }

    // Get the area of the blob using the trapezoid method
    public float GetArea()
    {
        float area = 0;
        for (int i = 0; i < Points.Count; i++)
        {
            Vector2 cur = Points[i].Position;
            Vector2 next = Points[i == Points.Count - 1 ? 0 : i + 1].Position;
            area += ((cur.x - next.x) * (cur.y + next.y) / 2);
        }
        return Mathf.Abs(area); // Make sure we return a positive area
    }

    private void PreventTwisting()
    {
        for (int i = 0; i < Points.Count; i++)
        {
            BlobPoint prev = Points[i == 0 ? Points.Count - 1 : i - 1];
            BlobPoint cur = Points[i];
            BlobPoint next = Points[i == Points.Count - 1 ? 0 : i + 1];

            // Calculate the cross product to detect twisting
            Vector2 prevToCur = cur.Position - prev.Position;
            Vector2 curToNext = next.Position - cur.Position;
            float cross = prevToCur.x * curToNext.y - prevToCur.y * curToNext.x;

            // If the cross product is negative, the points are twisting
            if (cross < 0)
            {
                // Apply a small correction to prevent twisting
                Vector2 correction = (prevToCur + curToNext).normalized * 0.1f; // Adjust the correction strength
                cur.AccumulateDisplacement(correction);
            }
        }
    }
}
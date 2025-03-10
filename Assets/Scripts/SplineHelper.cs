using UnityEngine;

public static class SplineHelper
{
    // Catmull-Rom spline interpolation
    public static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        // Calculate the Catmull-Rom spline point
        float t2 = t * t;
        float t3 = t2 * t;

        Vector2 result =
            0.5f * ((2 * p1) +
                    (-p0 + p2) * t +
                    (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 +
                    (-p0 + 3 * p1 - 3 * p2 + p3) * t3);

        return result;
    }
}
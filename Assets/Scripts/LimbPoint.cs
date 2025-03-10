using UnityEngine;

public class LimbPoint
{
    public Vector2 Position;
    public Vector2 PreviousPosition;
    public float Angle;

    public LimbPoint(Vector2 position)
    {
        this.Position = position;
        PreviousPosition = position;
        Angle = 0;
    }

    public void VerletIntegrate(float dampingFactor = 0.95f)
    {
        Vector2 temp = Position;
        Vector2 velocity = (Position - PreviousPosition) * dampingFactor;
        Position += velocity;
        PreviousPosition = temp;
    }

    // distance: distance between anchor and the point
    // angleRange: range of motion, how far the point can be from pointing toward normal
    // angleOffset: rotates the entire range of motion
    public void ApplyConstraint(Vector2 anchor, float normal, float distance, float angleRange, float angleOffset)
    {
        float anchorAngle = normal + angleOffset;
        float curAngle = Mathf.Atan2(anchor.y - Position.y, anchor.x - Position.x);
        Angle = ConstrainAngle(curAngle, anchorAngle, angleRange);

        // Convert angle to direction vector, scale by distance, and position relative to anchor
        float x = Mathf.Cos(Angle);
        float y = Mathf.Sin(Angle);
        Vector2 direction = new Vector2(x, y);
        Position = anchor - direction * distance;
    }

    public void ApplyGravity(float gravityForce = 1f)
    {
        Position += new Vector2(0, -gravityForce); // Note: using negative Y as gravity in Unity
    }

    public void KeepInBounds(float screenWidth, float screenHeight)
    {
        Position.x = Mathf.Clamp(Position.x, 0, screenWidth);
        Position.y = Mathf.Clamp(Position.y, 0, screenHeight);
    }

    // Constrain the angle to be within a certain range of the anchor
    private float ConstrainAngle(float angle, float anchor, float constraint)
    {
        if (Mathf.Abs(RelativeAngleDiff(angle, anchor)) <= constraint)
        {
            return SimplifyAngle(angle);
        }

        if (RelativeAngleDiff(angle, anchor) > constraint)
        {
            return SimplifyAngle(anchor - constraint);
        }

        return SimplifyAngle(anchor + constraint);
    }

    // i.e. How many radians do you need to turn the angle to match the anchor?
    private float RelativeAngleDiff(float angle, float anchor)
    {
        // Since angles are represented by values in [0, 2PI), it's helpful to rotate
        // the coordinate space such that PI is at the anchor. That way we don't have
        // to worry about the "seam" between 0 and 2PI.
        angle = SimplifyAngle(angle + Mathf.PI - anchor);
        anchor = Mathf.PI;
        return anchor - angle;
    }

    // Simplify the angle to be in the range [0, 2PI)
    private float SimplifyAngle(float angle)
    {
        while (angle >= 2 * Mathf.PI)
        {
            angle -= 2 * Mathf.PI;
        }
        while (angle < 0)
        {
            angle += 2 * Mathf.PI;
        }
        return angle;
    }
}
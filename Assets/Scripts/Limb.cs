using UnityEngine;

public class Limb
{
    public LimbPoint Elbow;
    public LimbPoint Foot;

    public float Distance; // between joints
    public float ElbowRange;
    public float ElbowOffset;
    public float FootRange;
    public float FootOffset;

    public Limb(Vector2 origin, float distance, float elbowRange, float elbowOffset, float footRange, float footOffset)
    {
        this.Distance = distance;
        this.ElbowRange = elbowRange;
        this.ElbowOffset = elbowOffset;
        this.FootRange = footRange;
        this.FootOffset = footOffset;

        Elbow = new LimbPoint(origin + new Vector2(0, -distance)); // Note: Y is inverted in Unity
        Foot = new LimbPoint(Elbow.Position + new Vector2(0, -distance));
    }

    public void Resolve(Vector2 anchor, float normal, float screenWidth, float screenHeight)
    {
        Elbow.VerletIntegrate();
        Elbow.ApplyGravity();
        Elbow.ApplyConstraint(anchor, normal, Distance, ElbowRange, ElbowOffset);
        Elbow.KeepInBounds(screenWidth, screenHeight);

        Foot.VerletIntegrate();
        Foot.ApplyGravity();
        Foot.ApplyConstraint(Elbow.Position, Elbow.Angle, Distance, FootRange, FootOffset);
        Foot.KeepInBounds(screenWidth, screenHeight);
    }
}
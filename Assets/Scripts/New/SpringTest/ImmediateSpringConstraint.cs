using SoftBodyPhysics;
using UnityEngine;

public class ImmediateSpringConstraint : IConstraint
{
    private IPointMass pointA;
    private IPointMass pointB;
    private float restDistance;
    private float stiffness;
    private float damping;

    public bool IsActive { get; set; } = true;

    public ImmediateSpringConstraint(IPointMass pointA, IPointMass pointB, float restDistance,
                                     float stiffness = 0.5f, float damping = 0.1f)
    {
        this.pointA = pointA;
        this.pointB = pointB;
        this.restDistance = restDistance;
        this.stiffness = Mathf.Clamp01(stiffness);
        this.damping = Mathf.Clamp01(damping);
    }

    public void Solve()
    {
        if (!IsActive) return;

        Vector2 delta = pointB.Position - pointA.Position;
        float currentDistance = delta.magnitude;

        // Skip if points are at the same position
        if (currentDistance < 0.0001f) return;

        // Calculate the spring force
        float springForce = (currentDistance - restDistance) * stiffness;

        // Add damping based on relative velocity
        Vector2 relativeVelocity = pointB.Velocity - pointA.Velocity;
        float dampingForce = Vector2.Dot(relativeVelocity, delta.normalized) * damping;

        // Total force 
        float totalForce = springForce + dampingForce;

        // Instead of just accumulating forces, directly adjust positions proportionally
        // This is a hybrid approach between position-based dynamics and force-based
        float adjustmentFactor = totalForce * 0.1f / currentDistance;

        // Calculate movement amounts based on mass
        float totalMass = pointA.Mass + pointB.Mass;
        float pointAFactor = pointA.IsFixed ? 0 : pointB.Mass / totalMass;
        float pointBFactor = pointB.IsFixed ? 0 : pointA.Mass / totalMass;

        // Apply immediate position correction
        if (!pointA.IsFixed)
            pointA.Position += delta * adjustmentFactor * pointAFactor;

        if (!pointB.IsFixed)
            pointB.Position -= delta * adjustmentFactor * pointBFactor;
    }
}
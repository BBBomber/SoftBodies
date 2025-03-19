using System.Collections.Generic;
using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Interface for any object that can be simulated in the soft body physics system
    /// </summary>
    public interface ISoftBodyObject
    {
        void UpdatePhysics(float deltaTime);
        Vector2 GetCenter();
        Bounds GetBounds();
        List<IPointMass> GetPoints();
        bool IsCollidable { get; }
        int CollisionLayer { get; }
        void HandleCollision(ISoftBodyObject other);
    }

    /// <summary>
    /// Interface for point masses that make up soft body objects
    /// </summary>
    public interface IPointMass
    {
        Vector2 Position { get; set; }
        Vector2 PreviousPosition { get; set; }
        Vector2 Velocity { get; }
        float Mass { get; set; }
        bool IsFixed { get; set; }
        void ApplyForce(Vector2 force);
        void ApplyDisplacement(Vector2 displacement);
        void VerletIntegrate(float deltaTime, float dampingFactor);
    }

    /// <summary>
    /// Interface for constraining two point masses
    /// </summary>
    public interface IConstraint
    {
        void Solve();
        bool IsActive { get; set; }
    }

    /// <summary>
    /// Interface for any joint that connects soft body objects
    /// </summary>
    public interface IJoint
    {
        ISoftBodyObject ObjectA { get; }
        ISoftBodyObject ObjectB { get; }
        void UpdateJoint();
        bool IsActive { get; set; }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace SoftBodyPhysics
{
    /// <summary>
    /// Manages collisions between soft body objects
    /// </summary>
    public class CollisionManager
    {
        private int subdivisions;
        private Dictionary<int, HashSet<int>> layerCollisionMatrix = new Dictionary<int, HashSet<int>>();

        public CollisionManager(int subdivisions)
        {
            this.subdivisions = subdivisions;
            InitializeDefaultLayerMatrix();
        }

        private void InitializeDefaultLayerMatrix()
        {
            // By default, all layers collide with each other
            for (int i = 0; i < 32; i++)
            {
                HashSet<int> collidableLayers = new HashSet<int>();
                for (int j = 0; j < 32; j++)
                {
                    collidableLayers.Add(j);
                }
                layerCollisionMatrix[i] = collidableLayers;
            }
        }

        public void SetLayerCollision(int layer1, int layer2, bool shouldCollide)
        {
            if (!layerCollisionMatrix.ContainsKey(layer1))
            {
                layerCollisionMatrix[layer1] = new HashSet<int>();
            }

            if (!layerCollisionMatrix.ContainsKey(layer2))
            {
                layerCollisionMatrix[layer2] = new HashSet<int>();
            }

            if (shouldCollide)
            {
                layerCollisionMatrix[layer1].Add(layer2);
                layerCollisionMatrix[layer2].Add(layer1);
            }
            else
            {
                layerCollisionMatrix[layer1].Remove(layer2);
                layerCollisionMatrix[layer2].Remove(layer1);
            }
        }

        public void DetectAndResolveCollisions(List<ISoftBodyObject> softBodies)
        {
            // For now, we'll use a simple O(n²) approach to check all pairs
            // Later we can implement spatial hashing for better performance

            for (int i = 0; i < softBodies.Count; i++)
            {
                ISoftBodyObject bodyA = softBodies[i];
                if (!bodyA.IsCollidable) continue;

                for (int j = i + 1; j < softBodies.Count; j++)
                {
                    ISoftBodyObject bodyB = softBodies[j];
                    if (!bodyB.IsCollidable) continue;

                    // Skip if they're on different collision layers that don't interact
                    if (!LayersMayCollide(bodyA.CollisionLayer, bodyB.CollisionLayer))
                        continue;

                    // Quick bounds check before detailed collision
                    if (BoundsOverlap(bodyA.GetBounds(), bodyB.GetBounds()))
                    {
                        // Let each body handle its response to the collision
                        bodyA.HandleCollision(bodyB);
                        bodyB.HandleCollision(bodyA);
                    }
                }
            }
        }

        private bool BoundsOverlap(Bounds a, Bounds b)
        {
            return a.Intersects(b);
        }

        private bool LayersMayCollide(int layerA, int layerB)
        {
            // Check if layers are set to collide with each other
            if (layerCollisionMatrix.TryGetValue(layerA, out HashSet<int> collidableLayers))
            {
                return collidableLayers.Contains(layerB);
            }

            // Default to true if layer isn't in the matrix
            return true;
        }

        // Advanced collision detection methods can be added later:
        // - Point-in-polygon tests for detecting overlaps
        // - Spatial partitioning for optimization
        // - Continuous collision detection for fast-moving objects
    }
}
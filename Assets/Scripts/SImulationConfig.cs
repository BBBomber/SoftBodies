using UnityEngine;

[CreateAssetMenu(fileName = "SimulationConfig", menuName = "SoftBody/SimulationConfig", order = 1)]
public class SimulationConfig : ScriptableObject
{
    [Header("Blob Settings")]
    public int numPoints = 16; // Number of points in the blob
    public float blobRadius = 30f; // Radius of the blob
    public float puffiness = 1.5f; // Puffiness factor for the blob

    [Header("Limb Settings")]
    public float frontLegDistance = 20f; // Distance between joints in front legs
    public float hindLegDistance = 30f; // Distance between joints in hind legs
    public float elbowRange = Mathf.PI / 4; // Range of motion for elbows
    public float footRange = Mathf.PI / 5; // Range of motion for feet

    [Header("Physics Settings")]
    public float gravityForce = 1f; // Gravity applied to points
    public float dampingFactor = 0.99f; // Damping factor for Verlet integration
    public float collisionRadius = 100f; // Radius for mouse collision

    [Header("Rendering Settings")]
    public Color bodyColor = new Color(85f / 255f, 145f / 255f, 127f / 255f); // Body color
    public Color outlineColor = Color.black; // Outline color
    public Color eyeColor = new Color(240f / 255f, 153f / 255f, 91f / 255f); // Eye color
}
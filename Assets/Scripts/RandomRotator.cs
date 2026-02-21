using UnityEngine;

public class RandomRotator : MonoBehaviour
{
    [Header("Rotation Speed Settings")]
    [Tooltip("Minimum possible rotation speed (degrees per second)")]
    public float minSpeed = 50f;
    
    [Tooltip("Maximum possible rotation speed (degrees per second)")]
    public float maxSpeed = 150f;

    private Vector3 rotationAxis;
    private float currentSpeed;

    void Start()
    {
        // 1. Pick a completely random 3D direction for the axis
        rotationAxis = Random.onUnitSphere;

        // 2. Pick a random speed between your min and max
        currentSpeed = Random.Range(minSpeed, maxSpeed);
    }

    void Update()
    {
        // 3. Rotate the object around the random axis at the random speed
        // Space.Self means it rotates relative to its own local axes. 
        // You can change it to Space.World if you want global rotation.
        transform.Rotate(rotationAxis, currentSpeed * Time.deltaTime, Space.Self);
    }

    /// <summary>
    /// Optional: Call this method from another script if you want 
    /// the object to suddenly pick a new random direction and speed!
    /// </summary>
    public void RandomizeAgain()
    {
        rotationAxis = Random.onUnitSphere;
        currentSpeed = Random.Range(minSpeed, maxSpeed);
    }
}
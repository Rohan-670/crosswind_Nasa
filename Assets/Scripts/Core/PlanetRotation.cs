using UnityEngine;

/// <summary>
/// Slowly rotates a planet around its Y-axis to simulate planetary spin.
/// Attach this to any sphere GameObject (like Earth) in the Inspector.
/// </summary>
public class PlanetRotation : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second. Positive = counterclockwise from above.")]
    public float rotationSpeed = 5f;

    void Update()
    {
        // Rotate around the Y-axis (up) at a steady speed, independent of frame rate
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
    }
}

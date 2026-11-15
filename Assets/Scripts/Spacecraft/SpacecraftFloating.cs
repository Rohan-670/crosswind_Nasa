using UnityEngine;

/// <summary>
/// Adds subtle microgravity drift and idle thruster stabilization to the spacecraft.
/// Keeps the ship feeling alive in low Earth orbit without erratic movement.
/// </summary>
public class SpacecraftFloating : MonoBehaviour
{
    [Tooltip("Speed of gentle bobbing and orientation sway")]
    public float floatSpeed = 0.5f;

    [Tooltip("Distance of positional micro-drift")]
    public float floatAmount = 0.15f;

    [Tooltip("Degrees of rotational sway")]
    public float pitchAmount = 1.2f;
    public float yawAmount = 0.8f;
    public float rollAmount = 1.5f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    void Update()
    {
        float t = Time.time * floatSpeed;

        // Subtle position oscillation
        float posY = Mathf.Sin(t) * floatAmount;
        float posX = Mathf.Cos(t * 0.7f) * (floatAmount * 0.5f);
        transform.position = initialPosition + new Vector3(posX, posY, 0f);

        // Gentle rotational breathing / stabilization
        float pitch = Mathf.Sin(t * 0.8f) * pitchAmount;
        float yaw = Mathf.Cos(t * 0.6f) * yawAmount;
        float roll = Mathf.Sin(t * 1.1f) * rollAmount;

        transform.rotation = initialRotation * Quaternion.Euler(pitch, yaw, roll);
    }
}

using UnityEngine;

/// <summary>
/// Procedural idle motion for the 3D astronaut model:
/// provides subtle breathing expansion, gentle stance sway, and head stabilization.
/// </summary>
public class AstronautIdling : MonoBehaviour
{
    [Header("--- Breathing Motion ---")]
    public float breathSpeed = 1.2f;
    public float breathAmount = 0.015f;

    [Header("--- Subtle Stance Sway ---")]
    public float swaySpeed = 0.6f;
    public float swayAmount = 0.8f;

    private Vector3 initialScale;
    private Quaternion initialRotation;

    void Start()
    {
        initialScale = transform.localScale;
        initialRotation = transform.localRotation;
    }

    void Update()
    {
        float t = Time.time;

        // Subtle chest breathing
        float breath = Mathf.Sin(t * breathSpeed) * breathAmount;
        transform.localScale = initialScale + new Vector3(breath * 0.5f, breath, breath * 0.5f);

        // Gentle postural sway
        float swayY = Mathf.Sin(t * swaySpeed) * swayAmount;
        float swayZ = Mathf.Cos(t * swaySpeed * 0.8f) * (swayAmount * 0.4f);
        transform.localRotation = initialRotation * Quaternion.Euler(0f, swayY, swayZ);
    }
}

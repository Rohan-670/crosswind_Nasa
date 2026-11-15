using UnityEngine;

/// <summary>
/// Controls a smooth, cinematic camera orbit around Earth while keeping
/// the spacecraft in the midground and Earth majestically in frame.
/// </summary>
public class CinematicCameraController : MonoBehaviour
{
    [Header("--- Tracking Targets ---")]
    [Tooltip("Target for Earth")]
    public Transform earthTarget;

    [Tooltip("Target for the Spacecraft")]
    public Transform spacecraftTarget;

    [Header("--- Orbit Parameters ---")]
    [Tooltip("Base position for camera if targets are not set")]
    public Vector3 orbitCenter = new Vector3(25f, -8f, 80f);

    [Tooltip("Distance from the orbit center")]
    public float orbitDistance = 88f;

    [Tooltip("Orbit speed in degrees per second")]
    public float orbitSpeed = 0.75f;

    [Tooltip("Starting horizontal angle in degrees")]
    public float startAzimuth = 196f;

    [Tooltip("Camera elevation angle in degrees above equator")]
    public float elevation = 6.5f;

    [Tooltip("Gentle vertical sine breathing speed")]
    public float bobSpeed = 0.2f;

    [Tooltip("Gentle vertical sine breathing distance")]
    public float bobAmount = 0.6f;

    [Header("--- Framing & Smoothing ---")]
    [Tooltip("How smoothly the camera aims at the focal point (lower = smoother)")]
    public float lookSmoothness = 2.5f;

    [Tooltip("Focal point blend: 0 = Earth center, 1 = Spacecraft center, 0.25 = ideal cinematic blend")]
    [Range(0f, 1f)]
    public float focalBlend = 0.28f;

    private float currentAzimuth;
    private Quaternion currentLookRotation;

    void Start()
    {
        currentAzimuth = startAzimuth;
        currentLookRotation = transform.rotation;
    }

    void LateUpdate()
    {
        // Advance orbit angle continuously
        currentAzimuth += orbitSpeed * Time.deltaTime;
        float radAzimuth = currentAzimuth * Mathf.Deg2Rad;
        float radElev = elevation * Mathf.Deg2Rad;

        Vector3 center = earthTarget != null ? earthTarget.position : orbitCenter;

        // Spherical orbital position around Earth
        float x = center.x + orbitDistance * Mathf.Cos(radElev) * Mathf.Sin(radAzimuth);
        float y = center.y + orbitDistance * Mathf.Sin(radElev) + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        float z = center.z + orbitDistance * Mathf.Cos(radElev) * Mathf.Cos(radAzimuth);

        transform.position = new Vector3(x, y, z);

        // Frame both Earth and the Spacecraft
        Vector3 targetFocus;
        if (earthTarget != null && spacecraftTarget != null)
        {
            targetFocus = Vector3.Lerp(earthTarget.position, spacecraftTarget.position, focalBlend);
        }
        else if (spacecraftTarget != null)
        {
            targetFocus = spacecraftTarget.position;
        }
        else if (earthTarget != null)
        {
            targetFocus = earthTarget.position;
        }
        else
        {
            targetFocus = new Vector3(8f, 0f, 30f);
        }

        Vector3 direction = targetFocus - transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(direction);
            currentLookRotation = Quaternion.Slerp(currentLookRotation, desiredRotation, Time.deltaTime * lookSmoothness);
            transform.rotation = currentLookRotation;
        }
    }
}

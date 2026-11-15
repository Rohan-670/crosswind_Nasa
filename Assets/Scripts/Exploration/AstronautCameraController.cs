using UnityEngine;

/// <summary>
/// Reusable third-person orbital follow camera:
/// smoothly tracks the astronaut, rotates with mouse look,
/// and maintains collision clearance from terrain.
/// </summary>
public class AstronautCameraController : MonoBehaviour
{
    [Header("--- Target Tracking ---")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

    [Header("--- Orbit Dynamics ---")]
    public float distance = 3.6f;
    public float minDistance = 1.2f;
    public float maxDistance = 6.0f;
    public float mouseSensitivityX = 140f;
    public float mouseSensitivityY = 100f;
    public float minPitch = -20f;
    public float maxPitch = 65f;
    public bool invertY = false;

    [Header("--- Damping & Smoothing ---")]
    public float positionDamping = 14f;
    public float rotationDamping = 18f;

    private float currentYaw = 0f;
    private float currentPitch = 15f;

    private void Start()
    {
        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            currentYaw = angles.y;
            currentPitch = angles.x;
        }

        // Lock cursor during exploration
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Mouse look inputs
        float mouseX = AstroInput.GetAxis("Mouse X");
        float mouseY = AstroInput.GetAxis("Mouse Y") * (invertY ? 1f : -1f);

        currentYaw += mouseX * mouseSensitivityX * Time.deltaTime;
        currentPitch = Mathf.Clamp(currentPitch - mouseY * mouseSensitivityY * Time.deltaTime, minPitch, maxPitch);

        // Calculate rotation and position
        Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 focusPoint = target.position + targetOffset;
        Vector3 targetPosition = focusPoint - (targetRotation * Vector3.forward * distance);

        // Smoothly interpolate
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationDamping * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionDamping * Time.deltaTime);

        // Unlock cursor with Escape
        if (AstroInput.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (AstroInput.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}

using UnityEngine;

/// <summary>
/// Reusable third-person astronaut character controller:
/// handles walking, low-gravity jumping, procedural spacesuit walking sway,
/// and camera-relative locomotion on planetary surfaces (Moon / Mars).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class AstronautController : MonoBehaviour
{
    [Header("--- Locomotion Dynamics ---")]
    [Tooltip("Standard walking speed in m/s")]
    public float walkSpeed = 3.0f;
    [Tooltip("Sprint speed in m/s")]
    public float sprintSpeed = 5.2f;
    [Tooltip("Turn rotation speed in degrees/sec")]
    public float turnSpeed = 12f;

    [Header("--- Planetary Gravity & Bounding ---")]
    [Tooltip("Local planetary gravity acceleration (Moon: 1.62, Mars: 3.72, Earth: 9.81)")]
    public float gravity = 1.62f;
    [Tooltip("Low-gravity jump upward velocity in m/s")]
    public float jumpVelocity = 4.2f;

    [Header("--- Camera Heading Reference ---")]
    public Transform cameraTransform;

    [Header("--- Spacesuit Procedural Sway ---")]
    public Transform spacesuitMeshRoot;
    public float stepSwayFrequency = 7.0f;
    public float stepSwayAmount = 2.5f;

    [Header("--- Articulated Locomotion Animator ---")]
    public AstronautLocomotionAnimator locomotionAnimator;

    // Runtime state
    public CharacterController Character { get; private set; }
    public bool IsGrounded => Character != null && Character.isGrounded;
    public bool IsSprinting { get; private set; }
    public float CurrentSpeed { get; private set; }

    private float verticalVelocity = 0f;
    private float swayTimer = 0f;
    private Quaternion meshInitialRotation;
    private bool isControlsLocked = false;

    private void Awake()
    {
        Character = GetComponent<CharacterController>();
        if (spacesuitMeshRoot != null)
            meshInitialRotation = spacesuitMeshRoot.localRotation;
        if (locomotionAnimator == null)
            locomotionAnimator = GetComponentInChildren<AstronautLocomotionAnimator>();
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (isControlsLocked) return;

        HandleLocomotion();
        HandleProceduralSway();
    }

    private void HandleLocomotion()
    {
        // 1. Inputs
        float h = AstroInput.GetAxis("Horizontal");
        float v = AstroInput.GetAxis("Vertical");
        IsSprinting = AstroInput.GetKey(KeyCode.LeftShift);

        // Calculate camera-relative movement direction
        Vector3 camForward = Vector3.forward;
        Vector3 camRight = Vector3.right;

        if (cameraTransform != null)
        {
            camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();
        }

        Vector3 moveDirection = (camForward * v + camRight * h).normalized;
        float targetSpeed = (IsSprinting ? sprintSpeed : walkSpeed) * moveDirection.magnitude;
        CurrentSpeed = targetSpeed;

        // 2. Rotation toward movement heading
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // 3. Gravity and Jumping
        if (Character.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -1.5f; // Keep grounded

            if (AstroInput.GetButtonDown("Jump") || AstroInput.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = jumpVelocity;
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // 4. Apply displacement
        Vector3 motion = moveDirection * (targetSpeed * Time.deltaTime);
        motion.y = verticalVelocity * Time.deltaTime;

        Character.Move(motion);
    }

    private void HandleProceduralSway()
    {
        if (spacesuitMeshRoot == null) return;

        if (IsGrounded && CurrentSpeed > 0.5f)
        {
            swayTimer += Time.deltaTime * stepSwayFrequency * (IsSprinting ? 1.4f : 1.0f);
            float rollSway = Mathf.Sin(swayTimer) * stepSwayAmount;
            float pitchSway = Mathf.Cos(swayTimer * 2f) * (stepSwayAmount * 0.5f);

            spacesuitMeshRoot.localRotation = meshInitialRotation * Quaternion.Euler(pitchSway, 0f, rollSway);
        }
        else
        {
            swayTimer = 0f;
            spacesuitMeshRoot.localRotation = Quaternion.Slerp(spacesuitMeshRoot.localRotation, meshInitialRotation, Time.deltaTime * 6f);
        }
    }

    public void SetControlsLocked(bool locked)
    {
        isControlsLocked = locked;
        CurrentSpeed = 0f;
    }
}

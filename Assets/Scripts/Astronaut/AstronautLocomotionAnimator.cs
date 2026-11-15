using UnityEngine;

/// <summary>
/// Authentic procedural locomotion animator for the NASA EMU spacesuit.
/// Provides dynamic articulated limb kinematics for walking, sprinting (lunar lope),
/// low-gravity jumping, and idle breathing without requiring external animation clips or skeletal rigs.
/// Operates seamlessly at 60+ FPS on WebGL and Windows.
/// </summary>
public class AstronautLocomotionAnimator : MonoBehaviour
{
    [Header("--- Articulated Joint References ---")]
    [Tooltip("Torso and helmet transform (root body mass)")]
    public Transform torsoTransform;

    [Tooltip("Left hip pivot GameObject (rotates left leg)")]
    public Transform leftLegPivot;

    [Tooltip("Right hip pivot GameObject (rotates right leg)")]
    public Transform rightLegPivot;

    [Tooltip("Left shoulder pivot GameObject (rotates left arm)")]
    public Transform leftArmPivot;

    [Tooltip("Right shoulder pivot GameObject (rotates right arm)")]
    public Transform rightArmPivot;

    [Header("--- Locomotion Kinematics ---")]
    [Tooltip("Cadence frequency during standard walk (rad/s)")]
    public float walkStrideRate = 6.2f;

    [Tooltip("Cadence frequency during sprint / low-G lope (rad/s)")]
    public float sprintStrideRate = 9.2f;

    [Tooltip("Leg swing pitch angle during walk (degrees)")]
    public float walkLegAngle = 22.0f;

    [Tooltip("Leg swing pitch angle during sprint (degrees)")]
    public float sprintLegAngle = 34.0f;

    [Tooltip("Arm swing pitch angle during walk (degrees)")]
    public float walkArmAngle = 18.0f;

    [Tooltip("Arm swing pitch angle during sprint (degrees)")]
    public float sprintArmAngle = 28.0f;

    [Header("--- Torso Dynamics ---")]
    [Tooltip("Vertical bounce bob height during walk (meters)")]
    public float walkTorsoBob = 0.020f;

    [Tooltip("Vertical bounce bob height during sprint (meters)")]
    public float sprintTorsoBob = 0.040f;

    [Tooltip("Forward torso tilt when moving (degrees)")]
    public float forwardTorsoLean = 3.0f;

    [Tooltip("Side-to-side torso roll during strides (degrees)")]
    public float torsoSwayRoll = 1.8f;

    [Header("--- Low-Gravity Airborne Pose ---")]
    [Tooltip("Leg trailing pitch during low-gravity jumps (degrees)")]
    public float jumpLegTrailAngle = -12.0f;

    [Tooltip("Arm stabilizing flare during low-gravity jumps (degrees)")]
    public float jumpArmFlareAngle = 15.0f;

    [Header("--- Idle Breathing & Smoothing ---")]
    public float idleBreathingSpeed = 1.1f;
    public float idleBreathingBob = 0.004f;
    public float smoothingSpeed = 12.0f;

    // Optional linked character controller
    [Header("--- Controller Reference ---")]
    public AstronautController astronautController;

    // Internal kinematic state
    private float strideCycle = 0f;
    private float currentSpeedSmooth = 0f;
    private float airTimeSmooth = 0f;

    // Resting transforms
    private Vector3 torsoInitialLocalPos;
    private Quaternion torsoInitialLocalRot = Quaternion.identity;
    private Quaternion leftLegInitialLocalRot = Quaternion.identity;
    private Quaternion rightLegInitialLocalRot = Quaternion.identity;
    private Quaternion leftArmInitialLocalRot = Quaternion.identity;
    private Quaternion rightArmInitialLocalRot = Quaternion.identity;

    private void Awake()
    {
        CacheInitialTransforms();
    }

    private void Start()
    {
        if (astronautController == null)
        {
            astronautController = GetComponentInParent<AstronautController>();
        }

        CacheInitialTransforms();
    }

    public void CacheInitialTransforms()
    {
        if (torsoTransform != null)
        {
            torsoInitialLocalPos = torsoTransform.localPosition;
            torsoInitialLocalRot = torsoTransform.localRotation;
        }

        if (leftLegPivot != null) leftLegInitialLocalRot = leftLegPivot.localRotation;
        if (rightLegPivot != null) rightLegInitialLocalRot = rightLegPivot.localRotation;
        if (leftArmPivot != null) leftArmInitialLocalRot = leftArmPivot.localRotation;
        if (rightArmPivot != null) rightArmInitialLocalRot = rightArmPivot.localRotation;
    }

    private void Update()
    {
        // 1. Gather motion metrics
        float targetSpeed = 0f;
        bool isSprinting = false;
        bool isGrounded = true;

        if (astronautController != null)
        {
            targetSpeed = astronautController.CurrentSpeed;
            isSprinting = astronautController.IsSprinting;
            isGrounded = astronautController.IsGrounded;
        }

        currentSpeedSmooth = Mathf.Lerp(currentSpeedSmooth, targetSpeed, Time.deltaTime * 10f);
        airTimeSmooth = Mathf.Lerp(airTimeSmooth, isGrounded ? 0f : 1f, Time.deltaTime * 8f);

        // Normalize speed into 0..1 locomotion weight
        float maxSpeed = isSprinting ? 5.4f : 3.2f;
        float moveWeight = Mathf.Clamp01(currentSpeedSmooth / Mathf.Max(0.1f, maxSpeed));

        // 2. Advance stride cycle if moving on ground
        if (isGrounded && moveWeight > 0.05f)
        {
            float rate = Mathf.Lerp(walkStrideRate, sprintStrideRate, isSprinting ? 1f : 0f);
            strideCycle += Time.deltaTime * rate * moveWeight;
            if (strideCycle > Mathf.PI * 2f) strideCycle -= Mathf.PI * 2f;
        }
        else if (isGrounded)
        {
            strideCycle = Mathf.Lerp(strideCycle, 0f, Time.deltaTime * 6f);
        }

        float cycleSin = Mathf.Sin(strideCycle);
        float doubleSin = Mathf.Sin(strideCycle * 2f);

        // 3. Compute target rotations for limbs
        ApplyLegKinematics(cycleSin, moveWeight, isSprinting, airTimeSmooth);
        ApplyArmKinematics(cycleSin, moveWeight, isSprinting, airTimeSmooth);
        ApplyTorsoKinematics(cycleSin, doubleSin, moveWeight, isSprinting, airTimeSmooth);
    }

    private void ApplyLegKinematics(float cycleSin, float moveWeight, bool isSprinting, float airFactor)
    {
        if (leftLegPivot == null || rightLegPivot == null) return;

        float maxLegAngle = isSprinting ? sprintLegAngle : walkLegAngle;

        // Ground swing: Left swings forward (+), Right swings backward (-)
        float leftPitch = cycleSin * maxLegAngle * moveWeight;
        float rightPitch = -cycleSin * maxLegAngle * moveWeight;

        // Low-gravity airborne pose: both legs trail slightly backward
        leftPitch = Mathf.Lerp(leftPitch, jumpLegTrailAngle, airFactor);
        rightPitch = Mathf.Lerp(rightPitch, jumpLegTrailAngle * 0.85f, airFactor);

        // Subtle lateral hip sway during strides
        float leftRoll = cycleSin * 1.5f * moveWeight;
        float rightRoll = -cycleSin * 1.5f * moveWeight;

        Quaternion targetLeftRot = leftLegInitialLocalRot * Quaternion.Euler(leftPitch, 0f, leftRoll);
        Quaternion targetRightRot = rightLegInitialLocalRot * Quaternion.Euler(rightPitch, 0f, rightRoll);

        leftLegPivot.localRotation = Quaternion.Slerp(leftLegPivot.localRotation, targetLeftRot, Time.deltaTime * smoothingSpeed);
        rightLegPivot.localRotation = Quaternion.Slerp(rightLegPivot.localRotation, targetRightRot, Time.deltaTime * smoothingSpeed);
    }

    private void ApplyArmKinematics(float cycleSin, float moveWeight, bool isSprinting, float airFactor)
    {
        if (leftArmPivot == null || rightArmPivot == null) return;

        float maxArmAngle = isSprinting ? sprintArmAngle : walkArmAngle;

        // Counter-swing opposite to legs:
        // Left leg forward (+) -> Left arm backward (-)
        // Right leg forward (+) -> Right arm backward (-)
        float leftPitch = -cycleSin * maxArmAngle * moveWeight;
        float rightPitch = cycleSin * maxArmAngle * moveWeight;

        // Outward clearance during active swing (0 at rest)
        float leftRoll = -Mathf.Abs(cycleSin) * 2.5f * moveWeight;
        float rightRoll = Mathf.Abs(cycleSin) * 2.5f * moveWeight;

        // Low-gravity jump flare: arms float slightly out/back
        leftPitch = Mathf.Lerp(leftPitch, -6.0f, airFactor);
        rightPitch = Mathf.Lerp(rightPitch, -6.0f, airFactor);
        leftRoll = Mathf.Lerp(leftRoll, -jumpArmFlareAngle, airFactor);
        rightRoll = Mathf.Lerp(rightRoll, jumpArmFlareAngle, airFactor);

        // Subtle yaw swing during active stride
        float leftYaw = cycleSin * 2.5f * moveWeight;
        float rightYaw = -cycleSin * 2.5f * moveWeight;

        // When moveWeight == 0 and airFactor == 0, target is EXACT initial resting rotation
        Quaternion targetLeftRot = leftArmInitialLocalRot * Quaternion.Euler(leftPitch, leftYaw, leftRoll);
        Quaternion targetRightRot = rightArmInitialLocalRot * Quaternion.Euler(rightPitch, rightYaw, rightRoll);

        leftArmPivot.localRotation = Quaternion.Slerp(leftArmPivot.localRotation, targetLeftRot, Time.deltaTime * smoothingSpeed);
        rightArmPivot.localRotation = Quaternion.Slerp(rightArmPivot.localRotation, targetRightRot, Time.deltaTime * smoothingSpeed);
    }

    private void ApplyTorsoKinematics(float cycleSin, float doubleSin, float moveWeight, bool isSprinting, float airFactor)
    {
        if (torsoTransform == null) return;

        float bobAmount = isSprinting ? sprintTorsoBob : walkTorsoBob;

        // 1. Vertical bounce (peaks at each step contact)
        float groundBob = Mathf.Abs(cycleSin) * bobAmount * moveWeight;

        // 2. Idle breathing displacement
        float idleBreath = Mathf.Sin(Time.time * idleBreathingSpeed) * idleBreathingBob * (1f - moveWeight);

        Vector3 targetPos = torsoInitialLocalPos + new Vector3(0f, groundBob + idleBreath, 0f);
        torsoTransform.localPosition = Vector3.Lerp(torsoTransform.localPosition, targetPos, Time.deltaTime * smoothingSpeed);

        // 3. Torso tilt and sway
        float pitchLean = forwardTorsoLean * moveWeight + (Mathf.Sin(Time.time * idleBreathingSpeed) * 0.25f * (1f - moveWeight));
        float rollSway = cycleSin * torsoSwayRoll * moveWeight;

        pitchLean = Mathf.Lerp(pitchLean, forwardTorsoLean * 1.4f, airFactor);

        Quaternion targetRot = torsoInitialLocalRot * Quaternion.Euler(pitchLean, 0f, rollSway);
        torsoTransform.localRotation = Quaternion.Slerp(torsoTransform.localRotation, targetRot, Time.deltaTime * smoothingSpeed);
    }

    /// <summary>
    /// Programmatically configure joint pivot transforms.
    /// </summary>
    public void ConfigureJoints(Transform torso, Transform lLeg, Transform rLeg, Transform lArm, Transform rArm)
    {
        torsoTransform = torso;
        leftLegPivot = lLeg;
        rightLegPivot = rLeg;
        leftArmPivot = lArm;
        rightArmPivot = rArm;
        CacheInitialTransforms();
    }
}

using UnityEngine;
using System;

/// <summary>
/// Controllable arcade-style spacecraft physics controller:
/// provides responsive 6-DOF arcade motion, speed acceleration, fuel consumption,
/// hull damage telemetry, and first-person cockpit camera control.
/// </summary>
public class SpacecraftController : MonoBehaviour
{
    [Header("--- Flight Dynamics (Arcade Physics) ---")]
    [Tooltip("Maximum standard forward cruise speed in m/s")]
    public float maxForwardSpeed = 50f;
    [Tooltip("Maximum reverse speed in m/s")]
    public float maxReverseSpeed = 20f;
    [Tooltip("Forward and reverse acceleration rate")]
    public float accelerationRate = 25f;
    [Tooltip("Passive drag deceleration rate when no thrust is applied")]
    public float naturalDamping = 12f;
    [Tooltip("Speed multiplier when boost thrusters are engaged")]
    public float boostMultiplier = 1.6f;

    [Header("--- Maneuvering & Steering ---")]
    [Tooltip("Pitch turn rate in degrees/sec")]
    public float pitchRate = 45f;
    [Tooltip("Yaw turn rate in degrees/sec")]
    public float yawRate = 50f;
    [Tooltip("Roll turn rate in degrees/sec")]
    public float rollRate = 60f;
    [Tooltip("Lateral and vertical strafe speed in m/s")]
    public float strafeSpeed = 18f;

    [Header("--- Fuel & Hull Vitals ---")]
    public float maxFuel = 100f;
    public float currentFuel = 100f;
    [Tooltip("Fuel consumed per second of standard thrust")]
    public float fuelBurnRate = 3.5f;
    [Tooltip("Extra fuel consumed per second when boosting")]
    public float boostFuelBurnRate = 7.0f;

    public float maxHull = 100f;
    public float currentHull = 100f;

    [Header("--- Cameras & Cockpit ---")]
    public Camera cockpitCamera;
    public Camera chaseCamera;
    public GameObject cockpitInteriorFrame;
    public Transform cockpitEyeAnchor;
    public bool startInCockpitView = true;

    [Header("--- Visual & Audio FX ---")]
    public Light[] thrusterLights;
    public GameObject[] exhaustPlumes;
    public ProceduralEngineAudio engineAudio;

    // Runtime state
    public float CurrentSpeed { get; private set; }
    public float Throttle01 { get; private set; }
    public bool IsBoosting { get; private set; }
    public bool IsAlive => currentHull > 0f && !isMissionFailed;
    public bool IsInCockpitView { get; private set; }

    public event Action<float, float> OnVitalsChanged; // fuel, hull
    public event Action<string> OnMissionFailed;

    private float currentForwardSpeed = 0f;
    private Vector2 currentStrafeVelocity = Vector2.zero; // X: lateral, Y: vertical
    private bool isMissionFailed = false;

    private void Start()
    {
        currentFuel = maxFuel;
        currentHull = maxHull;
        SetCameraView(startInCockpitView);
    }

    private void Update()
    {
        if (!IsAlive) return;

        HandleInput();
        HandleFuelConsumption();
        UpdateVisualsAndAudio();
    }

    private void FixedUpdate()
    {
        if (!IsAlive) return;

        ApplyMovement();
    }

    private void HandleInput()
    {
        // Toggle camera view with V
        if (AstroInput.GetKeyDown(KeyCode.V))
        {
            SetCameraView(!IsInCockpitView);
        }

        // Throttle & Pitch/Yaw Inputs
        float pitchInput = 0f;
        float yawInput = 0f;
        float rollInput = 0f;
        float forwardInput = 0f;
        float strafeX = 0f;
        float strafeY = 0f;

        // Forward / Reverse (W / S or Up / Down)
        if (AstroInput.GetKey(KeyCode.W) || AstroInput.GetKey(KeyCode.UpArrow)) forwardInput += 1f;
        if (AstroInput.GetKey(KeyCode.S) || AstroInput.GetKey(KeyCode.DownArrow)) forwardInput -= 1f;

        // Steering Yaw (A / D or Left / Right)
        if (AstroInput.GetKey(KeyCode.D) || AstroInput.GetKey(KeyCode.RightArrow)) yawInput += 1f;
        if (AstroInput.GetKey(KeyCode.A) || AstroInput.GetKey(KeyCode.LeftArrow)) yawInput -= 1f;

        // Pitch control (I / R = Pitch Up, K / F = Pitch Down)
        if (AstroInput.GetKey(KeyCode.I) || AstroInput.GetKey(KeyCode.R)) pitchInput -= 1f;
        if (AstroInput.GetKey(KeyCode.K) || AstroInput.GetKey(KeyCode.F)) pitchInput += 1f;

        // Roll control (Q / E)
        if (AstroInput.GetKey(KeyCode.Q)) rollInput += 1f;
        if (AstroInput.GetKey(KeyCode.E)) rollInput -= 1f;

        // Strafe Vertical (Space = Up, C / LeftCtrl = Down)
        if (AstroInput.GetKey(KeyCode.Space)) strafeY += 1f;
        if (AstroInput.GetKey(KeyCode.C) || AstroInput.GetKey(KeyCode.LeftControl)) strafeY -= 1f;

        // Strafe Lateral (J / L)
        if (AstroInput.GetKey(KeyCode.J)) strafeX -= 1f;
        if (AstroInput.GetKey(KeyCode.L)) strafeX += 1f;

        // Boost (Left Shift)
        IsBoosting = AstroInput.GetKey(KeyCode.LeftShift) && currentFuel > 0f && forwardInput > 0f;

        // Target speed calculation
        float topSpeed = IsBoosting ? maxForwardSpeed * boostMultiplier : maxForwardSpeed;
        float targetSpeed = 0f;

        if (currentFuel > 0f)
        {
            if (forwardInput > 0.05f) targetSpeed = forwardInput * topSpeed;
            else if (forwardInput < -0.05f) targetSpeed = forwardInput * maxReverseSpeed;
        }

        // Smooth speed acceleration or damping
        if (Mathf.Abs(targetSpeed) > 0.01f)
        {
            currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, targetSpeed, accelerationRate * Time.deltaTime);
        }
        else
        {
            currentForwardSpeed = Mathf.MoveTowards(currentForwardSpeed, 0f, naturalDamping * Time.deltaTime);
        }

        CurrentSpeed = currentForwardSpeed;
        Throttle01 = Mathf.Clamp01(Mathf.Abs(currentForwardSpeed) / topSpeed);

        // Strafe acceleration
        Vector2 targetStrafe = new Vector2(strafeX, strafeY) * strafeSpeed;
        currentStrafeVelocity = Vector2.MoveTowards(currentStrafeVelocity, targetStrafe, accelerationRate * 1.5f * Time.deltaTime);

        // Apply rotation directly
        float dt = Time.deltaTime;
        float appliedPitch = pitchInput * pitchRate * dt;
        float appliedYaw = yawInput * yawRate * dt;
        float appliedRoll = rollInput * rollRate * dt;

        transform.Rotate(appliedPitch, appliedYaw, appliedRoll, Space.Self);
    }

    private void ApplyMovement()
    {
        // Move along spacecraft orientation
        Vector3 forwardMotion = transform.forward * (currentForwardSpeed * Time.fixedDeltaTime);
        Vector3 strafeLateralMotion = transform.right * (currentStrafeVelocity.x * Time.fixedDeltaTime);
        Vector3 strafeVerticalMotion = transform.up * (currentStrafeVelocity.y * Time.fixedDeltaTime);

        transform.position += forwardMotion + strafeLateralMotion + strafeVerticalMotion;
    }

    private void HandleFuelConsumption()
    {
        if (currentFuel <= 0f) return;

        float burn = 0f;
        if (Mathf.Abs(currentForwardSpeed) > 1f)
        {
            burn += fuelBurnRate * (IsBoosting ? boostMultiplier : 1f);
        }
        if (currentStrafeVelocity.sqrMagnitude > 1f)
        {
            burn += fuelBurnRate * 0.5f;
        }

        if (burn > 0f)
        {
            currentFuel = Mathf.Max(0f, currentFuel - burn * Time.deltaTime);
            OnVitalsChanged?.Invoke(currentFuel, currentHull);
        }
    }

    public void TakeDamage(float amount, string damageSource = "Hazard Zone")
    {
        if (!IsAlive) return;

        currentHull = Mathf.Max(0f, currentHull - amount);
        OnVitalsChanged?.Invoke(currentFuel, currentHull);

        if (currentHull <= 0f)
        {
            TriggerFailure($"HULL CATASTROPHIC FAILURE // DESTROYED BY {damageSource.ToUpper()}");
        }
    }

    public void Refuel(float amount)
    {
        currentFuel = Mathf.Min(maxFuel, currentFuel + amount);
        OnVitalsChanged?.Invoke(currentFuel, currentHull);
    }

    public void RepairHull(float amount)
    {
        currentHull = Mathf.Min(maxHull, currentHull + amount);
        OnVitalsChanged?.Invoke(currentFuel, currentHull);
    }

    public void TriggerFailure(string reason)
    {
        if (isMissionFailed) return;
        isMissionFailed = true;

        currentForwardSpeed = 0f;
        currentStrafeVelocity = Vector2.zero;

        if (engineAudio != null)
            engineAudio.SetThrust(0f);

        SetEngineVisuals(false, 0f);
        OnMissionFailed?.Invoke(reason);
    }

    public void SetCameraView(bool toCockpit)
    {
        IsInCockpitView = toCockpit;

        if (cockpitCamera != null)
            cockpitCamera.gameObject.SetActive(toCockpit);

        if (chaseCamera != null)
            chaseCamera.gameObject.SetActive(!toCockpit);

        if (cockpitInteriorFrame != null)
            cockpitInteriorFrame.SetActive(toCockpit);
    }

    private void UpdateVisualsAndAudio()
    {
        float intensity = Throttle01 * (IsBoosting ? 1.4f : 1.0f);
        bool hasThrust = intensity > 0.05f && currentFuel > 0f;

        SetEngineVisuals(hasThrust, intensity);

        if (engineAudio != null)
        {
            engineAudio.SetThrust(hasThrust ? intensity : 0f);
        }
    }

    private void SetEngineVisuals(bool active, float intensity)
    {
        if (thrusterLights != null)
        {
            foreach (Light l in thrusterLights)
            {
                if (l != null)
                {
                    l.enabled = active;
                    l.intensity = intensity * 4.0f;
                }
            }
        }

        if (exhaustPlumes != null)
        {
            foreach (GameObject p in exhaustPlumes)
            {
                if (p != null)
                {
                    p.SetActive(active);
                    p.transform.localScale = new Vector3(
                        1f * (0.6f + intensity * 0.4f),
                        1f * (0.5f + intensity * 1.0f),
                        1f * (0.6f + intensity * 0.4f));
                }
            }
        }
    }
}

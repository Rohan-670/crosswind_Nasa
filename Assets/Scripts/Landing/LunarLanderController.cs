using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Controls the final lunar descent and landing sequence:
/// simulates lunar gravity descent, main engine counter-thrust, lateral RCS,
/// altitude radar telemetry, and detects safe touchdown vs hard impact.
/// </summary>
public class LunarLanderController : MonoBehaviour
{
    [Header("--- Lunar Descent Physics ---")]
    [Tooltip("Lunar gravity acceleration in m/s²")]
    public float lunarGravity = 1.62f;
    [Tooltip("Maximum main engine thrust acceleration in m/s²")]
    public float maxMainThrust = 4.2f;
    [Tooltip("Lateral RCS maneuvering acceleration in m/s²")]
    public float rcsThrust = 2.0f;
    [Tooltip("Maximum descent fuel capacity")]
    public float maxFuel = 100f;
    public float fuelBurnRate = 4.0f;

    [Header("--- Landing Safety Parameters ---")]
    [Tooltip("Maximum vertical descent speed for a safe landing in m/s")]
    public float safeTouchdownMaxSpeed = 4.0f;
    [Tooltip("Maximum horizontal drift speed for a safe landing in m/s")]
    public float safeTouchdownMaxDrift = 2.5f;

    [Header("--- Ground & Radar Detection ---")]
    public Transform groundContactRaycastOrigin;
    public LayerMask terrainLayer = ~0;
    public float initialAltitude = 320f;

    [Header("--- Visual & Audio FX ---")]
    public Light[] mainEngineLights;
    public GameObject[] mainEnginePlumes;
    public ProceduralEngineAudio engineAudio;
    public ParticleSystem landingDustParticles;

    // Runtime Telemetry
    public float CurrentAltitude { get; private set; }
    public float VerticalSpeed { get; private set; } // Negative = descending, positive = ascending
    public float HorizontalDriftSpeed { get; private set; }
    public float CurrentFuel { get; private set; }
    public float MainThrottle01 { get; private set; }
    public bool HasLanded { get; private set; } = false;
    public bool IsLandingSafe { get; private set; } = false;

    public event Action<bool, float, string> OnLandingResult; // isSafe, impactSpeed, message

    private Vector3 currentVelocity = Vector3.zero;
    private bool isInputLocked = false;

    private void Start()
    {
        CurrentFuel = maxFuel;
        CurrentAltitude = initialAltitude;
        currentVelocity = new Vector3(0.5f, -8.0f, 0.2f); // Starting descent velocity

        if (groundContactRaycastOrigin == null)
            groundContactRaycastOrigin = transform;
    }

    private void Update()
    {
        if (HasLanded) return;

        HandleInput();
        UpdateRadarTelemetry();
        UpdateEngineFX();
    }

    private void FixedUpdate()
    {
        if (HasLanded) return;

        ApplyDescentPhysics();
    }

    private void HandleInput()
    {
        if (isInputLocked)
        {
            MainThrottle01 = 0f;
            return;
        }

        float throttle = 0f;
        Vector3 rcs = Vector3.zero;

        // Main Descent Engine (Space or W or UpArrow)
        if (CurrentFuel > 0f)
        {
            if (AstroInput.GetKey(KeyCode.Space) || AstroInput.GetKey(KeyCode.W) || AstroInput.GetKey(KeyCode.UpArrow))
            {
                throttle = AstroInput.GetKey(KeyCode.LeftShift) ? 1.0f : 0.65f;
            }

            // Lateral RCS steering
            if (AstroInput.GetKey(KeyCode.A) || AstroInput.GetKey(KeyCode.LeftArrow)) rcs.x -= 1f;
            if (AstroInput.GetKey(KeyCode.D) || AstroInput.GetKey(KeyCode.RightArrow)) rcs.x += 1f;
            if (AstroInput.GetKey(KeyCode.S) || AstroInput.GetKey(KeyCode.DownArrow)) rcs.z -= 1f;
        }

        MainThrottle01 = Mathf.MoveTowards(MainThrottle01, throttle, Time.deltaTime * 3.5f);

        // Fuel consumption
        if (MainThrottle01 > 0.05f)
        {
            float burn = MainThrottle01 * fuelBurnRate * Time.deltaTime;
            CurrentFuel = Mathf.Max(0f, CurrentFuel - burn);
        }

        // Apply RCS directly to horizontal velocity
        if (rcs.sqrMagnitude > 0.01f && CurrentFuel > 0f)
        {
            currentVelocity += rcs.normalized * (rcsThrust * Time.deltaTime);
            CurrentFuel = Mathf.Max(0f, CurrentFuel - fuelBurnRate * 0.3f * Time.deltaTime);
        }
    }

    private void ApplyDescentPhysics()
    {
        float dt = Time.fixedDeltaTime;

        // Lunar gravity pulling down
        currentVelocity.y -= lunarGravity * dt;

        // Main engine thrust pushing up
        if (MainThrottle01 > 0.01f && CurrentFuel > 0f)
        {
            currentVelocity.y += MainThrottle01 * maxMainThrust * dt;
        }

        // Slight atmospheric/vacuum damping on lateral drift
        currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, 0f, 0.15f * dt);
        currentVelocity.z = Mathf.MoveTowards(currentVelocity.z, 0f, 0.15f * dt);

        // Move lander
        transform.position += currentVelocity * dt;

        // Check ground contact
        if (CurrentAltitude <= 0.6f && currentVelocity.y <= 0f)
        {
            EvaluateTouchdown();
        }
    }

    private void UpdateRadarTelemetry()
    {
        RaycastHit hit;
        Vector3 origin = groundContactRaycastOrigin != null ? groundContactRaycastOrigin.position : transform.position;

        if (Physics.Raycast(origin, Vector3.down, out hit, 1500f, terrainLayer))
        {
            CurrentAltitude = hit.distance;
        }
        else
        {
            CurrentAltitude = Mathf.Max(0f, transform.position.y);
        }

        VerticalSpeed = currentVelocity.y;
        HorizontalDriftSpeed = new Vector2(currentVelocity.x, currentVelocity.z).magnitude;
    }

    private void EvaluateTouchdown()
    {
        HasLanded = true;
        isInputLocked = true;
        MainThrottle01 = 0f;

        float impactVertSpeed = Mathf.Abs(VerticalSpeed);
        float drift = HorizontalDriftSpeed;

        bool speedSafe = impactVertSpeed <= safeTouchdownMaxSpeed;
        bool driftSafe = drift <= safeTouchdownMaxDrift;
        IsLandingSafe = speedSafe && driftSafe;

        if (landingDustParticles != null)
            landingDustParticles.Play();

        if (IsLandingSafe)
        {
            string msg = $"TOUCHDOWN NOMINAL\n" +
                $"Impact Velocity: {impactVertSpeed:F1} m/s (Limit: {safeTouchdownMaxSpeed:F1})\n" +
                $"Lateral Drift: {drift:F1} m/s // Landing Gear Locked";
            Debug.Log($"[LunarLander] {msg}");
            OnLandingResult?.Invoke(true, impactVertSpeed, msg);

            StartCoroutine(TransitionToExplorationRoutine());
        }
        else
        {
            string msg = $"HARD IMPACT // LANDER STRUCTURAL COLLAPSE\n" +
                $"Impact Velocity: {impactVertSpeed:F1} m/s (Maximum Allowable: {safeTouchdownMaxSpeed:F1} m/s)";
            Debug.LogWarning($"[LunarLander] {msg}");
            OnLandingResult?.Invoke(false, impactVertSpeed, msg);
        }
    }

    private IEnumerator TransitionToExplorationRoutine()
    {
        yield return new WaitForSeconds(3.0f);

        string nextScene = "LunarExploration";
        if (Application.CanStreamedLevelBeLoaded(nextScene))
        {
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            Debug.LogWarning($"[LunarLander] Scene '{nextScene}' not in build settings. Falling back to MissionSelection.");
            SceneManager.LoadScene("MissionSelection");
        }
    }

    private void UpdateEngineFX()
    {
        bool hasThrust = MainThrottle01 > 0.05f && CurrentFuel > 0f;

        if (mainEngineLights != null)
        {
            foreach (Light l in mainEngineLights)
            {
                if (l != null)
                {
                    l.enabled = hasThrust;
                    l.intensity = MainThrottle01 * 4.0f;
                }
            }
        }

        if (mainEnginePlumes != null)
        {
            foreach (GameObject p in mainEnginePlumes)
            {
                if (p != null)
                {
                    p.SetActive(hasThrust);
                    p.transform.localScale = new Vector3(
                        1f * (0.6f + MainThrottle01 * 0.4f),
                        1f * (0.5f + MainThrottle01 * 1.2f),
                        1f * (0.6f + MainThrottle01 * 0.4f));
                }
            }
        }

        if (engineAudio != null)
        {
            engineAudio.SetThrust(hasThrust ? MainThrottle01 : 0f);
        }
    }

    public void RetryDescent()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

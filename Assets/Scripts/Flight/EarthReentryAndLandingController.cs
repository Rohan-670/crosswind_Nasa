using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Controls the complete Earth Return flight progression:
/// 1. Trans-Earth deep space flight & checkpoint navigation
/// 2. Simplified atmospheric re-entry with heat shield & pitch corridor monitoring
/// 3. Final runway glide-slope approach and touchdown evaluation (safe vs hard landing)
/// 4. Seamless transition to MissionResults scene
/// </summary>
public class EarthReentryAndLandingController : MonoBehaviour
{
    public enum ReturnStage
    {
        TransEarthCruise,
        AtmosphericReentry,
        RunwayApproach,
        TouchdownComplete,
        MissionFailed
    }

    [Header("--- Target Core Systems ---")]
    public SpacecraftController spacecraft;
    public FlightHUDController flightHUD;
    public CheckpointManager checkpointManager;
    public RouteManager routeManager;
    public Transform earthModel;

    [Header("--- Re-entry Environment & FX ---")]
    public ReturnFlightVFXController vfxController;
    public GameObject plasmaFXRoot;
    public Light[] plasmaLights;
    public ParticleSystem plasmaParticles;
    public AudioSource atmosphericRumbleAudio;

    [Header("--- Runway & Landing Zone ---")]
    public Transform runwayRoot;
    public Transform touchdownTargetPoint;
    public float runwayLength = 350f;
    public float runwayWidth = 35f;
    public float maxSafeDescentRate = 6.5f; // m/s
    public float maxSafeForwardSpeed = 45f; // m/s

    [Header("--- HUD Re-entry & Landing Overlays ---")]
    public GameObject reentryOverlayPanel;
    public TextMeshProUGUI reentryCorridorText;
    public TextMeshProUGUI heatShieldTempText;
    public Image heatShieldBar;

    public GameObject landingGuidancePanel;
    public TextMeshProUGUI radarAltimeterText;
    public TextMeshProUGUI sinkRateText;
    public TextMeshProUGUI glideSlopeStatusText;

    [Header("--- Touchdown Results Modal ---")]
    public GameObject touchdownSuccessModal;
    public TextMeshProUGUI touchdownSummaryText;
    public Button proceedToResultsButton;

    public GameObject touchdownFailureModal;
    public TextMeshProUGUI failureDetailsText;
    public Button retryApproachButton;
    public Button returnToMissionsButton;

    [Header("--- Scene Navigation ---")]
    public string missionResultsSceneName = "MissionResults";

    // Runtime state
    public ReturnStage CurrentStage { get; private set; } = ReturnStage.TransEarthCruise;
    public float HeatShieldTemperature { get; private set; } = 650f;
    public float AltitudeAboveRunway { get; private set; } = 3000f;
    public float CurrentDescentRate { get; private set; } = 0f;

    private float reentryTimer = 0f;
    private const float ReentryCorridorDuration = 15f;
    private bool hasTouchedDown = false;
    private float previousAltitude = 0f;

    private void Start()
    {
        if (vfxController == null) vfxController = GetComponent<ReturnFlightVFXController>();

        // Initial overlay states
        if (reentryOverlayPanel != null) reentryOverlayPanel.SetActive(false);
        if (landingGuidancePanel != null) landingGuidancePanel.SetActive(false);
        if (touchdownSuccessModal != null) touchdownSuccessModal.SetActive(false);
        if (touchdownFailureModal != null) touchdownFailureModal.SetActive(false);
        if (plasmaFXRoot != null) plasmaFXRoot.SetActive(false);

        // Wire modal buttons
        if (proceedToResultsButton != null)
        {
            proceedToResultsButton.onClick.RemoveAllListeners();
            proceedToResultsButton.onClick.AddListener(OnProceedToResultsClicked);
        }

        if (retryApproachButton != null)
        {
            retryApproachButton.onClick.RemoveAllListeners();
            retryApproachButton.onClick.AddListener(OnRetryApproachClicked);
        }

        if (returnToMissionsButton != null)
        {
            returnToMissionsButton.onClick.RemoveAllListeners();
            returnToMissionsButton.onClick.AddListener(OnReturnToMissionsClicked);
        }

        // Listen for checkpoint clearances
        if (checkpointManager != null)
        {
            checkpointManager.OnCheckpointCleared += HandleCheckpointCleared;
            checkpointManager.OnAllCheckpointsCleared += HandleAllCheckpointsCleared;
        }

        if (spacecraft != null)
        {
            spacecraft.OnMissionFailed += HandleSpacecraftDestroyed;
        }

        previousAltitude = spacecraft != null ? spacecraft.transform.position.y : 3000f;
    }

    private void OnDestroy()
    {
        if (checkpointManager != null)
        {
            checkpointManager.OnCheckpointCleared -= HandleCheckpointCleared;
            checkpointManager.OnAllCheckpointsCleared -= HandleAllCheckpointsCleared;
        }

        if (spacecraft != null)
        {
            spacecraft.OnMissionFailed -= HandleSpacecraftDestroyed;
        }
    }

    private void Update()
    {
        if (spacecraft == null || !spacecraft.IsAlive) return;

        UpdateEarthProximity();

        switch (CurrentStage)
        {
            case ReturnStage.TransEarthCruise:
                UpdateTransEarthCruise();
                break;

            case ReturnStage.AtmosphericReentry:
                UpdateAtmosphericReentry();
                break;

            case ReturnStage.RunwayApproach:
                UpdateRunwayApproach();
                break;
        }
    }

    private void UpdateEarthProximity()
    {
        // Scale and visual position of Earth as player progresses towards Entry Interface
        if (earthModel != null && spacecraft != null)
        {
            float z = spacecraft.transform.position.z;
            // From z=0 to z=900, Earth grows smoothly in perspective
            float t = Mathf.Clamp01(z / 900f);
            float baseScale = Mathf.Lerp(80f, 260f, t);
            earthModel.localScale = new Vector3(baseScale, baseScale, baseScale);
        }
    }

    private void UpdateTransEarthCruise()
    {
        // Check if spacecraft has crossed into the atmospheric entry interface corridor (z >= 880)
        if (spacecraft.transform.position.z >= 880f)
        {
            InitiateAtmosphericReentry();
        }
    }

    private void HandleCheckpointCleared(Checkpoint cp, int index, int total)
    {
        Debug.Log($"[EarthReturn] Checkpoint {index + 1}/{total} cleared: {cp.checkpointName}");

        // If cleared final deep space checkpoint (Entry Interface)
        if (cp.isFinalDestination || index == total - 1)
        {
            InitiateAtmosphericReentry();
        }
    }

    private void HandleAllCheckpointsCleared()
    {
        if (CurrentStage == ReturnStage.TransEarthCruise)
        {
            InitiateAtmosphericReentry();
        }
    }

    public void InitiateAtmosphericReentry()
    {
        if (CurrentStage != ReturnStage.TransEarthCruise) return;
        CurrentStage = ReturnStage.AtmosphericReentry;
        reentryTimer = 0f;

        Debug.Log("[EarthReturn] Penetrating Earth's Atmospheric Interface (120 km) // Re-entry initiated.");

        if (reentryOverlayPanel != null) reentryOverlayPanel.SetActive(true);
        if (vfxController != null) vfxController.SetReentryFXActive(true);
        if (plasmaFXRoot != null) plasmaFXRoot.SetActive(true);
        if (plasmaParticles != null) plasmaParticles.Play();
        if (atmosphericRumbleAudio != null) atmosphericRumbleAudio.Play();

        // Hide deep space deviation banner to focus on re-entry corridor
        if (flightHUD != null && flightHUD.deviationBanner != null)
            flightHUD.deviationBanner.SetActive(false);
    }

    private void UpdateAtmosphericReentry()
    {
        reentryTimer += Time.deltaTime;

        // 1. Evaluate Spacecraft Pitch Attitude
        // In Unity, Euler angles: positive X is nose down, negative X (or 360-X) is nose up.
        float pitch = spacecraft.transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        // Optimal nose-up re-entry attitude is +8 to +16 degrees nose-up
        float pitchAngle = -pitch; // positive = nose up

        bool inPitchCorridor = (pitchAngle >= 4.0f && pitchAngle <= 18.0f);

        // 2. Heat Shield Dynamics
        float targetHeat;
        if (inPitchCorridor)
        {
            targetHeat = Mathf.Lerp(900f, 1650f, Mathf.Sin((reentryTimer / ReentryCorridorDuration) * Mathf.PI));
        }
        else
        {
            // Pitch out of corridor causes severe thermal spikes
            targetHeat = 2200f + Mathf.Abs(pitchAngle - 11f) * 45f;
            // Inflict thermal hull damage if overheating
            if (HeatShieldTemperature > 2250f)
            {
                spacecraft.TakeDamage(12f * Time.deltaTime, "ATMOSPHERIC RE-ENTRY THERMAL OVERHEAT");
                if (!spacecraft.IsAlive)
                {
                    CurrentStage = ReturnStage.MissionFailed;
                    return;
                }
            }
        }

        HeatShieldTemperature = Mathf.MoveTowards(HeatShieldTemperature, targetHeat, 320f * Time.deltaTime);

        // 3. Aerodynamic Drag slows the ship into atmospheric cruise speed
        if (spacecraft.CurrentSpeed > 45f)
        {
            // Drag applied
            spacecraft.transform.position += spacecraft.transform.forward * (-6f * Time.deltaTime);
        }

        // 4. Update Re-entry HUD
        UpdateReentryHUD(pitchAngle, inPitchCorridor);

        // 5. Complete Re-entry blackout phase and enter visual runway approach
        if (reentryTimer >= ReentryCorridorDuration)
        {
            TransitionToRunwayApproach();
        }
    }

    private void UpdateReentryHUD(float pitchAngle, bool inCorridor)
    {
        if (reentryCorridorText != null)
        {
            if (inCorridor)
            {
                reentryCorridorText.text = $"RE-ENTRY PITCH: {pitchAngle:+0.0;-0.0}° <color=#55FF88>[CORRIDOR OPTIMAL]</color>";
                reentryCorridorText.color = new Color(0.35f, 1f, 0.55f);
            }
            else
            {
                string advise = pitchAngle < 4.0f ? "PULL UP (+8° to +15°)" : "PITCH NOSE DOWN (+8° to +15°)";
                reentryCorridorText.text = $"RE-ENTRY PITCH: {pitchAngle:+0.0;-0.0}° <color=#FF4444>[ATTITUDE DANGER: {advise}]</color>";
                reentryCorridorText.color = new Color(1f, 0.35f, 0.35f);
            }
        }

        if (heatShieldTempText != null)
        {
            Color tempCol = HeatShieldTemperature > 2100f ? new Color(1f, 0.25f, 0.25f) : (HeatShieldTemperature > 1600f ? new Color(1f, 0.75f, 0.2f) : new Color(0.4f, 0.9f, 1f));
            heatShieldTempText.text = $"HEAT SHIELD: {HeatShieldTemperature:F0}°C";
            heatShieldTempText.color = tempCol;
        }

        if (heatShieldBar != null)
        {
            float fill = Mathf.Clamp01(HeatShieldTemperature / 2500f);
            heatShieldBar.fillAmount = fill;
            heatShieldBar.color = Color.Lerp(new Color(0.2f, 0.8f, 1f), new Color(1f, 0.2f, 0.2f), fill);
        }
    }

    private void TransitionToRunwayApproach()
    {
        CurrentStage = ReturnStage.RunwayApproach;
        Debug.Log("[EarthReturn] Atmospheric re-entry cleared. Established on final visual runway approach.");

        if (reentryOverlayPanel != null) reentryOverlayPanel.SetActive(false);
        if (vfxController != null) vfxController.SetReentryFXActive(false);
        if (plasmaFXRoot != null) plasmaFXRoot.SetActive(false);
        if (plasmaParticles != null) plasmaParticles.Stop();
        if (atmosphericRumbleAudio != null) atmosphericRumbleAudio.Stop();

        if (landingGuidancePanel != null) landingGuidancePanel.SetActive(true);

        // Reposition spacecraft to final approach glide slope if needed
        if (runwayRoot != null)
        {
            // Position 120m altitude, 450m out on centerline
            Vector3 approachStart = runwayRoot.position + new Vector3(0f, 95f, -420f);
            spacecraft.transform.position = approachStart;
            spacecraft.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
        }
    }

    private void UpdateRunwayApproach()
    {
        if (hasTouchedDown || runwayRoot == null) return;

        // 1. Calculate Altimeter and Vertical Descent Rate
        float currentY = spacecraft.transform.position.y;
        float runwayY = runwayRoot.position.y;
        AltitudeAboveRunway = Mathf.Max(0f, currentY - runwayY);

        CurrentDescentRate = -(currentY - previousAltitude) / Mathf.Max(0.001f, Time.deltaTime);
        previousAltitude = currentY;

        // 2. Update Landing HUD Telemetry
        UpdateLandingGuidanceHUD();

        // 3. Evaluate Touchdown Proximity
        if (AltitudeAboveRunway <= 1.2f)
        {
            EvaluateTouchdown();
        }
    }

    private void UpdateLandingGuidanceHUD()
    {
        if (radarAltimeterText != null)
        {
            radarAltimeterText.text = $"RADAR ALTITUDE: {AltitudeAboveRunway:F1} M";
            radarAltimeterText.color = AltitudeAboveRunway < 20f ? new Color(1f, 0.85f, 0.2f) : new Color(0.85f, 0.92f, 0.98f);
        }

        if (sinkRateText != null)
        {
            bool isSafeSink = CurrentDescentRate <= maxSafeDescentRate;
            string status = isSafeSink ? "<color=#55FF88>[NOMINAL]</color>" : "<color=#FF4444>[EXCESSIVE SINK - FLARE NOSE]</color>";
            sinkRateText.text = $"SINK RATE: {CurrentDescentRate:F1} M/S {status}";
            sinkRateText.color = isSafeSink ? new Color(0.35f, 1f, 0.55f) : new Color(1f, 0.3f, 0.3f);
        }

        if (glideSlopeStatusText != null)
        {
            float relX = runwayRoot != null ? spacecraft.transform.position.x - runwayRoot.position.x : 0f;
            if (Mathf.Abs(relX) <= (runwayWidth * 0.45f))
            {
                glideSlopeStatusText.text = "GLIDESLOPE: CENTERLINE ALIGNED";
                glideSlopeStatusText.color = new Color(0.35f, 1f, 0.55f);
            }
            else
            {
                string steer = relX < 0 ? "STEER RIGHT >" : "< STEER LEFT";
                glideSlopeStatusText.text = $"GLIDESLOPE DEVIATION // {steer}";
                glideSlopeStatusText.color = new Color(1f, 0.75f, 0.2f);
            }
        }
    }

    private void EvaluateTouchdown()
    {
        hasTouchedDown = true;
        float finalSinkRate = CurrentDescentRate;
        float finalSpeed = spacecraft.CurrentSpeed;

        // Check horizontal bounds relative to runway
        Vector3 localPos = runwayRoot.InverseTransformPoint(spacecraft.transform.position);
        bool onRunwayX = Mathf.Abs(localPos.x) <= (runwayWidth * 0.6f);
        bool onRunwayZ = localPos.z >= -30f && localPos.z <= (runwayLength + 60f);

        bool safeSink = finalSinkRate <= maxSafeDescentRate;
        bool safeSpeed = finalSpeed <= maxSafeForwardSpeed;

        if (vfxController != null) vfxController.TriggerTouchdownSmoke(spacecraft.transform.position);

        if (onRunwayX && onRunwayZ && safeSink && safeSpeed)
        {
            // Successful Safe Touchdown!
            TriggerSuccessfulLanding(finalSinkRate, finalSpeed);
        }
        else
        {
            // Crash / Hard Touchdown
            string reason = "";
            if (!safeSink) reason = $"HARD LANDING // SINK RATE ({finalSinkRate:F1} M/S) EXCEEDED SAFETY LIMIT ({maxSafeDescentRate:F1} M/S)";
            else if (!safeSpeed) reason = $"RUNWAY OVERRUN // TOUCHDOWN AIRSPEED ({finalSpeed:F1} M/S) EXCEEDED TIRE THRESHOLD";
            else reason = "OFF-RUNWAY IMPACT // SPACECRAFT VEERED BEYOND LANDING STRIP PERIMETER";

            TriggerHardLanding(reason);
        }
    }

    private void TriggerSuccessfulLanding(float sinkRate, float speed)
    {
        CurrentStage = ReturnStage.TouchdownComplete;
        Debug.Log("[EarthReturn] Successful landing confirmed on Kennedy Space Center runway!");

        if (MissionManager.Instance != null)
        {
            float fuel = spacecraft != null ? spacecraft.currentFuel : 68.5f;
            int cpCleared = checkpointManager != null ? checkpointManager.checkpoints.Count : 5;
            float navAcc = 98.4f;
            if (routeManager != null && routeManager.IsDeviating) navAcc = 84.0f;
            MissionManager.Instance.RecordReturnFlightResults(true, navAcc, cpCleared, 5, sinkRate, speed, true, fuel);
        }

        if (landingGuidancePanel != null) landingGuidancePanel.SetActive(false);

        // Smooth stop
        StartCoroutine(BrakeToStopRoutine());

        // Show Touchdown Confirmed modal
        if (touchdownSuccessModal != null)
        {
            touchdownSuccessModal.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (touchdownSummaryText != null)
            {
                float fuel = spacecraft != null ? spacecraft.currentFuel : 100f;
                float hull = spacecraft != null ? spacecraft.currentHull : 100f;
                touchdownSummaryText.text = "KENNEDY SPACE CENTER // SHUTTLE LANDING FACILITY\n\n" +
                    $"• Touchdown Status: WHEELS STOP // RECOVERY CONFIRMED\n" +
                    $"• Touchdown Sink Rate: {sinkRate:F1} m/s (Grade: Nominal)\n" +
                    $"• Touchdown Airspeed: {speed:F1} m/s\n" +
                    $"• Heat Shield Integrity: INTACT (Max {HeatShieldTemperature:F0}°C)\n" +
                    $"• Remaining Flight Fuel: {fuel:F1}%\n" +
                    $"• Final Airframe Hull: {hull:F1}%\n\n" +
                    "All lunar samples and scientific data secured for planetary debriefing.";
            }
        }
    }

    private IEnumerator BrakeToStopRoutine()
    {
        float dur = 2.5f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            if (spacecraft != null)
            {
                spacecraft.transform.position += spacecraft.transform.forward * (Mathf.Lerp(15f, 0f, elapsed / dur) * Time.deltaTime);
            }
            yield return null;
        }
    }

    private void TriggerHardLanding(string reason)
    {
        CurrentStage = ReturnStage.MissionFailed;
        Debug.LogWarning($"[EarthReturn] Touchdown failed: {reason}");

        if (MissionManager.Instance != null)
        {
            float fuel = spacecraft != null ? spacecraft.currentFuel : 0f;
            int cpCleared = checkpointManager != null ? checkpointManager.CurrentCheckpointIndex : 0;
            MissionManager.Instance.RecordReturnFlightResults(false, 60f, cpCleared, 5, CurrentDescentRate, spacecraft != null ? spacecraft.CurrentSpeed : 0f, false, fuel, reason);
        }

        if (landingGuidancePanel != null) landingGuidancePanel.SetActive(false);

        if (spacecraft != null)
        {
            spacecraft.TriggerFailure(reason);
        }

        if (touchdownFailureModal != null)
        {
            touchdownFailureModal.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (failureDetailsText != null)
            {
                failureDetailsText.text = reason;
            }
        }
    }

    private void HandleSpacecraftDestroyed(string reason)
    {
        if (CurrentStage == ReturnStage.TouchdownComplete) return;

        CurrentStage = ReturnStage.MissionFailed;
        if (reentryOverlayPanel != null) reentryOverlayPanel.SetActive(false);
        if (landingGuidancePanel != null) landingGuidancePanel.SetActive(false);

        if (touchdownFailureModal != null)
        {
            touchdownFailureModal.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (failureDetailsText != null)
            {
                failureDetailsText.text = reason;
            }
        }
    }

    private void OnProceedToResultsClicked()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!string.IsNullOrEmpty(missionResultsSceneName) && Application.CanStreamedLevelBeLoaded(missionResultsSceneName))
        {
            SceneManager.LoadScene(missionResultsSceneName);
        }
        else
        {
            OnReturnToMissionsClicked();
        }
    }

    private void OnRetryApproachClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnReturnToMissionsClicked()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (Application.CanStreamedLevelBeLoaded("MissionSelection"))
        {
            SceneManager.LoadScene("MissionSelection");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }
}

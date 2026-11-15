using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Comprehensive flight HUD controller:
/// manages speedometer, fuel/hull condition bars, 3D world destination marker,
/// deviation warning banners with recovery countdowns, hazard alerts,
/// and mission completion/failure modals.
/// </summary>
public class FlightHUDController : MonoBehaviour
{
    [Header("--- Target Systems ---")]
    public SpacecraftController spacecraft;
    public RouteManager routeManager;
    public CheckpointManager checkpointManager;
    public HazardManager hazardManager;
    public Camera flightCamera;

    [Header("--- Flight Telemetry Displays ---")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI throttleText;
    public Image throttleBar;

    [Header("--- Fuel & Hull Telemetry ---")]
    public TextMeshProUGUI fuelText;
    public Image fuelBar;
    public TextMeshProUGUI hullText;
    public Image hullBar;

    [Header("--- Objectives & Navigation ---")]
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI checkpointDistanceText;

    [Header("--- 3D Screen-Space Destination Marker ---")]
    public RectTransform destinationMarker;
    public TextMeshProUGUI destinationMarkerLabel;
    public TextMeshProUGUI destinationMarkerDistance;

    [Header("--- Deviation Warning Banner ---")]
    public GameObject deviationBanner;
    public TextMeshProUGUI deviationTitleText;
    public TextMeshProUGUI deviationTimerText;

    [Header("--- Hazard Warning Banner ---")]
    public GameObject hazardBanner;
    public TextMeshProUGUI hazardTitleText;

    [Header("--- Mission Result Modals ---")]
    public GameObject missionFailedModal;
    public TextMeshProUGUI failureReasonText;
    public Button retryFlightButton;
    public Button returnFromFailureButton;

    public GameObject missionSuccessModal;
    public TextMeshProUGUI successSummaryText;
    public Button continueMissionButton;

    [Header("--- Scene Navigation ---")]
    [Tooltip("Target scene loaded when continuing upon mission success")]
    public string nextSceneName = "LunarLanding";
    [Tooltip("Target scene loaded when retrying from failure modal (defaults to LaunchSequence)")]
    public string retrySceneName = "LaunchSequence";

    [Header("--- Camera & Controls Hint ---")]
    public TextMeshProUGUI cameraModeText;
    public TextMeshProUGUI controlsHintText;

    private void Start()
    {
        if (flightCamera == null)
            flightCamera = Camera.main;

        // Wire buttons
        if (retryFlightButton != null)
        {
            retryFlightButton.onClick.RemoveAllListeners();
            retryFlightButton.onClick.AddListener(OnRetryFlightClicked);
        }

        if (returnFromFailureButton != null)
        {
            returnFromFailureButton.onClick.RemoveAllListeners();
            returnFromFailureButton.onClick.AddListener(OnReturnToMissionsClicked);
        }

        if (continueMissionButton != null)
        {
            continueMissionButton.onClick.RemoveAllListeners();
            continueMissionButton.onClick.AddListener(OnContinueToLunarLandingClicked);
        }

        // Hide modals & banners initially
        if (deviationBanner != null) deviationBanner.SetActive(false);
        if (hazardBanner != null) hazardBanner.SetActive(false);
        if (missionFailedModal != null) missionFailedModal.SetActive(false);
        if (missionSuccessModal != null) missionSuccessModal.SetActive(false);

        // Bind events
        if (spacecraft != null)
        {
            spacecraft.OnVitalsChanged += UpdateVitalsUI;
            spacecraft.OnMissionFailed += ShowMissionFailedModal;
        }

        if (routeManager != null)
        {
            routeManager.OnDeviationStatusChanged += UpdateDeviationUI;
        }

        if (checkpointManager != null)
        {
            checkpointManager.OnCheckpointCleared += HandleCheckpointCleared;
            checkpointManager.OnAllCheckpointsCleared += ShowMissionSuccessModal;
        }

        if (hazardManager != null)
        {
            hazardManager.OnHazardAlertChanged += UpdateHazardUI;
        }

        UpdateObjectiveDisplay();
        UpdateVitalsUI(spacecraft != null ? spacecraft.currentFuel : 100f, spacecraft != null ? spacecraft.currentHull : 100f);
    }

    private void OnDestroy()
    {
        if (spacecraft != null)
        {
            spacecraft.OnVitalsChanged -= UpdateVitalsUI;
            spacecraft.OnMissionFailed -= ShowMissionFailedModal;
        }

        if (routeManager != null)
        {
            routeManager.OnDeviationStatusChanged -= UpdateDeviationUI;
        }

        if (checkpointManager != null)
        {
            checkpointManager.OnCheckpointCleared -= HandleCheckpointCleared;
            checkpointManager.OnAllCheckpointsCleared -= ShowMissionSuccessModal;
        }

        if (hazardManager != null)
        {
            hazardManager.OnHazardAlertChanged -= UpdateHazardUI;
        }
    }

    private void Update()
    {
        if (spacecraft == null) return;

        UpdateFlightMetricsUI();
        UpdateDestinationMarkerUI();
    }

    private void UpdateFlightMetricsUI()
    {
        // Speed & Throttle
        if (speedText != null)
        {
            string boostTag = spacecraft.IsBoosting ? " <color=#FF8800>[BOOST]</color>" : "";
            speedText.text = $"SPEED: {spacecraft.CurrentSpeed:F1} M/S{boostTag}";
        }

        if (throttleText != null)
        {
            float percent = spacecraft.Throttle01 * (spacecraft.IsBoosting ? 160f : 100f);
            throttleText.text = $"THROTTLE: {percent:F0}%";
        }

        if (throttleBar != null)
        {
            throttleBar.fillAmount = spacecraft.Throttle01;
            throttleBar.color = spacecraft.IsBoosting ? new Color(1f, 0.5f, 0.1f) : new Color(0.2f, 0.7f, 1f);
        }

        // Camera mode display
        if (cameraModeText != null)
        {
            cameraModeText.text = spacecraft.IsInCockpitView
                ? "VIEW: FIRST-PERSON COCKPIT [V TO TOGGLE]"
                : "VIEW: THIRD-PERSON CHASE [V TO TOGGLE]";
        }
    }

    private void UpdateVitalsUI(float fuel, float hull)
    {
        // Fuel
        if (fuelText != null && spacecraft != null)
        {
            float pct = (fuel / spacecraft.maxFuel) * 100f;
            fuelText.text = $"FUEL: {pct:F0}%";
            fuelText.color = pct < 20f ? new Color(1f, 0.3f, 0.3f) : (pct < 50f ? new Color(1f, 0.8f, 0.2f) : new Color(0.35f, 0.9f, 1f));
        }

        if (fuelBar != null && spacecraft != null)
        {
            float fill = Mathf.Clamp01(fuel / spacecraft.maxFuel);
            fuelBar.fillAmount = fill;
            fuelBar.color = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.2f, 0.8f, 1f), fill);
        }

        // Hull
        if (hullText != null && spacecraft != null)
        {
            float pct = (hull / spacecraft.maxHull) * 100f;
            hullText.text = $"HULL INTEGRITY: {pct:F0}%";
            hullText.color = pct < 30f ? new Color(1f, 0.25f, 0.25f) : (pct < 65f ? new Color(1f, 0.85f, 0.2f) : new Color(0.35f, 1f, 0.55f));
        }

        if (hullBar != null && spacecraft != null)
        {
            float fill = Mathf.Clamp01(hull / spacecraft.maxHull);
            hullBar.fillAmount = fill;
            hullBar.color = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.25f, 1f, 0.45f), fill);
        }
    }

    private void UpdateDestinationMarkerUI()
    {
        if (destinationMarker == null || checkpointManager == null) return;

        Checkpoint activeCP = checkpointManager.ActiveCheckpoint;
        Camera currentCam = (spacecraft != null && !spacecraft.IsInCockpitView && spacecraft.chaseCamera != null)
            ? spacecraft.chaseCamera
            : (spacecraft != null && spacecraft.cockpitCamera != null ? spacecraft.cockpitCamera : flightCamera);

        if (activeCP == null || currentCam == null || !currentCam.gameObject.activeInHierarchy)
        {
            destinationMarker.gameObject.SetActive(false);
            return;
        }

        destinationMarker.gameObject.SetActive(true);

        // Project checkpoint 3D position to screen
        Vector3 targetWorldPos = activeCP.transform.position;
        Vector3 screenPos = currentCam.WorldToScreenPoint(targetWorldPos);

        bool isBehind = screenPos.z < 0;
        if (isBehind)
        {
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        // Clamp to screen bounds with margin
        float margin = 50f;
        screenPos.x = Mathf.Clamp(screenPos.x, margin, Screen.width - margin);
        screenPos.y = Mathf.Clamp(screenPos.y, margin, Screen.height - margin);

        destinationMarker.position = new Vector3(screenPos.x, screenPos.y, 0f);

        // Label and distance
        float dist = Vector3.Distance(spacecraft.transform.position, targetWorldPos);

        if (destinationMarkerLabel != null)
        {
            destinationMarkerLabel.text = activeCP.isFinalDestination ? "DESTINATION GATE" : activeCP.checkpointName.ToUpper();
            destinationMarkerLabel.color = activeCP.isFinalDestination ? new Color(1f, 0.85f, 0.25f) : new Color(0.35f, 0.9f, 1f);
        }

        if (destinationMarkerDistance != null)
        {
            destinationMarkerDistance.text = $"{dist:N0} M";
        }

        if (checkpointDistanceText != null)
        {
            checkpointDistanceText.text = $"TARGET DISTANCE: {dist:N0} M";
        }
    }

    private void UpdateObjectiveDisplay()
    {
        if (objectiveText == null || checkpointManager == null) return;

        Checkpoint active = checkpointManager.ActiveCheckpoint;
        if (active != null)
        {
            int current = checkpointManager.CurrentCheckpointIndex + 1;
            int total = checkpointManager.checkpoints.Count;

            objectiveText.text = active.isFinalDestination
                ? $"OBJECTIVE [{current}/{total}]: DOCK WITH LUNAR INSERTION DESTINATION GATE"
                : $"OBJECTIVE [{current}/{total}]: NAVIGATE THROUGH {active.checkpointName.ToUpper()}";
        }
        else
        {
            objectiveText.text = "MISSION OBJECTIVE: FLIGHT CORRIDOR CLEARED";
        }
    }

    private void HandleCheckpointCleared(Checkpoint cp, int index, int total)
    {
        UpdateObjectiveDisplay();
    }

    private void UpdateDeviationUI(bool isDeviating, float deviationDist, float secondsLeft)
    {
        if (deviationBanner == null) return;

        deviationBanner.SetActive(isDeviating);

        if (isDeviating)
        {
            if (deviationTitleText != null)
            {
                deviationTitleText.text = $"[!] TRAJECTORY DEVIATION: {deviationDist:F0}M OFF PLANNED CORRIDOR";
            }

            if (deviationTimerText != null)
            {
                deviationTimerText.text = $"RETURN TO CORRIDOR: 00:{Mathf.CeilToInt(secondsLeft):D2}S";
            }
        }
    }

    private void UpdateHazardUI(bool inHazard, HazardZone hz)
    {
        if (hazardBanner == null) return;

        hazardBanner.SetActive(inHazard);

        if (inHazard && hz != null && hazardTitleText != null)
        {
            hazardTitleText.text = $"[!] DANGER: PENETRATING {hz.hazardName.ToUpper()} // HULL DEGRADATION ACTIVE";
        }
    }

    private void ShowMissionFailedModal(string reason)
    {
        if (missionFailedModal != null)
        {
            missionFailedModal.SetActive(true);
            if (failureReasonText != null)
            {
                failureReasonText.text = reason;
            }
        }
    }

    private void ShowMissionSuccessModal()
    {
        if (missionSuccessModal != null)
        {
            missionSuccessModal.SetActive(true);
            if (successSummaryText != null)
            {
                float remainingFuel = spacecraft != null ? spacecraft.currentFuel : 100f;
                float remainingHull = spacecraft != null ? spacecraft.currentHull : 100f;

                successSummaryText.text = "TRANSLUNAR INSERTION TRAJECTORY CLEARED\n\n" +
                    $"• All Navigation Gates: Verified Passed\n" +
                    $"• Final Fuel Reserve: {remainingFuel:F1}%\n" +
                    $"• Final Hull Integrity: {remainingHull:F1}%\n\n" +
                    $"Spacecraft is now established in stable lunar approach trajectory.";
            }
        }
    }

    private void OnRetryFlightClicked()
    {
        string target = !string.IsNullOrEmpty(retrySceneName) ? retrySceneName : "LaunchSequence";
        if (Application.CanStreamedLevelBeLoaded(target))
        {
            SceneManager.LoadScene(target);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnContinueToLunarLandingClicked()
    {
        string nextScene = !string.IsNullOrEmpty(nextSceneName) ? nextSceneName : "LunarLanding";
        if (Application.CanStreamedLevelBeLoaded(nextScene))
        {
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            OnReturnToMissionsClicked();
        }
    }

    private void OnReturnToMissionsClicked()
    {
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

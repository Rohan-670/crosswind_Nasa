using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Exploration HUD controller:
/// manages oxygen reserve gauges, mission timers, interactive checklist updates,
/// interaction prompts, scanning reticle overlays, photo mode viewfinders,
/// and expedition completion/failure modals.
/// </summary>
public class ExplorationHUDController : MonoBehaviour
{
    [Header("--- Target Systems ---")]
    public ExplorationManager explorationManager;
    public AstronautScanner scanner;
    public PhotoCaptureSystem photoSystem;
    public AstronautController astronaut;

    [Header("--- Life Support & Clock ---")]
    public TextMeshProUGUI oxygenText;
    public Image oxygenBar;
    public TextMeshProUGUI missionTimerText;

    [Header("--- Objective Checklist ---")]
    public TextMeshProUGUI samplesObjectiveText;
    public TextMeshProUGUI anomalyObjectiveText;
    public TextMeshProUGUI photoObjectiveText;
    public TextMeshProUGUI returnLanderObjectiveText;

    [Header("--- Center Interaction Prompt ---")]
    public GameObject interactionPromptPanel;
    public TextMeshProUGUI interactionPromptText;

    [Header("--- Scanner Reticle Overlay ---")]
    public GameObject scanOverlay;
    public Image scanProgressRing;
    public TextMeshProUGUI scanTargetText;
    public TextMeshProUGUI scanAnalysisText;

    [Header("--- Photo Mode Viewfinder ---")]
    public GameObject photoViewfinderOverlay;
    public TextMeshProUGUI photoTargetTagText;
    public Image photoShutterFlash;

    [Header("--- Modals ---")]
    public GameObject missionSuccessModal;
    public TextMeshProUGUI successStatsText;
    public Button successContinueButton;

    public GameObject missionFailedModal;
    public TextMeshProUGUI failureReasonText;
    public Button failureRetryButton;
    public Button failureReturnButton;

    private SampleObject currentNearbySample = null;

    [Header("--- Transition ---")]
    [Tooltip("Target scene loaded when continuing upon exploration mission success")]
    public string returnFlightSceneName = "EarthReturnFlight";

    private void Start()
    {
        if (interactionPromptPanel != null) interactionPromptPanel.SetActive(false);
        if (scanOverlay != null) scanOverlay.SetActive(false);
        if (photoViewfinderOverlay != null) photoViewfinderOverlay.SetActive(false);
        if (missionSuccessModal != null) missionSuccessModal.SetActive(false);
        if (missionFailedModal != null) missionFailedModal.SetActive(false);

        // Wire buttons
        if (failureRetryButton != null)
        {
            failureRetryButton.onClick.RemoveAllListeners();
            failureRetryButton.onClick.AddListener(OnRetryClicked);
        }

        if (failureReturnButton != null)
        {
            failureReturnButton.onClick.RemoveAllListeners();
            failureReturnButton.onClick.AddListener(OnReturnToMissionsClicked);
        }

        if (successContinueButton != null)
        {
            successContinueButton.onClick.RemoveAllListeners();
            successContinueButton.onClick.AddListener(OnContinueToReturnFlightClicked);
        }

        // Bind events
        if (explorationManager != null)
        {
            explorationManager.OnTelemetryUpdated += UpdateTelemetryUI;
            explorationManager.OnObjectiveStateChanged += UpdateChecklistUI;
            explorationManager.OnMissionSuccess += ShowSuccessModal;
            explorationManager.OnMissionFailed += ShowFailedModal;
        }

        if (scanner != null)
        {
            scanner.OnScanProgressUpdated += UpdateScanOverlay;
            scanner.OnAnomalyScanCompleted += HandleAnomalyScanned;
        }

        if (photoSystem != null)
        {
            photoSystem.OnPhotoModeToggled += TogglePhotoModeUI;
            photoSystem.OnPhotoCaptured += TriggerShutterFlash;
        }

        UpdateChecklistUI();
        UpdateTelemetryUI(100f, explorationManager != null ? explorationManager.missionDurationSeconds : 480f);
    }

    private void OnDestroy()
    {
        if (explorationManager != null)
        {
            explorationManager.OnTelemetryUpdated -= UpdateTelemetryUI;
            explorationManager.OnObjectiveStateChanged -= UpdateChecklistUI;
            explorationManager.OnMissionSuccess -= ShowSuccessModal;
            explorationManager.OnMissionFailed -= ShowFailedModal;
        }

        if (scanner != null)
        {
            scanner.OnScanProgressUpdated -= UpdateScanOverlay;
            scanner.OnAnomalyScanCompleted -= HandleAnomalyScanned;
        }

        if (photoSystem != null)
        {
            photoSystem.OnPhotoModeToggled -= TogglePhotoModeUI;
            photoSystem.OnPhotoCaptured -= TriggerShutterFlash;
        }
    }

    private void Update()
    {
        CheckNearbySampleInteraction();
        UpdatePhotoModeSubjectTag();
    }

    private void UpdateTelemetryUI(float oxygen, float timeRemaining)
    {
        // 1. Oxygen
        if (oxygenText != null)
        {
            oxygenText.text = $"OXYGEN: {oxygen:F0}%";
            oxygenText.color = oxygen < 25f ? new Color(1f, 0.3f, 0.3f) : (oxygen < 50f ? new Color(1f, 0.8f, 0.2f) : new Color(0.35f, 0.9f, 1f));
        }

        if (oxygenBar != null && explorationManager != null)
        {
            float fill = Mathf.Clamp01(oxygen / explorationManager.maxOxygen);
            oxygenBar.fillAmount = fill;
            oxygenBar.color = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.2f, 0.8f, 1f), fill);
        }

        // 2. Timer
        if (missionTimerText != null)
        {
            int mins = Mathf.FloorToInt(timeRemaining / 60f);
            int secs = Mathf.FloorToInt(timeRemaining % 60f);
            missionTimerText.text = $"EVA TIMER: {mins:D2}:{secs:D2}";
        }
    }

    private void UpdateChecklistUI()
    {
        if (explorationManager == null) return;

        // 1. Samples
        if (samplesObjectiveText != null)
        {
            bool done = explorationManager.CollectedSampleCount >= explorationManager.requiredSampleCount;
            string check = done ? "<color=#40FF70>[OK]</color>" : "[  ]";
            samplesObjectiveText.text = $"{check} Geological Regolith Samples: ({explorationManager.CollectedSampleCount}/{explorationManager.requiredSampleCount})";
        }

        // 2. Anomaly Scan
        if (anomalyObjectiveText != null)
        {
            string check = explorationManager.AnomalyScanned ? "<color=#40FF70>[OK]</color>" : "[  ]";
            anomalyObjectiveText.text = $"{check} Multispectral Anomaly Scan";
        }

        // 3. Photo
        if (photoObjectiveText != null)
        {
            string check = explorationManager.PhotoCaptured ? "<color=#40FF70>[OK]</color>" : "[  ]";
            photoObjectiveText.text = $"{check} Photograph Earthrise from Surface";
        }

        // 4. Return to Lander
        if (returnLanderObjectiveText != null)
        {
            bool ready = explorationManager.AllPrimaryObjectivesMet;
            string check = explorationManager.IsMissionCompleted ? "<color=#40FF70>[OK]</color>" : "[  ]";
            returnLanderObjectiveText.text = ready
                ? $"{check} <color=#FFD040>RETURN TO LUNAR LANDER [INGRESS READY]</color>"
                : $"{check} Return to Lunar Lander (Pending Objectives)";
        }
    }

    private void CheckNearbySampleInteraction()
    {
        if (astronaut == null) return;

        Vector3 pos = astronaut.transform.position;
        SampleObject[] allSamples = Object.FindObjectsByType<SampleObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        SampleObject nearest = null;
        float minDist = 3.5f;

        foreach (SampleObject s in allSamples)
        {
            if (s != null && s.CanInteract(pos))
            {
                float d = Vector3.Distance(pos, s.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = s;
                }
            }
        }

        currentNearbySample = nearest;

        if (currentNearbySample != null)
        {
            if (interactionPromptPanel != null) interactionPromptPanel.SetActive(true);
            if (interactionPromptText != null)
            {
                interactionPromptText.text = $"[E] COLLECT {currentNearbySample.sampleName.ToUpper()}";
            }

            if (AstroInput.GetKeyDown(KeyCode.E))
            {
                currentNearbySample.Collect();
                if (interactionPromptPanel != null) interactionPromptPanel.SetActive(false);
            }
        }
        else if (explorationManager != null && explorationManager.AllPrimaryObjectivesMet && explorationManager.landerReturnZone != null && !explorationManager.IsMissionCompleted && (explorationManager.landerFX == null || !explorationManager.landerFX.IsAscending))
        {
            float distToLander = Vector3.Distance(pos, explorationManager.landerReturnZone.position);
            if (distToLander <= explorationManager.landerReturnRadius + 3.5f)
            {
                if (interactionPromptPanel != null) interactionPromptPanel.SetActive(true);
                if (interactionPromptText != null)
                {
                    interactionPromptText.text = "[E] BOARD LUNAR LANDER // INITIATE RETURN FLIGHT";
                }

                if (AstroInput.GetKeyDown(KeyCode.E) || distToLander <= explorationManager.landerReturnRadius)
                {
                    if (interactionPromptPanel != null) interactionPromptPanel.SetActive(false);
                    explorationManager.BoardLander();
                }
            }
            else
            {
                if (interactionPromptPanel != null) interactionPromptPanel.SetActive(false);
            }
        }
        else
        {
            if (interactionPromptPanel != null) interactionPromptPanel.SetActive(false);
        }
    }

    private void UpdateScanOverlay(ScannableAnomaly anomaly, float progress01)
    {
        if (scanOverlay == null) return;

        bool active = scanner != null && scanner.IsScanning;
        scanOverlay.SetActive(active);

        if (active)
        {
            if (scanProgressRing != null) scanProgressRing.fillAmount = progress01;

            if (scanTargetText != null)
            {
                scanTargetText.text = anomaly != null ? $"ANALYZING: {anomaly.anomalyName.ToUpper()}" : "SEARCHING FOR SPECTRAL SIGNATURE...";
            }

            if (scanAnalysisText != null)
            {
                scanAnalysisText.text = anomaly != null ? $"SCAN PROGRESS: {progress01 * 100f:F0}%" : "HOLD [F] TOWARD GEOLOGICAL ANOMALY";
            }
        }
    }

    private void HandleAnomalyScanned(ScannableAnomaly anomaly)
    {
        if (scanAnalysisText != null && anomaly != null)
        {
            scanAnalysisText.text = $"[SCAN COMPLETE]\n<color=#88DDAA>{anomaly.telemetryAnalysis}</color>";
        }
    }

    private void TogglePhotoModeUI(bool inPhotoMode)
    {
        if (photoViewfinderOverlay != null)
            photoViewfinderOverlay.SetActive(inPhotoMode);
    }

    private void UpdatePhotoModeSubjectTag()
    {
        if (photoSystem != null && photoSystem.IsInPhotoMode && photoTargetTagText != null)
        {
            string target = photoSystem.CurrentTargetInFrame;
            if (!string.IsNullOrEmpty(target))
            {
                photoTargetTagText.text = $"TARGET LOCKED: {target}";
                photoTargetTagText.color = new Color(0.35f, 1f, 0.55f);
            }
            else
            {
                photoTargetTagText.text = "AIM AT EARTH IN SKY OR LUNAR LANDER";
                photoTargetTagText.color = new Color(0.85f, 0.88f, 0.95f);
            }
        }
    }

    private void TriggerShutterFlash(string targetName)
    {
        StartCoroutine(ShutterFlashRoutine());
    }

    private IEnumerator ShutterFlashRoutine()
    {
        if (photoShutterFlash != null)
        {
            photoShutterFlash.gameObject.SetActive(true);
            photoShutterFlash.color = new Color(1f, 1f, 1f, 0.85f);

            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.85f, 0f, elapsed / duration);
                photoShutterFlash.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            photoShutterFlash.gameObject.SetActive(false);
        }
    }

    private void ShowSuccessModal()
    {
        if (missionSuccessModal != null)
        {
            missionSuccessModal.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (successStatsText != null && explorationManager != null)
            {
                float remainingO2 = explorationManager.CurrentOxygen;
                int samples = explorationManager.CollectedSampleCount;
                successStatsText.text = "LUNAR SURFACE EXPEDITION ACCOMPLISHED\n\n" +
                    $"• Geological Samples Secured: {samples}/{explorationManager.requiredSampleCount}\n" +
                    $"• Subsurface Volatiles: Analyzed and Cataloged\n" +
                    $"• Surface Photography: Transmitted to Mission Control\n" +
                    $"• Spacesuit Oxygen Reserve Remaining: {remainingO2:F1}%\n\n" +
                    $"Crew ingress into the Lunar Lander confirmed. Ready to initiate lunar ascent and return to Earth.";
            }
        }
    }

    private void ShowFailedModal(string reason)
    {
        if (missionFailedModal != null)
        {
            missionFailedModal.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (failureReasonText != null)
            {
                failureReasonText.text = reason;
            }
        }
    }

    private void OnRetryClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnContinueToReturnFlightClicked()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!string.IsNullOrEmpty(returnFlightSceneName) && Application.CanStreamedLevelBeLoaded(returnFlightSceneName))
        {
            SceneManager.LoadScene(returnFlightSceneName);
        }
        else
        {
            OnReturnToMissionsClicked();
        }
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

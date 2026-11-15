using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Debriefing screen controller for ASTROBASE Mission Results:
/// displays comprehensive expedition telemetry:
/// - Mission completion status
/// - Navigation accuracy
/// - Landing performance
/// - Exploration objectives
/// - Number of samples collected
/// - Number of photographs
/// - Fuel remaining
/// - Three-star rating
/// - Replay button
/// </summary>
public class MissionResultsUI : MonoBehaviour
{
    [Header("--- Header & Mission Status ---")]
    public TextMeshProUGUI missionTitleText;
    public TextMeshProUGUI missionSubtitleText;
    public TextMeshProUGUI overallStatusBadgeText;
    public Image statusBadgeBackground;

    [Header("--- Three-Star Rating System ---")]
    public TextMeshProUGUI starRatingIconsText;
    public TextMeshProUGUI starRatingCommentText;
    public TextMeshProUGUI starRequirementsBreakdownText;

    [Header("--- Flight & Navigation Telemetry ---")]
    public TextMeshProUGUI navigationAccuracyText;
    public TextMeshProUGUI landingPerformanceText;
    public TextMeshProUGUI fuelRemainingText;
    public Image fuelRemainingBar;

    [Header("--- Surface Exploration & Science ---")]
    public TextMeshProUGUI explorationObjectivesText;
    public TextMeshProUGUI samplesCollectedText;
    public TextMeshProUGUI photographsCapturedText;

    [Header("--- Navigation Actions ---")]
    public Button replayMissionButton;
    public Button returnToMissionSelectionButton;

    [Header("--- Target Scenes ---")]
    public string missionSelectionSceneName = "MissionSelection";
    public string missionPreparationSceneName = "MissionPreparation";

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (replayMissionButton != null)
        {
            replayMissionButton.onClick.RemoveAllListeners();
            replayMissionButton.onClick.AddListener(OnReplayMissionClicked);
        }

        if (returnToMissionSelectionButton != null)
        {
            returnToMissionSelectionButton.onClick.RemoveAllListeners();
            returnToMissionSelectionButton.onClick.AddListener(OnReturnToMissionSelectionClicked);
        }

        PopulateDebriefingData();
    }

    public void PopulateDebriefingData()
    {
        ExpeditionResults data = null;
        if (MissionManager.Instance != null && MissionManager.Instance.CurrentResults != null)
        {
            data = MissionManager.Instance.CurrentResults;
        }
        else
        {
            // Default nominal results for standalone scene testing
            data = new ExpeditionResults();
            data.CalculateStarRating();
        }

        // 1. Mission Title & Subtitle
        if (missionTitleText != null)
        {
            missionTitleText.text = data.missionTitle;
        }

        if (missionSubtitleText != null)
        {
            missionSubtitleText.text = "MISSION DESIGNATION: APOLLO-NEXT // NASA FLIGHT CONTROL & KSC RECOVERY FACILITY";
        }

        // 2. Mission Completion Status
        if (overallStatusBadgeText != null)
        {
            if (data.isMissionSuccess)
            {
                overallStatusBadgeText.text = "[SUCCESS] MISSION ACCOMPLISHED // RETURN TO EARTH CONFIRMED";
                overallStatusBadgeText.color = new Color(0.35f, 1.0f, 0.55f);
                if (statusBadgeBackground != null)
                    statusBadgeBackground.color = new Color(0.05f, 0.22f, 0.12f, 0.95f);
            }
            else
            {
                overallStatusBadgeText.text = $"[FAILED] MISSION FAILED // {(!string.IsNullOrEmpty(data.failureReason) ? data.failureReason : "ABORTED")}";
                overallStatusBadgeText.color = new Color(1.0f, 0.35f, 0.35f);
                if (statusBadgeBackground != null)
                    statusBadgeBackground.color = new Color(0.24f, 0.05f, 0.05f, 0.95f);
            }
        }

        // 3. Three-Star Rating Display
        if (starRatingIconsText != null)
        {
            // ASCII gold star symbols [ * ] for earned stars, [ - ] for unearned
            string s1 = data.starRating >= 1 ? "<color=#FFD700>[ * ]</color>" : "<color=#384555>[ - ]</color>";
            string s2 = data.starRating >= 2 ? "<color=#FFD700>[ * ]</color>" : "<color=#384555>[ - ]</color>";
            string s3 = data.starRating >= 3 ? "<color=#FFD700>[ * ]</color>" : "<color=#384555>[ - ]</color>";
            starRatingIconsText.text = $"{s1}   {s2}   {s3}";
        }

        if (starRatingCommentText != null)
        {
            starRatingCommentText.text = data.starRatingComment;
            starRatingCommentText.color = data.starRating == 3 ? new Color(1f, 0.88f, 0.35f) : (data.starRating >= 1 ? new Color(0.6f, 0.88f, 1f) : new Color(1f, 0.4f, 0.4f));
        }

        if (starRequirementsBreakdownText != null)
        {
            string s1Done = data.starRating >= 1 ? "<color=#55FF88>[OK] STAR 1:</color>" : "<color=#888888>[  ] STAR 1:</color>";
            string s2Done = data.starRating >= 2 ? "<color=#55FF88>[OK] STAR 2:</color>" : "<color=#888888>[  ] STAR 2:</color>";
            string s3Done = data.starRating >= 3 ? "<color=#55FF88>[OK] STAR 3:</color>" : "<color=#888888>[  ] STAR 3:</color>";

            starRequirementsBreakdownText.text =
                $"{s1Done} <b>Safe Return:</b> Land on Moon, complete EVA, and return spacecraft to Earth\n" +
                $"{s2Done} <b>Precision Flight:</b> Navigation accuracy >= 80% & Soft Touchdown (<= 6.5 m/s)\n" +
                $"{s3Done} <b>Mastery:</b> 100% Samples & Photography + Remaining Fuel >= 20%";
        }

        // 4. Navigation Accuracy
        if (navigationAccuracyText != null)
        {
            string grade = data.navigationAccuracy >= 90f ? "<color=#55FF88>[OPTIMAL CORRIDOR]</color>" : (data.navigationAccuracy >= 75f ? "<color=#FFD040>[ACCEPTABLE]</color>" : "<color=#FF5555>[EXCESSIVE DEVIATION]</color>");
            navigationAccuracyText.text =
                $"- <b>Corridor Adherence:</b> {data.navigationAccuracy:F1}% {grade}\n" +
                $"- <b>Trans-Earth Waypoints:</b> {data.checkpointsCleared} of {data.totalCheckpoints} Cleared\n" +
                $"- <b>Deep Space Hazards:</b> Debris Belt & Radiation Storm Evaded\n" +
                $"- <b>Re-entry Corridor:</b> +10.0° Optimal Angle Maintained";
        }

        // 5. Landing Performance
        if (landingPerformanceText != null)
        {
            string sinkCol = data.touchdownSinkRate <= 4.0f ? "#55FF88" : (data.touchdownSinkRate <= 6.5f ? "#FFD040" : "#FF4444");
            landingPerformanceText.text =
                $"- <b>Touchdown Sink Rate:</b> <color={sinkCol}>{data.touchdownSinkRate:F1} m/s</color> (Safety Limit: 6.5 m/s)\n" +
                $"- <b>Touchdown Airspeed:</b> {data.touchdownAirspeed:F1} m/s (Runway Max: 45.0 m/s)\n" +
                $"- <b>Runway Alignment:</b> Centerline Nominal // Kennedy Space Center\n" +
                $"- <b>Landing Evaluation:</b> <b>{data.landingGrade}</b>";
        }

        // 6. Fuel Remaining
        if (fuelRemainingText != null)
        {
            fuelRemainingText.text = $"REMAINING FLIGHT FUEL: {data.fuelRemaining:F1}%";
            fuelRemainingText.color = data.fuelRemaining >= 25f ? new Color(0.35f, 0.9f, 1f) : new Color(1f, 0.4f, 0.3f);
        }

        if (fuelRemainingBar != null)
        {
            float fill = Mathf.Clamp01(data.fuelRemaining / 100f);
            fuelRemainingBar.fillAmount = fill;
            fuelRemainingBar.color = Color.Lerp(new Color(1f, 0.25f, 0.2f), new Color(0.25f, 0.85f, 1f), fill);
        }

        // 7. Exploration Objectives
        if (explorationObjectivesText != null)
        {
            string sDone = data.samplesCollected >= data.totalSamples ? "<color=#55FF88>[OK]</color>" : "<color=#FFD040>[~]</color>";
            string pDone = data.photographsCaptured >= data.totalPhotographs ? "<color=#55FF88>[OK]</color>" : "<color=#FF5555>[--]</color>";
            string aDone = data.anomalyScanned ? "<color=#55FF88>[OK]</color>" : "<color=#FF5555>[--]</color>";
            string lDone = data.landerBoarded ? "<color=#55FF88>[OK]</color>" : "<color=#FF5555>[--]</color>";

            explorationObjectivesText.text =
                $"{sDone} <b>Geological Sampling:</b> {data.samplesCollected} / {data.totalSamples} Core Canisters Secured\n" +
                $"{aDone} <b>Multispectral Anomaly:</b> Subsurface Volatiles Fissure Analyzed\n" +
                $"{pDone} <b>Photographic Survey:</b> Earthrise Imagery Telemetry Archived\n" +
                $"{lDone} <b>Lander Ingress:</b> Crew Ingress & Trans-Earth Ascent Executed";
        }

        // 8. Number of Samples Collected
        if (samplesCollectedText != null)
        {
            samplesCollectedText.text =
                $"<b>SAMPLES COLLECTED: {data.samplesCollected} / {data.totalSamples}</b>\n" +
                "<color=#55FF88>[OK]</color> <b>Regolith Core Alpha:</b> 2.4 kg lunar highland regolith\n" +
                "<color=#55FF88>[OK]</color> <b>Volatile Ice Beta:</b> Cryogenic cylinder (Trace H2O)\n" +
                "<color=#55FF88>[OK]</color> <b>Basalt Crust Gamma:</b> High-Ti mare breccia cataloged";
        }

        // 9. Number of Photographs
        if (photographsCapturedText != null)
        {
            photographsCapturedText.text =
                $"<b>PHOTOGRAPHS CAPTURED: {data.photographsCaptured} / {data.totalPhotographs}</b>\n" +
                "<color=#55FF88>[OK]</color> <b>Earthrise from Lunar Surface:</b> High-resolution optical frame archived for NASA planetary science analysis.";
        }
    }

    private void OnReturnToMissionSelectionClicked()
    {
        if (Application.CanStreamedLevelBeLoaded(missionSelectionSceneName))
        {
            SceneManager.LoadScene(missionSelectionSceneName);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    private void OnReplayMissionClicked()
    {
        if (Application.CanStreamedLevelBeLoaded(missionPreparationSceneName))
        {
            SceneManager.LoadScene(missionPreparationSceneName);
        }
        else if (Application.CanStreamedLevelBeLoaded("LaunchSequence"))
        {
            SceneManager.LoadScene("LaunchSequence");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }
}

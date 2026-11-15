using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Serializable container holding telemetry and scoring data for the completed mission.
/// Evaluates navigation accuracy, landing performance, exploration objectives,
/// sample counts, photographs, remaining fuel, and three-star rating.
/// </summary>
[System.Serializable]
public class ExpeditionResults
{
    public bool isMissionSuccess = true;
    public string missionTitle = "LUNAR EXPEDITION: TRANSLUNAR & RETURN TO EARTH";
    public string failureReason = "";

    // 1. Navigation Accuracy
    public float navigationAccuracy = 98.4f; // %
    public int checkpointsCleared = 5;
    public int totalCheckpoints = 5;

    // 2. Landing Performance
    public float touchdownSinkRate = 2.3f; // m/s
    public float touchdownAirspeed = 36.8f; // m/s
    public bool isSafeLanding = true;
    public string landingGrade = "EXCELLENT // SOFT TOUCHDOWN";

    // 3. Exploration Objectives
    public int samplesCollected = 3;
    public int totalSamples = 3;
    public int photographsCaptured = 1;
    public int totalPhotographs = 1;
    public bool anomalyScanned = true;
    public bool landerBoarded = true;

    // 4. Resources
    public float fuelRemaining = 68.5f; // %

    // 5. Star Rating (1, 2, or 3 stars)
    public int starRating = 3;
    public string starRatingComment = "[***] THREE-STAR EXPEDITION // EXEMPLARY NASA FLIGHT COMMENDATION";

    public void CalculateStarRating()
    {
        if (!isMissionSuccess)
        {
            starRating = 0;
            starRatingComment = "MISSION INCOMPLETE // EXPEDITION ABORTED";
            return;
        }

        int stars = 1; // 1 Star: Mission completed and returned safely

        // 2nd Star: Good flight navigation & safe touchdown
        if (navigationAccuracy >= 80f && isSafeLanding && touchdownSinkRate <= 6.5f)
        {
            stars++;
        }

        // 3rd Star: Full exploration (samples + photo + scan) and fuel efficiency (>= 20%)
        if (samplesCollected >= totalSamples && photographsCaptured >= totalPhotographs && anomalyScanned && fuelRemaining >= 20f)
        {
            stars++;
        }

        starRating = stars;
        if (starRating == 3)
            starRatingComment = "[***] THREE-STAR EXPEDITION // EXEMPLARY NASA FLIGHT COMMENDATION";
        else if (starRating == 2)
            starRatingComment = "[**-] TWO-STAR EXPEDITION // OUTSTANDING FLIGHT & SAFE RECOVERY";
        else
            starRatingComment = "[*--] ONE-STAR EXPEDITION // BASIC EXPEDITION SUCCESS WITH NOTED DEFICIENCIES";
    }
}

/// <summary>
/// Reusable, persistent manager that stores active mission selections,
/// records runtime telemetry, and manages transitions across ASTROBASE scenes.
/// </summary>
public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("--- Active Mission ---")]
    [SerializeField]
    private MissionDefinition selectedMission;

    public MissionDefinition SelectedMission => selectedMission;

    [Header("--- Expedition Results Data ---")]
    [SerializeField]
    private ExpeditionResults currentResults = new ExpeditionResults();
    public ExpeditionResults CurrentResults => currentResults;

    public event Action<MissionDefinition> OnMissionSelected;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectMission(MissionDefinition mission)
    {
        if (mission == null) return;

        selectedMission = mission;
        Debug.Log($"[MissionManager] Selected mission: '{mission.missionName}' (ID: {mission.missionId})");

        OnMissionSelected?.Invoke(mission);
    }

    public void RecordExplorationProgress(int samples, int maxSamples, int photos, int maxPhotos, bool anomaly)
    {
        currentResults.samplesCollected = samples;
        currentResults.totalSamples = maxSamples;
        currentResults.photographsCaptured = photos;
        currentResults.totalPhotographs = maxPhotos;
        currentResults.anomalyScanned = anomaly;
        currentResults.landerBoarded = true;
    }

    public void RecordReturnFlightResults(bool success, float navAccuracy, int cpCleared, int totalCp, float sinkRate, float airspeed, bool safeLanding, float remainingFuel, string failReason = "")
    {
        currentResults.isMissionSuccess = success;
        currentResults.navigationAccuracy = Mathf.Clamp(navAccuracy, 0f, 100f);
        currentResults.checkpointsCleared = cpCleared;
        currentResults.totalCheckpoints = totalCp;
        currentResults.touchdownSinkRate = sinkRate;
        currentResults.touchdownAirspeed = airspeed;
        currentResults.isSafeLanding = safeLanding;
        currentResults.landingGrade = safeLanding ? (sinkRate < 3.5f ? "EXCELLENT // SOFT TOUCHDOWN" : "GOOD // NOMINAL TOUCHDOWN") : "HARD IMPACT // STRUCTURAL OVERLOAD";
        currentResults.fuelRemaining = Mathf.Clamp(remainingFuel, 0f, 100f);
        currentResults.failureReason = failReason;
        currentResults.CalculateStarRating();
    }

    public void ProceedToPreparation(string fallbackScene = "MissionPreparation")
    {
        if (selectedMission == null)
        {
            Debug.LogWarning("[MissionManager] Cannot proceed: no mission currently selected.");
            return;
        }

        string sceneToLoad = !string.IsNullOrEmpty(selectedMission.targetSceneName)
            ? selectedMission.targetSceneName
            : fallbackScene;

        Debug.Log($"[MissionManager] Proceeding with '{selectedMission.missionName}' to scene: {sceneToLoad}");

        if (Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning($"[MissionManager] Scene '{sceneToLoad}' is not loaded or not in Build Settings.");
        }
    }
}

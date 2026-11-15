using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Central manager for planetary surface exploration:
/// tracks mission timer, life support oxygen decay, sample collection count,
/// scanning and photography objectives, and handles mission victory/failure.
/// Modularized for seamless reuse on Mars expeditions.
/// </summary>
public class ExplorationManager : MonoBehaviour
{
    public static ExplorationManager Instance { get; private set; }

    [Header("--- Target References ---")]
    public AstronautController astronaut;
    public AstronautScanner scanner;
    public PhotoCaptureSystem photoSystem;
    public Transform landerReturnZone;
    public float landerReturnRadius = 6.0f;
    public LanderProximityAndAscentFX landerFX;

    [Header("--- Life Support & Mission Clock ---")]
    [Tooltip("Initial oxygen supply percentage (100)")]
    public float maxOxygen = 100f;
    [Tooltip("Rate of oxygen depletion per second (e.g. 0.12% / sec)")]
    public float oxygenDepletionRate = 0.12f;
    [Tooltip("Mission exploration duration countdown in seconds (e.g. 480s = 8 mins)")]
    public float missionDurationSeconds = 480f;

    [Header("--- Objective Targets ---")]
    public int requiredSampleCount = 3;
    public bool requireAnomalyScan = true;
    public bool requireEarthrisePhoto = true;

    // Runtime state
    public float CurrentOxygen { get; private set; }
    public float MissionTimeRemaining { get; private set; }
    public int CollectedSampleCount { get; private set; } = 0;
    public bool AnomalyScanned { get; private set; } = false;
    public bool PhotoCaptured { get; private set; } = false;
    public bool IsMissionCompleted { get; private set; } = false;
    public bool IsOxygenDepleted { get; private set; } = false;

    public bool AllPrimaryObjectivesMet =>
        CollectedSampleCount >= requiredSampleCount &&
        (!requireAnomalyScan || AnomalyScanned) &&
        (!requireEarthrisePhoto || PhotoCaptured);

    public event Action<float, float> OnTelemetryUpdated; // oxygen, timeRemaining
    public event Action OnObjectiveStateChanged;
    public event Action OnMissionSuccess;
    public event Action<string> OnMissionFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        CurrentOxygen = maxOxygen;
        MissionTimeRemaining = missionDurationSeconds;

        SampleObject.OnSampleCollectedGlobal += HandleSampleCollected;

        if (scanner != null)
            scanner.OnAnomalyScanCompleted += HandleAnomalyScanned;

        if (photoSystem != null)
            photoSystem.OnPhotoCaptured += HandlePhotoCaptured;
    }

    private void OnDestroy()
    {
        SampleObject.OnSampleCollectedGlobal -= HandleSampleCollected;

        if (scanner != null)
            scanner.OnAnomalyScanCompleted -= HandleAnomalyScanned;

        if (photoSystem != null)
            photoSystem.OnPhotoCaptured -= HandlePhotoCaptured;
    }

    private void Update()
    {
        if (IsMissionCompleted || IsOxygenDepleted) return;

        UpdateMissionClocks();
        CheckLanderReturn();
    }

    private void UpdateMissionClocks()
    {
        float dt = Time.deltaTime;

        // 1. Oxygen depletion
        CurrentOxygen = Mathf.Max(0f, CurrentOxygen - oxygenDepletionRate * dt);

        // 2. Timer countdown
        MissionTimeRemaining = Mathf.Max(0f, MissionTimeRemaining - dt);

        OnTelemetryUpdated?.Invoke(CurrentOxygen, MissionTimeRemaining);

        // Check failure
        if (CurrentOxygen <= 0f)
        {
            IsOxygenDepleted = true;
            if (astronaut != null) astronaut.SetControlsLocked(true);
            OnMissionFailed?.Invoke("LIFE SUPPORT DEPLETED // SPACESUIT OXYGEN EXHAUSTED");
        }
        else if (MissionTimeRemaining <= 0f)
        {
            IsOxygenDepleted = true;
            if (astronaut != null) astronaut.SetControlsLocked(true);
            OnMissionFailed?.Invoke("MISSION TIME EXPIRED // EVA TIMEOUT LIMIT REACHED");
        }
    }

    private void CheckLanderReturn()
    {
        if (!AllPrimaryObjectivesMet || landerReturnZone == null || astronaut == null) return;
        if (landerFX != null && landerFX.IsAscending) return;

        float distToLander = Vector3.Distance(astronaut.transform.position, landerReturnZone.position);
        if (distToLander <= landerReturnRadius)
        {
            BoardLander();
        }
    }

    private void HandleSampleCollected(SampleObject sample)
    {
        CollectedSampleCount++;
        Debug.Log($"[ExplorationManager] Sample collected ({CollectedSampleCount}/{requiredSampleCount}): {sample.sampleName}");
        OnObjectiveStateChanged?.Invoke();
    }

    private void HandleAnomalyScanned(ScannableAnomaly anomaly)
    {
        AnomalyScanned = true;
        Debug.Log($"[ExplorationManager] Anomaly scanned: {anomaly.anomalyName}");
        OnObjectiveStateChanged?.Invoke();
    }

    private void HandlePhotoCaptured(string targetName)
    {
        PhotoCaptured = true;
        Debug.Log($"[ExplorationManager] Photography target verified: {targetName}");
        OnObjectiveStateChanged?.Invoke();
    }

    public void TriggerMissionSuccess()
    {
        if (IsMissionCompleted) return;
        IsMissionCompleted = true;

        if (astronaut != null) astronaut.SetControlsLocked(true);
        Debug.Log("[ExplorationManager] All exploration objectives cleared and returned to Lunar Lander!");

        if (MissionManager.Instance != null)
        {
            int photos = PhotoCaptured ? 1 : 0;
            MissionManager.Instance.RecordExplorationProgress(CollectedSampleCount, requiredSampleCount, photos, 1, AnomalyScanned);
        }

        OnMissionSuccess?.Invoke();
    }

    public void BoardLander()
    {
        if (!AllPrimaryObjectivesMet)
        {
            Debug.LogWarning("[ExplorationManager] Cannot board lander yet - primary objectives pending.");
            return;
        }

        if (landerFX != null && !landerFX.IsAscending)
        {
            if (astronaut != null) astronaut.SetControlsLocked(true);
            landerFX.PlayAscentSequence(() =>
            {
                TriggerMissionSuccess();
            });
        }
        else
        {
            TriggerMissionSuccess();
        }
    }
}

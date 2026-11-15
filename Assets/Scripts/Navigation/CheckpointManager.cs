using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Reusable CheckpointManager:
/// manages the ordered sequence of navigation checkpoints and the final destination beacon,
/// handles pass-through validation, fuel bonuses, and triggers mission completion.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("--- Checkpoint Sequence ---")]
    public List<Checkpoint> checkpoints = new List<Checkpoint>();

    [Header("--- Target Spacecraft ---")]
    public SpacecraftController spacecraft;

    [Header("--- Rewards on Clearance ---")]
    public float fuelBonusPerCheckpoint = 15f;

    // Runtime state
    public int CurrentCheckpointIndex { get; private set; } = 0;
    public Checkpoint ActiveCheckpoint => (CurrentCheckpointIndex >= 0 && CurrentCheckpointIndex < checkpoints.Count) ? checkpoints[CurrentCheckpointIndex] : null;
    public Checkpoint DestinationCheckpoint => checkpoints.Count > 0 ? checkpoints[checkpoints.Count - 1] : null;
    public bool IsMissionCompleted { get; private set; } = false;

    public event Action<Checkpoint, int, int> OnCheckpointCleared; // checkpoint, index, total
    public event Action OnAllCheckpointsCleared;

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
        InitializeCheckpoints();
    }

    private void Update()
    {
        if (spacecraft == null || !spacecraft.IsAlive || IsMissionCompleted) return;

        CheckProximityToActiveCheckpoint();
    }

    public void InitializeCheckpoints()
    {
        CurrentCheckpointIndex = 0;
        IsMissionCompleted = false;

        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] != null)
            {
                checkpoints[i].checkpointIndex = i;
                checkpoints[i].isFinalDestination = (i == checkpoints.Count - 1);
                checkpoints[i].SetState(i == 0 ? Checkpoint.CheckpointState.Active : Checkpoint.CheckpointState.Pending);
            }
        }
    }

    private void CheckProximityToActiveCheckpoint()
    {
        Checkpoint current = ActiveCheckpoint;
        if (current == null) return;

        float dist = Vector3.Distance(spacecraft.transform.position, current.transform.position);
        if (dist <= current.triggerRadius)
        {
            ClearCurrentCheckpoint(current);
        }
    }

    private void ClearCurrentCheckpoint(Checkpoint cp)
    {
        cp.SetState(Checkpoint.CheckpointState.Completed);

        // Award fuel bonus
        if (spacecraft != null && fuelBonusPerCheckpoint > 0f)
        {
            spacecraft.Refuel(fuelBonusPerCheckpoint);
        }

        OnCheckpointCleared?.Invoke(cp, CurrentCheckpointIndex + 1, checkpoints.Count);

        if (cp.isFinalDestination || CurrentCheckpointIndex >= checkpoints.Count - 1)
        {
            IsMissionCompleted = true;
            OnAllCheckpointsCleared?.Invoke();
        }
        else
        {
            CurrentCheckpointIndex++;
            if (ActiveCheckpoint != null)
            {
                ActiveCheckpoint.SetState(Checkpoint.CheckpointState.Active);
            }
        }
    }

    public float GetDistanceToActiveCheckpoint()
    {
        if (spacecraft == null || ActiveCheckpoint == null) return 0f;
        return Vector3.Distance(spacecraft.transform.position, ActiveCheckpoint.transform.position);
    }

    public float GetDistanceToDestination()
    {
        if (spacecraft == null || DestinationCheckpoint == null) return 0f;
        return Vector3.Distance(spacecraft.transform.position, DestinationCheckpoint.transform.position);
    }
}

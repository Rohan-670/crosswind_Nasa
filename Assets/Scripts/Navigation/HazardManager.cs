using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Reusable HazardManager:
/// tracks all active hazard volumes (debris fields, radiation storms),
/// detects spacecraft penetration, inflicts hull damage over time,
/// and broadcasts alerts to the flight HUD.
/// </summary>
public class HazardManager : MonoBehaviour
{
    public static HazardManager Instance { get; private set; }

    [Header("--- Registered Hazards ---")]
    public List<HazardZone> hazardZones = new List<HazardZone>();

    [Header("--- Target Spacecraft ---")]
    public SpacecraftController spacecraft;

    // Runtime Alert Telemetry
    public bool IsInHazard { get; private set; } = false;
    public HazardZone CurrentActiveHazard { get; private set; } = null;

    public event Action<bool, HazardZone> OnHazardAlertChanged; // inHazard, hazardZone

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (spacecraft == null || !spacecraft.IsAlive) return;

        CheckHazardPenetration();
    }

    private void CheckHazardPenetration()
    {
        Vector3 shipPos = spacecraft.transform.position;
        HazardZone penetratedZone = null;

        for (int i = 0; i < hazardZones.Count; i++)
        {
            if (hazardZones[i] != null && hazardZones[i].IsPositionInside(shipPos))
            {
                penetratedZone = hazardZones[i];
                break;
            }
        }

        if (penetratedZone != null)
        {
            if (!IsInHazard || CurrentActiveHazard != penetratedZone)
            {
                IsInHazard = true;
                CurrentActiveHazard = penetratedZone;
                OnHazardAlertChanged?.Invoke(true, CurrentActiveHazard);
            }

            // Apply continuous hull damage
            float dmg = penetratedZone.damagePerSecond * Time.deltaTime;
            spacecraft.TakeDamage(dmg, penetratedZone.hazardName);
        }
        else
        {
            if (IsInHazard)
            {
                IsInHazard = false;
                CurrentActiveHazard = null;
                OnHazardAlertChanged?.Invoke(false, null);
            }
        }
    }
}

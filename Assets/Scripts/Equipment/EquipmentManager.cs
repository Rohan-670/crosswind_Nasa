using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Reusable, persistent manager that tracks astronaut equipped gear,
/// cargo capacity constraints, and launchpad loadout validation.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    [Header("--- Capacity Configuration ---")]
    [Tooltip("Maximum cargo capacity in kilograms for the mission payload")]
    public float maxCargoCapacityKg = 100f;

    [Header("--- Active Loadout ---")]
    [SerializeField]
    private List<EquipmentItem> equippedItems = new List<EquipmentItem>();

    public IReadOnlyList<EquipmentItem> EquippedItems => equippedItems;

    public event Action OnLoadoutChanged;

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

    public bool IsEquipped(EquipmentItem item)
    {
        if (item == null) return false;
        return equippedItems.Exists(i => i != null && i.itemId == item.itemId);
    }

    public bool IsEquipped(string itemId)
    {
        return equippedItems.Exists(i => i != null && i.itemId == itemId);
    }

    public bool Equip(EquipmentItem item)
    {
        if (item == null || IsEquipped(item)) return false;

        equippedItems.Add(item);
        Debug.Log($"[EquipmentManager] Equipped '{item.itemName}' (+{item.weightKg:F1} kg). Total: {GetTotalWeight():F1}/{maxCargoCapacityKg:F1} kg");

        OnLoadoutChanged?.Invoke();
        return true;
    }

    public bool Unequip(EquipmentItem item)
    {
        if (item == null) return false;

        int removedCount = equippedItems.RemoveAll(i => i != null && i.itemId == item.itemId);
        if (removedCount > 0)
        {
            Debug.Log($"[EquipmentManager] Unequipped '{item.itemName}' (-{item.weightKg:F1} kg). Total: {GetTotalWeight():F1}/{maxCargoCapacityKg:F1} kg");
            OnLoadoutChanged?.Invoke();
            return true;
        }

        return false;
    }

    public void Toggle(EquipmentItem item)
    {
        if (item == null) return;

        if (IsEquipped(item))
        {
            Unequip(item);
        }
        else
        {
            Equip(item);
        }
    }

    public float GetTotalWeight()
    {
        float total = 0f;
        for (int i = 0; i < equippedItems.Count; i++)
        {
            if (equippedItems[i] != null)
                total += equippedItems[i].weightKg;
        }
        return total;
    }

    public float GetRemainingCapacity()
    {
        return Mathf.Max(0f, maxCargoCapacityKg - GetTotalWeight());
    }

    public bool IsOverweight()
    {
        return GetTotalWeight() > maxCargoCapacityKg;
    }

    /// <summary>
    /// Validates whether the current loadout meets all mission safety requirements.
    /// Returns true if valid; otherwise false with a descriptive reason.
    /// </summary>
    public bool ValidateLoadout(EquipmentItem[] allAvailableItems, out string failureReason)
    {
        failureReason = string.Empty;

        // 1. Check weight capacity
        float currentWeight = GetTotalWeight();
        if (currentWeight > maxCargoCapacityKg)
        {
            float excess = currentWeight - maxCargoCapacityKg;
            failureReason = $"Cargo capacity exceeded by {excess:F1} kg! Reduce payload before boarding.";
            return false;
        }

        // 2. Check all required items
        List<string> missingRequired = new List<string>();
        if (allAvailableItems != null)
        {
            for (int i = 0; i < allAvailableItems.Length; i++)
            {
                EquipmentItem item = allAvailableItems[i];
                if (item != null && item.isRequired && !IsEquipped(item))
                {
                    missingRequired.Add(item.itemName);
                }
            }
        }

        if (missingRequired.Count > 0)
        {
            failureReason = $"Mandatory life support gear missing: {string.Join(", ", missingRequired)}.";
            return false;
        }

        return true;
    }
}

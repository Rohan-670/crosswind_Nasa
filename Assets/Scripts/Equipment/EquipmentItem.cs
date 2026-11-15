using UnityEngine;

public enum EquipmentCategory
{
    LifeSupport,
    Safety,
    Scientific,
    Tools
}

/// <summary>
/// ScriptableObject defining an outfitting item for astronaut missions.
/// Create new equipment items via Assets > Create > ASTROBASE > Equipment Item.
/// </summary>
[CreateAssetMenu(fileName = "NewEquipment", menuName = "ASTROBASE/Equipment Item", order = 2)]
public class EquipmentItem : ScriptableObject
{
    [Header("--- Identification ---")]
    public string itemId = "equipment_id";
    public string itemName = "Equipment Name";
    public EquipmentCategory category = EquipmentCategory.LifeSupport;

    [Header("--- Specifications ---")]
    [TextArea(2, 4)]
    public string description = "Equipment item description and operational parameters.";
    [Tooltip("Weight in kilograms")]
    public float weightKg = 10f;

    [Header("--- Mission Requirement ---")]
    [Tooltip("If true, the mission cannot launch without this item")]
    public bool isRequired = false;

    [Header("--- Visual Telemetry ---")]
    public Sprite icon;
    public Color accentColor = new Color(0.3f, 0.85f, 1.0f);
}

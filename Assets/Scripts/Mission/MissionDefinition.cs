using UnityEngine;

public enum MissionDifficulty
{
    Moderate,
    Challenging,
    Extreme
}

/// <summary>
/// ScriptableObject defining an expedition in ASTROBASE: BEYOND EARTH.
/// Easily create new missions via Create > ASTROBASE > Mission Definition.
/// </summary>
[CreateAssetMenu(fileName = "NewMission", menuName = "ASTROBASE/Mission Definition", order = 1)]
public class MissionDefinition : ScriptableObject
{
    [Header("--- Identification ---")]
    public string missionId = "lunar_expedition";
    public string destination;
    public string missionName = "Lunar Expedition";
    public string targetDestination = "The Moon (South Pole)";
    public string estimatedDuration = "14 Earth Days";

    [Header("--- Briefing ---")]
    [TextArea(3, 6)]
    public string description = "Descend to the lunar surface at Shackleton Crater. Establish a base camp, collect core regolith ice samples, and safely navigate lunar hazards before returning to Earth.";

    [Header("--- Parameters ---")]
    public MissionDifficulty difficulty = MissionDifficulty.Moderate;
    public Color accentColor = new Color(0.3f, 0.85f, 1.0f); // NASA Cyan
    public Sprite previewImage;

    [Header("--- Objectives ---")]
    [TextArea(1, 3)]
    public string[] objectives = new string[]
    {
        "Trans-Lunar injection and orbital insertion",
        "Targeted landing at Shackleton Crater",
        "Collect volatile ice and regolith samples",
        "Ascent to lunar orbit and safe return to Earth"
    };

    [Header("--- Readiness & Status ---")]
    public bool isPlayableNow = true;
    public string targetSceneName = "MissionPreparation";

    [Header("--- Coming Soon Telemetry ---")]
    [TextArea(3, 6)]
    public string comingSoonNotice = "";
}

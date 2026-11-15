using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Controls an individual Mission Card in the MissionSelection UI.
/// </summary>
public class MissionCardUI : MonoBehaviour
{
    [Header("--- Header Elements ---")]
    public TextMeshProUGUI missionNameText;
    public TextMeshProUGUI destinationText;
    public Image headerGlow;

    [Header("--- Difficulty Badge ---")]
    public TextMeshProUGUI difficultyText;
    public Image difficultyBadgeImage;

    [Header("--- Visual & Briefing ---")]
    public Image previewImage;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI objectivesText;

    [Header("--- Card Controls ---")]
    public Button selectButton;
    public TextMeshProUGUI selectButtonText;
    public Image selectButtonImage;
    public Image cardBorder;

    private MissionDefinition boundMission;
    private Action<MissionDefinition> onSelectCallback;

    public void Bind(MissionDefinition data, Action<MissionDefinition> onSelect)
    {
        boundMission = data;
        onSelectCallback = onSelect;

        if (data == null) return;

        if (missionNameText != null) missionNameText.text = data.missionName.ToUpper();
        if (destinationText != null) destinationText.text = $"{data.targetDestination.ToUpper()} // {data.estimatedDuration.ToUpper()}";
        if (descriptionText != null) descriptionText.text = data.description;

        if (difficultyText != null)
        {
            difficultyText.text = data.difficulty.ToString().ToUpper();
        }

        if (difficultyBadgeImage != null)
        {
            difficultyBadgeImage.color = GetDifficultyColor(data.difficulty);
        }

        if (previewImage != null && data.previewImage != null)
        {
            previewImage.sprite = data.previewImage;
        }

        if (objectivesText != null && data.objectives != null)
        {
            string bulleted = "";
            for (int i = 0; i < data.objectives.Length; i++)
            {
                bulleted += $"- {data.objectives[i]}\n";
            }
            objectivesText.text = bulleted.TrimEnd();
        }

        if (cardBorder != null)
        {
            Color borderCol = data.accentColor;
            borderCol.a = 0.65f;
            cardBorder.color = borderCol;
        }

        if (headerGlow != null)
        {
            Color glow = data.accentColor;
            glow.a = 0.35f;
            headerGlow.color = glow;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(HandleSelectClicked);

            if (selectButtonText != null)
            {
                selectButtonText.text = data.isPlayableNow ? "SELECT MISSION >" : "VIEW STATUS";
            }

            if (selectButtonImage != null)
            {
                Color btnCol = data.isPlayableNow
                    ? new Color(0.12f, 0.45f, 0.85f, 0.95f)
                    : new Color(0.22f, 0.28f, 0.38f, 0.85f);
                selectButtonImage.color = btnCol;
            }
        }
    }

    private void HandleSelectClicked()
    {
        if (boundMission != null)
        {
            onSelectCallback?.Invoke(boundMission);
        }
    }

    private Color GetDifficultyColor(MissionDifficulty difficulty)
    {
        switch (difficulty)
        {
            case MissionDifficulty.Moderate:
                return new Color(0.25f, 0.85f, 0.45f); // Green/Cyan
            case MissionDifficulty.Challenging:
                return new Color(1.0f, 0.65f, 0.15f); // Amber/Orange
            case MissionDifficulty.Extreme:
                return new Color(0.95f, 0.28f, 0.28f); // Red
            default:
                return Color.white;
        }
    }
}

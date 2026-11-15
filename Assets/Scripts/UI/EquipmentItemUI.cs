using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Controls an individual equipment row in the outfitting interface.
/// </summary>
public class EquipmentItemUI : MonoBehaviour
{
    [Header("--- UI References ---")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI weightText;
    public TextMeshProUGUI statusBadgeText;
    public Image statusBadgeImage;
    public Button equipToggleButton;
    public TextMeshProUGUI toggleButtonText;
    public Image toggleButtonImage;
    public Image itemBorder;

    private EquipmentItem boundItem;
    private Action<EquipmentItem> onToggleCallback;

    public void Bind(EquipmentItem item, bool isEquipped, Action<EquipmentItem> onToggle)
    {
        boundItem = item;
        onToggleCallback = onToggle;

        if (item == null) return;

        if (itemNameText != null) itemNameText.text = item.itemName.ToUpper();
        if (descriptionText != null) descriptionText.text = item.description;
        if (weightText != null) weightText.text = $"{item.weightKg:F1} KG";

        // Required vs Optional Badge
        if (statusBadgeText != null)
        {
            statusBadgeText.text = item.isRequired ? "MANDATORY" : "OPTIONAL";
        }

        if (statusBadgeImage != null)
        {
            statusBadgeImage.color = item.isRequired
                ? new Color(0.95f, 0.35f, 0.20f, 0.9f) // Red-orange
                : new Color(0.25f, 0.32f, 0.42f, 0.8f); // Slate
        }

        // Toggle Button
        if (equipToggleButton != null)
        {
            equipToggleButton.onClick.RemoveAllListeners();
            equipToggleButton.onClick.AddListener(HandleToggleClicked);
        }

        UpdateEquippedVisual(isEquipped);
    }

    public void UpdateEquippedVisual(bool isEquipped)
    {
        if (toggleButtonText != null)
        {
            toggleButtonText.text = isEquipped ? "[OK] EQUIPPED" : "+ EQUIP";
        }

        if (toggleButtonImage != null)
        {
            toggleButtonImage.color = isEquipped
                ? new Color(0.15f, 0.55f, 0.85f, 0.95f) // Active Cyan/Blue
                : new Color(0.18f, 0.22f, 0.30f, 0.85f); // Inactive dark
        }

        if (itemBorder != null)
        {
            Color bCol = isEquipped ? new Color(0.3f, 0.85f, 1f, 0.8f) : new Color(0.2f, 0.25f, 0.35f, 0.4f);
            itemBorder.color = bCol;
        }
    }

    private void HandleToggleClicked()
    {
        if (boundItem != null)
        {
            onToggleCallback?.Invoke(boundItem);
        }
    }
}

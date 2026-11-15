using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Controller for the MissionPreparation scene:
/// manages astronaut gear outfitting, capacity telemetry, required item validation,
/// and flight deck boarding clearance.
/// </summary>
public class MissionPreparationUI : MonoBehaviour
{
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI objectivesText;
    public TextMeshProUGUI launchpadStatusText;
    public Button launchpadButton;
    [Header("--- Header Telemetry ---")]
    public TextMeshProUGUI missionTitleText;
    public TextMeshProUGUI destinationText;
    public TextMeshProUGUI missionStatusText;

    [Header("--- Equipment Catalog ---")]
    public EquipmentItem[] availableEquipment;
    public Transform equipmentListContainer;

    [Header("--- Capacity Telemetry ---")]
    public TextMeshProUGUI totalWeightText;
    public TextMeshProUGUI remainingCapacityText;
    public Image capacityProgressBar;

    [Header("--- Validation & Flight Clearance ---")]
    public TextMeshProUGUI validationStatusText;
    public Image validationStatusBg;

    [Header("--- Navigation & Action Buttons ---")]
    public Button returnButton;
    public Button continueToBoardingButton;
    public TextMeshProUGUI continueButtonText;
    public Image continueButtonImage;

    [Header("--- 360 Turntable View Actions ---")]
    public Button rotateLeftButton;
    public Button rotateRightButton;
    public Button autoSpinButton;
    public Button resetViewButton;

    [Header("--- Boarding Confirmation Modal ---")]
    public GameObject boardingModal;
    public TextMeshProUGUI boardingModalText;
    public Button boardingModalCloseButton;
    public Button proceedToLaunchButton;

    private Dictionary<string, EquipmentItemUI> itemUIMap = new Dictionary<string, EquipmentItemUI>();

    private void Start()
    {
        // Ensure 360 viewer & modular visual equipment controller is active on the astronaut
        GameObject astroGO = GameObject.Find("Astronaut_EMU");
        if (astroGO != null && astroGO.GetComponent<AstronautVisualEquipmentController>() == null)
        {
            AstronautVisualEquipmentController visualCtrl = astroGO.AddComponent<AstronautVisualEquipmentController>();
            visualCtrl.astronautRoot = astroGO.transform;
            visualCtrl.defaultYRotation = 160f;
            visualCtrl.BuildOrFindAttachmentProps();
        }

        if (returnButton != null)
        {
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(OnReturnClicked);
        }

        if (continueToBoardingButton != null)
        {
            continueToBoardingButton.onClick.RemoveAllListeners();
            continueToBoardingButton.onClick.AddListener(OnContinueToBoardingClicked);
        }

        if (boardingModalCloseButton != null)
        {
            boardingModalCloseButton.onClick.RemoveAllListeners();
            boardingModalCloseButton.onClick.AddListener(CloseBoardingModal);
        }

        if (proceedToLaunchButton != null)
        {
            proceedToLaunchButton.onClick.RemoveAllListeners();
            proceedToLaunchButton.onClick.AddListener(OnProceedToLaunchClicked);
        }

        if (rotateLeftButton != null)
        {
            rotateLeftButton.onClick.RemoveAllListeners();
            rotateLeftButton.onClick.AddListener(() =>
            {
                if (AstronautVisualEquipmentController.Instance != null)
                    AstronautVisualEquipmentController.Instance.RotateLeft(30f);
            });
        }

        if (rotateRightButton != null)
        {
            rotateRightButton.onClick.RemoveAllListeners();
            rotateRightButton.onClick.AddListener(() =>
            {
                if (AstronautVisualEquipmentController.Instance != null)
                    AstronautVisualEquipmentController.Instance.RotateRight(30f);
            });
        }

        if (autoSpinButton != null)
        {
            autoSpinButton.onClick.RemoveAllListeners();
            autoSpinButton.onClick.AddListener(() =>
            {
                if (AstronautVisualEquipmentController.Instance != null)
                    AstronautVisualEquipmentController.Instance.ToggleAutoSpin();
            });
        }

        if (resetViewButton != null)
        {
            resetViewButton.onClick.RemoveAllListeners();
            resetViewButton.onClick.AddListener(() =>
            {
                if (AstronautVisualEquipmentController.Instance != null)
                    AstronautVisualEquipmentController.Instance.ResetView();
            });
        }

        if (boardingModal != null)
            boardingModal.SetActive(false);

        // Bind equipment manager events
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnLoadoutChanged += RefreshUI;
        }

        PopulateHeader();
        PopulateEquipmentRows();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnLoadoutChanged -= RefreshUI;
        }
    }

    private void PopulateHeader()
    {
        MissionDefinition mission = MissionManager.Instance != null
            ? MissionManager.Instance.SelectedMission
            : null;

        if (mission != null)
        {
            if (missionTitleText != null)
                missionTitleText.text = $"{mission.missionName.ToUpper()} // CREW OUTFITTING";
            if (destinationText != null)
                destinationText.text = $"TARGET: {mission.targetDestination.ToUpper()} // DURATION: {mission.estimatedDuration.ToUpper()}";
        }
        else
        {
            if (missionTitleText != null)
                missionTitleText.text = "LUNAR EXPEDITION // CREW OUTFITTING";
            if (destinationText != null)
                destinationText.text = "TARGET: THE MOON (SOUTH POLE) // DURATION: 14 EARTH DAYS";
        }
    }

    private void PopulateEquipmentRows()
    {
        if (equipmentListContainer == null || availableEquipment == null) return;

        // Auto-detect existing row components in container
        EquipmentItemUI[] existingUIs = equipmentListContainer.GetComponentsInChildren<EquipmentItemUI>(true);

        itemUIMap.Clear();
        for (int i = 0; i < existingUIs.Length && i < availableEquipment.Length; i++)
        {
            EquipmentItem item = availableEquipment[i];
            EquipmentItemUI ui = existingUIs[i];
            bool isEquipped = EquipmentManager.Instance != null && EquipmentManager.Instance.IsEquipped(item);
            ui.Bind(item, isEquipped, OnEquipmentToggled);
            itemUIMap[item.itemId] = ui;
        }
    }

    private void OnEquipmentToggled(EquipmentItem item)
    {
        if (item == null || EquipmentManager.Instance == null) return;

        EquipmentManager.Instance.Toggle(item);
    }

    public void RefreshUI()
    {
        if (EquipmentManager.Instance == null) return;

        float totalWeight = EquipmentManager.Instance.GetTotalWeight();
        float maxCap = EquipmentManager.Instance.maxCargoCapacityKg;
        float remaining = EquipmentManager.Instance.GetRemainingCapacity();

        // 1. Update Weight Telemetry
        if (totalWeightText != null)
        {
            totalWeightText.text = $"PAYLOAD WEIGHT: {totalWeight:F1} / {maxCap:F1} KG";
            totalWeightText.color = totalWeight > maxCap ? new Color(1f, 0.35f, 0.35f) : Color.white;
        }

        if (remainingCapacityText != null)
        {
            remainingCapacityText.text = $"FREE CAPACITY: {remaining:F1} KG";
            remainingCapacityText.color = remaining <= 0f && totalWeight > maxCap
                ? new Color(1f, 0.35f, 0.35f)
                : new Color(0.4f, 0.85f, 1f);
        }

        if (capacityProgressBar != null)
        {
            float fill = maxCap > 0f ? Mathf.Clamp01(totalWeight / maxCap) : 0f;
            capacityProgressBar.fillAmount = fill;
            capacityProgressBar.color = totalWeight > maxCap
                ? new Color(1f, 0.25f, 0.25f)
                : Color.Lerp(new Color(0.25f, 0.85f, 0.45f), new Color(0.2f, 0.6f, 1f), fill);
        }

        // 2. Update Row Visuals
        if (availableEquipment != null)
        {
            foreach (var item in availableEquipment)
            {
                if (item != null && itemUIMap.TryGetValue(item.itemId, out EquipmentItemUI ui))
                {
                    bool isEquipped = EquipmentManager.Instance.IsEquipped(item);
                    ui.UpdateEquippedVisual(isEquipped);
                }
            }
        }

        // 3. Validation Check
        bool isValid = EquipmentManager.Instance.ValidateLoadout(availableEquipment, out string failureReason);

        if (isValid)
        {
            if (validationStatusText != null)
            {
                validationStatusText.text = "[OK] ALL REQUIRED LIFE SUPPORT GEAR VERIFIED // READY FOR BOARDING";
                validationStatusText.color = new Color(0.35f, 1.0f, 0.55f);
            }
            if (validationStatusBg != null)
            {
                validationStatusBg.color = new Color(0.08f, 0.25f, 0.15f, 0.85f);
            }

            if (continueToBoardingButton != null)
                continueToBoardingButton.interactable = true;

            if (continueButtonText != null)
                continueButtonText.text = "CONTINUE TO BOARDING >";

            if (continueButtonImage != null)
                continueButtonImage.color = new Color(0.12f, 0.52f, 0.90f, 0.95f);
        }
        else
        {
            if (validationStatusText != null)
            {
                validationStatusText.text = $"[!] FLIGHT HOLD: {failureReason.ToUpper()}";
                validationStatusText.color = new Color(1.0f, 0.45f, 0.35f);
            }
            if (validationStatusBg != null)
            {
                validationStatusBg.color = new Color(0.28f, 0.10f, 0.10f, 0.85f);
            }

            if (continueToBoardingButton != null)
                continueToBoardingButton.interactable = false;

            if (continueButtonText != null)
                continueButtonText.text = "BOARDING LOCKED";

            if (continueButtonImage != null)
                continueButtonImage.color = new Color(0.22f, 0.25f, 0.32f, 0.7f);
        }
    }

    private void OnContinueToBoardingClicked()
    {
        string reason = "EquipmentManager instance is missing.";
        bool isValid = EquipmentManager.Instance != null &&
            EquipmentManager.Instance.ValidateLoadout(availableEquipment, out  reason);

        if (!isValid)
        {
            Debug.LogWarning($"[MissionPreparationUI] Boarding blocked: {reason}");
            return;
        }

        Debug.Log("[MissionPreparationUI] Loadout approved! Astronaut proceeding to spacecraft airlock boarding.");

        if (boardingModal != null)
        {
            if (boardingModalText != null)
            {
                float totalWeight = EquipmentManager.Instance.GetTotalWeight();
                int itemCount = EquipmentManager.Instance.EquippedItems.Count;
                boardingModalText.text = $"MISSION CLEARANCE: GRANTED\n\n" +
                    $"• Outfitted Items: {itemCount} units\n" +
                    $"• Total Payload: {totalWeight:F1} kg\n" +
                    $"• Life Support Systems: Nominal\n\n" +
                    $"The astronaut has boarded the spacecraft in low Earth orbit. Flight telemetry is primed for launch sequence.";
            }
            boardingModal.SetActive(true);
        }
    }

    private void CloseBoardingModal()
    {
        if (boardingModal != null)
            boardingModal.SetActive(false);
    }

    private void OnProceedToLaunchClicked()
    {
        if (Application.CanStreamedLevelBeLoaded("LaunchSequence"))
        {
            SceneManager.LoadScene("LaunchSequence");
        }
        else
        {
            Debug.LogWarning("[MissionPreparationUI] LaunchSequence scene not found in build settings.");
        }
    }

    private void OnReturnClicked()
    {
        if (Application.CanStreamedLevelBeLoaded("MissionSelection"))
        {
            SceneManager.LoadScene("MissionSelection");
        }
    }
}

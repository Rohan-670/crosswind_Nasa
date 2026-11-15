using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main controller for the MissionSelection screen:
/// manages mission cards, coming-soon modal popups, and hand-off to MissionManager.
/// </summary>
public class MissionSelectionUI : MonoBehaviour
{
    [Header("--- Mission Definitions ---")]
    public MissionDefinition[] missions;

    [Header("--- Card Container ---")]
    public Transform cardsContainer;
    public GameObject missionCardPrefab;

    [Header("--- Coming Soon Modal ---")]
    public GameObject comingSoonModal;
    public TextMeshProUGUI modalTitleText;
    public TextMeshProUGUI modalDestinationText;
    public TextMeshProUGUI modalBodyText;
    public Button modalCloseButton;

    [Header("--- Header / Navigation ---")]
    public Button backButton;

    private void Start()
    {
        if (comingSoonModal != null)
            comingSoonModal.SetActive(false);

        if (modalCloseButton != null)
        {
            modalCloseButton.onClick.RemoveAllListeners();
            modalCloseButton.onClick.AddListener(CloseModal);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackPressed);
        }

        PopulateCards();
    }

    public void PopulateCards()
    {
        if (missions == null || missions.Length == 0) return;

        MissionCardUI[] existingCards = cardsContainer != null
            ? cardsContainer.GetComponentsInChildren<MissionCardUI>(true)
            : new MissionCardUI[0];

        if (existingCards.Length >= missions.Length)
        {
            for (int i = 0; i < missions.Length; i++)
            {
                existingCards[i].Bind(missions[i], OnMissionCardSelected);
            }
        }
        else if (missionCardPrefab != null && cardsContainer != null)
        {
            for (int i = cardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(cardsContainer.GetChild(i).gameObject);
            }

            for (int i = 0; i < missions.Length; i++)
            {
                GameObject cardGO = Instantiate(missionCardPrefab, cardsContainer);
                MissionCardUI cardUI = cardGO.GetComponent<MissionCardUI>();
                if (cardUI != null)
                {
                    cardUI.Bind(missions[i], OnMissionCardSelected);
                }
            }
        }
    }

    public void OnMissionCardSelected(MissionDefinition mission)
    {
        if (mission == null) return;

        // Save selected mission in MissionManager
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.SelectMission(mission);
        }

        if (mission.isPlayableNow)
        {
            Debug.Log($"[MissionSelectionUI] Mission '{mission.missionName}' selected. Proceeding to preparation...");
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.ProceedToPreparation();
            }
        }
        else
        {
            OpenComingSoonModal(mission);
        }
    }

    private void OpenComingSoonModal(MissionDefinition mission)
    {
        if (comingSoonModal == null) return;

        if (modalTitleText != null)
            modalTitleText.text = mission.missionName.ToUpper();

        if (modalDestinationText != null)
            modalDestinationText.text = $"TARGET: {mission.targetDestination.ToUpper()} // STATUS: IN ENGINEERING REVIEW";

        if (modalBodyText != null)
        {
            string notice = !string.IsNullOrEmpty(mission.comingSoonNotice)
                ? mission.comingSoonNotice
                : $"{mission.missionName} is currently undergoing flight hardware integration and trajectory simulation.\n\nFlight clearance for this expedition will unlock in future ASTROBASE mission updates.\n\nPlease proceed with the active Lunar Expedition.";
            modalBodyText.text = notice;
        }

        comingSoonModal.SetActive(true);
    }

    public void CloseModal()
    {
        if (comingSoonModal != null)
            comingSoonModal.SetActive(false);
    }

    private void OnBackPressed()
    {
        if (Application.CanStreamedLevelBeLoaded("OpeningStory_VisualTest"))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("OpeningStory_VisualTest");
        }
        else if (Application.CanStreamedLevelBeLoaded("OpeningStory"))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("OpeningStory");
        }
    }
}

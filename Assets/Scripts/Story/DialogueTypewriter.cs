using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Displays dialogue text with a smooth typewriter effect, speaker identification,
/// and interactive Continue / Skip controls.
/// </summary>
public class DialogueTypewriter : MonoBehaviour
{
    [System.Serializable]
    public struct DialogueEntry
    {
        public string speaker;
        [TextArea(2, 4)]
        public string text;
        public Color speakerColor;

        public DialogueEntry(string speaker, string text, Color speakerColor)
        {
            this.speaker = speaker;
            this.text = text;
            this.speakerColor = speakerColor;
        }
    }

    [Header("--- UI References ---")]
    [Tooltip("Text element for displaying the speaker callsign / title")]
    public TextMeshProUGUI speakerText;

    [Tooltip("Text element for displaying dialogue character-by-character")]
    public TextMeshProUGUI dialogueText;

    [Tooltip("Button to advance dialogue or complete current line instantly")]
    public Button continueButton;

    [Tooltip("Button to skip through all dialogue immediately")]
    public Button skipButton;

    [Header("--- Astronaut Introduction Content ---")]
    public DialogueEntry[] dialogueEntries = new DialogueEntry[]
    {
        new DialogueEntry(
            "COMMANDER // HORIZON-1",
            "Welcome aboard, astronaut. Humanity is preparing for its next great journey.",
            new Color(0.3f, 0.95f, 1.0f) // Cyan
        ),
        new DialogueEntry(
            "MISSION CONTROL // HOUSTON",
            "Our destination is beyond Earth. Three extraordinary missions await us.",
            new Color(1.0f, 0.8f, 0.25f) // Warm amber
        ),
        new DialogueEntry(
            "COMMANDER // HORIZON-1",
            "The Moon, the Red Planet, and the mysteries of our Sun. Your decisions will determine the success of every mission.",
            new Color(0.3f, 0.95f, 1.0f) // Cyan
        )
    };

    [Header("--- Timing & Pacing ---")]
    [Tooltip("Seconds between each typed character")]
    public float typingSpeed = 0.035f;

    [Tooltip("Seconds before dialogue starts typing (allows cinematic title to establish)")]
    public float delayBeforeStart = 3.5f;

    // --- Runtime state ---
    private int currentEntryIndex = 0;
    private bool isCurrentlyTyping = false;
    private bool finishLineRequested = false;
    private Coroutine activeTypingCoroutine;

    void Start()
    {
        if (dialogueText != null)
            dialogueText.text = "";

        if (speakerText != null)
            speakerText.text = "";

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinuePressed);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipPressed);
        }

        StartCoroutine(BeginDialogueSequence());
    }

    IEnumerator BeginDialogueSequence()
    {
        yield return new WaitForSeconds(delayBeforeStart);
        ShowCurrentEntry();
    }

    void ShowCurrentEntry()
    {
        if (currentEntryIndex >= dialogueEntries.Length)
        {
            FinishAllDialogue();
            return;
        }

        if (activeTypingCoroutine != null)
            StopCoroutine(activeTypingCoroutine);

        activeTypingCoroutine = StartCoroutine(TypeOutEntry(dialogueEntries[currentEntryIndex]));
    }

    IEnumerator TypeOutEntry(DialogueEntry entry)
    {
        isCurrentlyTyping = true;
        finishLineRequested = false;

        // Set speaker header if available
        if (speakerText != null)
        {
            speakerText.text = entry.speaker;
            speakerText.color = entry.speakerColor;
        }

        if (dialogueText != null)
            dialogueText.text = "";

        // Hide Continue button while typing
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        string fullText = entry.text;
        for (int i = 0; i < fullText.Length; i++)
        {
            if (finishLineRequested)
            {
                dialogueText.text = fullText;
                break;
            }

            dialogueText.text = fullText.Substring(0, i + 1);
            yield return new WaitForSeconds(typingSpeed);
        }

        // Line is complete — prompt player to continue
        isCurrentlyTyping = false;
        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
    }

    public void OnContinuePressed()
    {
        if (isCurrentlyTyping)
        {
            // Fast-forward current line instantly
            finishLineRequested = true;
        }
        else
        {
            // Advance to next transmission
            currentEntryIndex++;
            ShowCurrentEntry();
        }
    }

    public void OnSkipPressed()
    {
        if (activeTypingCoroutine != null)
            StopCoroutine(activeTypingCoroutine);

        FinishAllDialogue();
    }

    void FinishAllDialogue()
    {
        isCurrentlyTyping = false;

        if (speakerText != null)
        {
            speakerText.text = "MISSION CONTROL // TELEMETRY";
            speakerText.color = new Color(0.4f, 1f, 0.6f); // Terminal green
        }

        if (dialogueText != null)
        {
            dialogueText.text = "All systems primed. Press Continue to enter Mission Operations.";
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            TextMeshProUGUI btnText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = "MISSION SELECT >";

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnFinalContinuePressed);
        }

        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        Debug.Log("[ASTROBASE] Opening story introduction complete.");
    }

    void OnFinalContinuePressed()
    {
        Debug.Log("[ASTROBASE] Player acknowledged mission readiness. Transitioning to MissionSelection...");
        if (UnityEngine.Application.CanStreamedLevelBeLoaded("MissionSelection"))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MissionSelection");
        }
        else
        {
            Debug.LogWarning("[ASTROBASE] Scene 'MissionSelection' is not in Build Settings or not yet loaded.");
        }
    }
}

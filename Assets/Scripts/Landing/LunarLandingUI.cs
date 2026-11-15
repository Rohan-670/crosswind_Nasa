using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Descent and landing HUD controller:
/// displays radar altimeter, vertical descent speed indicator (VSI),
/// horizontal drift telemetry, fuel gauge, and touchdown success/failure banners.
/// </summary>
public class LunarLandingUI : MonoBehaviour
{
    [Header("--- Target Lander ---")]
    public LunarLanderController lander;

    [Header("--- Radar Telemetry Displays ---")]
    public TextMeshProUGUI altitudeText;
    public TextMeshProUGUI verticalSpeedText;
    public Image verticalSpeedIndicatorBar;
    public TextMeshProUGUI horizontalDriftText;

    [Header("--- Propulsion Telemetry ---")]
    public TextMeshProUGUI throttleText;
    public Image throttleBar;
    public TextMeshProUGUI fuelText;
    public Image fuelBar;

    [Header("--- Landing Safety Status ---")]
    public TextMeshProUGUI landingStatusBadge;
    public Image landingStatusBg;

    [Header("--- Result Modals & Banners ---")]
    public GameObject touchdownSuccessBanner;
    public TextMeshProUGUI touchdownSuccessText;

    public GameObject crashFailureModal;
    public TextMeshProUGUI crashReasonText;
    public Button retryDescentButton;
    public Button returnToMissionsButton;

    private void Start()
    {
        if (touchdownSuccessBanner != null) touchdownSuccessBanner.SetActive(false);
        if (crashFailureModal != null) crashFailureModal.SetActive(false);

        if (retryDescentButton != null)
        {
            retryDescentButton.onClick.RemoveAllListeners();
            retryDescentButton.onClick.AddListener(OnRetryClicked);
        }

        if (returnToMissionsButton != null)
        {
            returnToMissionsButton.onClick.RemoveAllListeners();
            returnToMissionsButton.onClick.AddListener(OnReturnClicked);
        }

        if (lander != null)
        {
            lander.OnLandingResult += HandleLandingResult;
        }
    }

    private void OnDestroy()
    {
        if (lander != null)
        {
            lander.OnLandingResult -= HandleLandingResult;
        }
    }

    private void Update()
    {
        if (lander == null) return;

        UpdateTelemetryDisplays();
    }

    private void UpdateTelemetryDisplays()
    {
        // 1. Altitude
        if (altitudeText != null)
        {
            altitudeText.text = $"RADAR ALTITUDE: {lander.CurrentAltitude:F1} M";
        }

        // 2. Vertical Speed & VSI
        float vSpeed = lander.VerticalSpeed;
        float absVSpeed = Mathf.Abs(vSpeed);
        bool isDescending = vSpeed < 0;

        if (verticalSpeedText != null)
        {
            string arrow = isDescending ? "▼" : "▲";
            verticalSpeedText.text = $"VERTICAL SPEED: {arrow} {absVSpeed:F1} M/S";

            if (absVSpeed <= lander.safeTouchdownMaxSpeed)
            {
                verticalSpeedText.color = new Color(0.35f, 1.0f, 0.55f); // Green safe
            }
            else if (absVSpeed <= lander.safeTouchdownMaxSpeed * 1.4f)
            {
                verticalSpeedText.color = new Color(1.0f, 0.85f, 0.25f); // Amber warning
            }
            else
            {
                verticalSpeedText.color = new Color(1.0f, 0.35f, 0.35f); // Red dangerous
            }
        }

        if (verticalSpeedIndicatorBar != null)
        {
            float vsiNorm = Mathf.Clamp01(absVSpeed / (lander.safeTouchdownMaxSpeed * 2.0f));
            verticalSpeedIndicatorBar.fillAmount = vsiNorm;
            verticalSpeedIndicatorBar.color = (absVSpeed <= lander.safeTouchdownMaxSpeed)
                ? new Color(0.3f, 0.9f, 0.5f)
                : (absVSpeed <= lander.safeTouchdownMaxSpeed * 1.4f ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.3f, 0.3f));
        }

        // 3. Horizontal Drift
        if (horizontalDriftText != null)
        {
            horizontalDriftText.text = $"HORIZONTAL DRIFT: {lander.HorizontalDriftSpeed:F1} M/S";
        }

        // 4. Throttle
        if (throttleText != null)
        {
            throttleText.text = $"THROTTLE: {lander.MainThrottle01 * 100f:F0}%";
        }

        if (throttleBar != null)
        {
            throttleBar.fillAmount = lander.MainThrottle01;
        }

        // 5. Fuel
        if (fuelText != null)
        {
            float fPct = (lander.CurrentFuel / lander.maxFuel) * 100f;
            fuelText.text = $"DESCENT FUEL: {fPct:F0}%";
        }

        if (fuelBar != null)
        {
            fuelBar.fillAmount = lander.CurrentFuel / lander.maxFuel;
        }

        // 6. Landing Status Badge
        if (landingStatusBadge != null && !lander.HasLanded)
        {
            if (lander.CurrentAltitude <= 30f)
            {
                if (absVSpeed <= lander.safeTouchdownMaxSpeed)
                {
                    landingStatusBadge.text = "[ READY FOR CONTACT // SPEED NOMINAL ]";
                    landingStatusBadge.color = new Color(0.35f, 1f, 0.55f);
                    if (landingStatusBg != null) landingStatusBg.color = new Color(0.05f, 0.25f, 0.12f, 0.85f);
                }
                else
                {
                    landingStatusBadge.text = "[ THROTTLE UP // EXCESSIVE DESCENT VELOCITY ]";
                    landingStatusBadge.color = new Color(1.0f, 0.35f, 0.35f);
                    if (landingStatusBg != null) landingStatusBg.color = new Color(0.35f, 0.08f, 0.08f, 0.85f);
                }
            }
            else
            {
                landingStatusBadge.text = "[ DESCENT IN PROGRESS // RADAR LOCKED ]";
                landingStatusBadge.color = new Color(0.35f, 0.85f, 1.0f);
                if (landingStatusBg != null) landingStatusBg.color = new Color(0.04f, 0.12f, 0.22f, 0.85f);
            }
        }
    }

    private void HandleLandingResult(bool isSafe, float impactSpeed, string message)
    {
        if (isSafe)
        {
            if (touchdownSuccessBanner != null)
            {
                touchdownSuccessBanner.SetActive(true);
                if (touchdownSuccessText != null)
                {
                    touchdownSuccessText.text = "LUNAR TOUCHDOWN CONFIRMED\n" +
                        $"<size=75%><color=#88DDAA>{message}\n" +
                        "TRANSITIONING TO EVA SURFACE EXPLORATION...</color></size>";
                }
            }
        }
        else
        {
            if (crashFailureModal != null)
            {
                crashFailureModal.SetActive(true);
                if (crashReasonText != null)
                {
                    crashReasonText.text = message;
                }
            }
        }
    }

    private void OnRetryClicked()
    {
        if (lander != null) lander.RetryDescent();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnReturnClicked()
    {
        if (Application.CanStreamedLevelBeLoaded("MissionSelection"))
        {
            SceneManager.LoadScene("MissionSelection");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }
}

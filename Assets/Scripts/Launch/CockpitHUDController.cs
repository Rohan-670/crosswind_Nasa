using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Telemetry HUD displayed inside the spacecraft cockpit during the launch sequence:
/// shows mission designation, destination, countdown numerals, throttle bars,
/// orbital velocity, and altitude metrics.
/// </summary>
public class CockpitHUDController : MonoBehaviour
{
    [Header("--- Mission Telemetry ---")]
    public TextMeshProUGUI missionTitleText;
    public TextMeshProUGUI destinationText;
    public TextMeshProUGUI vehicleIdText;

    [Header("--- Countdown Display ---")]
    public TextMeshProUGUI countdownNumeralText;
    public TextMeshProUGUI countdownSubStatusText;

    [Header("--- Flight Telemetry ---")]
    public TextMeshProUGUI velocityText;
    public TextMeshProUGUI altitudeText;
    public TextMeshProUGUI throttleText;
    public Image throttleBar;

    [Header("--- Subsystems Indicators ---")]
    public TextMeshProUGUI systemsStatusText;

    public void InitializeTelemetry(string missionName, string destination, string vehicleName = "CTV-01 ORBITER")
    {
        if (missionTitleText != null)
            missionTitleText.text = missionName.ToUpper();

        if (destinationText != null)
            destinationText.text = destination.ToUpper();

        if (vehicleIdText != null)
            vehicleIdText.text = vehicleName.ToUpper();

        if (countdownNumeralText != null)
            countdownNumeralText.text = "STANDBY";

        if (countdownSubStatusText != null)
            countdownSubStatusText.text = "ALL FLIGHT CONTROLLERS POLLING GO";

        UpdateFlightMetrics(7650f, 408f, 0f);
    }

    public void SetCountdown(int secondsRemaining, string subStatus)
    {
        if (countdownNumeralText != null)
        {
            if (secondsRemaining > 0)
            {
                countdownNumeralText.text = $"T-{secondsRemaining:D2}";
                countdownNumeralText.color = secondsRemaining <= 3
                    ? new Color(1.0f, 0.40f, 0.20f)
                    : new Color(0.35f, 0.90f, 1.0f);
            }
            else
            {
                countdownNumeralText.text = "IGNITION // LIFTOFF";
                countdownNumeralText.color = new Color(0.30f, 1.0f, 0.45f);
            }
        }

        if (countdownSubStatusText != null)
        {
            countdownSubStatusText.text = subStatus;
        }
    }

    public void UpdateFlightMetrics(float velocityMps, float altitudeKm, float throttle01)
    {
        if (velocityText != null)
            velocityText.text = $"VELOCITY: {velocityMps:N0} M/S";

        if (altitudeText != null)
            altitudeText.text = $"ALTITUDE: {altitudeKm:N0} KM";

        if (throttleText != null)
        {
            float percent = throttle01 * 104f;
            throttleText.text = $"THROTTLE: {percent:F0}%";
        }

        if (throttleBar != null)
        {
            throttleBar.fillAmount = Mathf.Clamp01(throttle01);
            throttleBar.color = Color.Lerp(new Color(0.2f, 0.6f, 1f), new Color(1f, 0.5f, 0.1f), throttle01);
        }
    }

    public void SetSystemsStatus(string statusString, Color color)
    {
        if (systemsStatusText != null)
        {
            systemsStatusText.text = statusString;
            systemsStatusText.color = color;
        }
    }
}

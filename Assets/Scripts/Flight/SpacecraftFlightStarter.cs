using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Flight phase controller for the translunar cruise scene:
/// manages orbital telemetry, spacecraft cruising motion, free-look camera,
/// and flight navigation status.
/// </summary>
public class SpacecraftFlightStarter : MonoBehaviour
{
    [Header("--- Spacecraft & Celestial Bodies ---")]
    public Transform spacecraftTransform;
    public Transform moonTransform;
    public Transform earthTransform;

    [Header("--- Flight Telemetry HUD ---")]
    public TextMeshProUGUI missionTitleText;
    public TextMeshProUGUI flightPhaseText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI distanceToMoonText;
    public TextMeshProUGUI systemsStatusText;

    [Header("--- Navigation Buttons ---")]
    public Button returnToMissionsButton;

    [Header("--- Flight Cruise Parameters ---")]
    public float cruiseSpeedKps = 1.08f; // km per second
    public float distanceToMoonKm = 284000f;
    public float rollControlSpeed = 30f;

    private void Start()
    {
        if (returnToMissionsButton != null)
        {
            returnToMissionsButton.onClick.RemoveAllListeners();
            returnToMissionsButton.onClick.AddListener(OnReturnClicked);
        }

        string missionName = "LUNAR EXPEDITION: ARTEMIS BEYOND";
        if (MissionManager.Instance != null && MissionManager.Instance.SelectedMission != null)
        {
            missionName = MissionManager.Instance.SelectedMission.missionName;
        }

        if (missionTitleText != null)
            missionTitleText.text = missionName.ToUpper();

        if (flightPhaseText != null)
            flightPhaseText.text = "PHASE: TRANSLUNAR CRUISE COAST // TRAJECTORY NOMINAL";

        if (systemsStatusText != null)
            systemsStatusText.text = "[LIFE SUPPORT: NOMINAL] [POWER: 98% (SOLAR)] [COMMS: DSN GOLDSTONE ACTIVE]";
    }

    private void Update()
    {
        // Simulate progress toward the Moon
        distanceToMoonKm -= cruiseSpeedKps * Time.deltaTime * 10f;
        if (distanceToMoonKm < 1500f) distanceToMoonKm = 1500f;

        if (distanceToMoonText != null)
            distanceToMoonText.text = $"DISTANCE TO MOON: {distanceToMoonKm:N0} KM";

        if (speedText != null)
            speedText.text = $"CRUISE VELOCITY: {cruiseSpeedKps * 1000f:N0} M/S (3,888 KM/H)";

        // User can gently maneuver / roll the spacecraft with A/D or Left/Right arrows
        float rollInput = AstroInput.GetAxis("Horizontal");
        if (spacecraftTransform != null && Mathf.Abs(rollInput) > 0.01f)
        {
            spacecraftTransform.Rotate(Vector3.forward * (-rollInput * rollControlSpeed * Time.deltaTime), Space.Self);
        }

        // Slowly rotate celestial bodies
        if (earthTransform != null)
            earthTransform.Rotate(Vector3.up * (Time.deltaTime * 0.2f), Space.World);

        if (moonTransform != null)
            moonTransform.Rotate(Vector3.up * (Time.deltaTime * 0.1f), Space.World);
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

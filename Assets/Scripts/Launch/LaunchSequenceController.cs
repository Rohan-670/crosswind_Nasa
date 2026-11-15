using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Sequence controller for the Lunar Expedition:
/// 1. Staged at the Space Center Realistic Launch Facility on Earth.
/// 2. Clicking 'BOARD & LAUNCH [E]' starts automated boarding, cockpit systems arming, and T-10 countdown.
/// 3. Automated liftoff and vertical ascent clearing the umbilical gantry tower.
/// 4. Automated atmospheric gravity turn into orbit and Translunar Injection (TLI) burn.
/// 5. Automated translunar flight cruising directly to the Moon with real-time telemetry.
/// 6. Transitions smoothly into LunarLanding.unity for manual powered landing on the Moon.
/// </summary>
public class LaunchSequenceController : MonoBehaviour
{
    public string destination = "THE MOON";
    public string missionName = "LUNAR EXPEDITION: ARTEMIS BEYOND";

    public enum SequencePhase
    {
        PadStaging,           // Space Center on Earth: Rocket on pad, gantry tower in view
        BoardingTransition,   // Camera glides up gantry towards crew ingress
        CockpitPreLaunch,     // Cabin view, automated flight computer armed
        Countdown,            // T-10 to T-0 countdown with audio telemetry
        LiftoffBurn,          // T-0: Main engine ignition, vertical climb clearing 95m tower
        AtmosphericAscent,    // Pitch downrange gravity turn into orbit, sky to space
        TranslunarCruise,     // Automated translunar injection flight across cislunar space
        LunarApproach,        // Arriving at Moon, orbital capture confirmation
        TransitionToLanding   // Smooth fade to LunarLanding scene for manual touchdown
    }

    [Header("--- Current Phase ---")]
    public SequencePhase currentPhase = SequencePhase.PadStaging;

    [Header("--- Spacecraft & Environment Objects ---")]
    public Transform spacecraftRoot;
    public Transform launchFacilityRoot;
    public Transform earthTransform;
    public Transform moonTransform;
    public Transform padViewAnchor;
    public Transform airlockHatchAnchor;
    public Transform cockpitViewAnchor;
    public Transform externalTrackingAnchor;

    [Header("--- Camera System ---")]
    public Camera mainCamera;
    public float cameraTransitionSpeed = 2.0f;

    [Header("--- Audio & Engine Effects ---")]
    public ProceduralEngineAudio engineAudio;
    public Light[] engineThrusterLights;
    public GameObject[] engineExhaustPlumes;
    public ParticleSystem[] engineExhaustParticles;

    [Header("--- Cockpit & Ingress HUD ---")]
    public CockpitHUDController cockpitHUD;
    public GameObject cockpitInteriorFrame;

    [Header("--- Boarding UI Canvas ---")]
    public GameObject stagingUIPanel;
    public Button boardSpacecraftButton;
    public TextMeshProUGUI stagingPromptText;
    public TextMeshProUGUI stagingHeaderSubtitle;
    public Button skipSequenceButton;

    [Header("--- Flight Telemetry & Controls Bar ---")]
    public GameObject flightControlsBar;
    public TextMeshProUGUI flightControlsText;
    public Button fastForwardButton;

    [Header("--- Fade & Transition Overlay ---")]
    public Image fadeOverlay;
    public TextMeshProUGUI transitionCalloutText;

    [Header("--- Automated Flight Telemetry ---")]
    public float currentSpeed = 0f;
    public float currentAltitudeKm = 0.05f;
    public float currentThrust = 0f;
    public float distanceToMoonKm = 384400f;

    private float shakeIntensity = 0f;
    private Vector3 cameraOriginalLocalPos;
    private bool isSkipping = false;
    private bool fastForwardRequested = false;
    private Vector3 initialRocketPosition = new Vector3(0f, 7.0f, 0f);
    private Quaternion initialRocketRotation = Quaternion.identity;

    private readonly string[] countdownTelemetryLogs = new string[]
    {
        "T-10: MAIN PROPULSION SYSTEM PRIMED",
        "T-09: AUTOMATED FLIGHT COMPUTERS ARMED AND GO",
        "T-08: INTERNAL POWER TRANSFER NOMINAL",
        "T-07: SOUND SUPPRESSION WATER DELUGE ACTIVE",
        "T-06: ENGINE PRE-IGNITION SEQUENCE START",
        "T-05: CRYOGENIC PRESSURIZATION CONFIRMED",
        "T-04: MAIN ENGINE GIMBAL SWEEP COMPLETE",
        "T-03: ATTITUDE CONTROL SYSTEM READY",
        "T-02: AUTOMATED TRANSLUNAR TRAJECTORY LOCKED",
        "T-01: MAXIMUM THRUST // IGNITION // LIFTOFF"
    };

    private void Awake()
    {
        if (gameObject != null && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        this.enabled = true;

        if (spacecraftRoot != null && !spacecraftRoot.gameObject.activeSelf)
        {
            spacecraftRoot.gameObject.SetActive(true);
        }
    }

    private void Start()
    {
        // 1. Ensure Mission Telemetry Data
        if (MissionManager.Instance != null && MissionManager.Instance.SelectedMission != null)
        {
            missionName = MissionManager.Instance.SelectedMission.missionName;
            destination = MissionManager.Instance.SelectedMission.destination;
        }

        // 2. Discover or Anchor Space Center Launch Facility
        EnsureSpaceCenterEnvironment();

        // 3. Station Rocket 100% vertically upright on the launch pad
        // For rocket and shuttle models whose longitudinal axis is local Z (nose = +Z, engines = -Z),
        // rotating by -90 deg on X stands the vehicle vertically upright (nose = +Y into sky, engines = -Y into flame trench).
        initialRocketRotation = Quaternion.Euler(-90f, 0f, 0f);
        initialRocketPosition = new Vector3(0f, 15.1f, 0f);

        if (spacecraftRoot != null)
        {
            spacecraftRoot.rotation = initialRocketRotation;
            spacecraftRoot.position = initialRocketPosition;
        }

        // 4. Initialize Camera Anchors
        SetupCameraAnchors();

        // 5. Initialize Lighting & Atmosphere
        SetupAtmosphereAndLighting();

        // 6. Setup Buttons
        if (boardSpacecraftButton != null)
        {
            boardSpacecraftButton.onClick.RemoveAllListeners();
            boardSpacecraftButton.onClick.AddListener(OnBoardingButtonClicked);
        }

        if (skipSequenceButton != null)
        {
            skipSequenceButton.onClick.RemoveAllListeners();
            skipSequenceButton.onClick.AddListener(SkipDirectlyToLunarLanding);
        }

        if (fastForwardButton != null)
        {
            fastForwardButton.onClick.RemoveAllListeners();
            fastForwardButton.onClick.AddListener(() => fastForwardRequested = true);
        }

        if (cockpitHUD != null)
        {
            cockpitHUD.InitializeTelemetry(missionName, "THE MOON // PETAVIUS CRATER", "ORBITAL LAUNCH SYSTEM");
        }

        SetEngineEffectsActive(false, 0f);
        SetPhase(SequencePhase.PadStaging);
    }

    private void Update()
    {
        // Boarding shortcut
        if (currentPhase == SequencePhase.PadStaging && (AstroInput.GetKeyDown(KeyCode.E) || AstroInput.GetKeyDown(KeyCode.Space)))
        {
            OnBoardingButtonClicked();
        }

        // Skip directly to lunar landing
        if (AstroInput.GetKeyDown(KeyCode.Escape))
        {
            SkipDirectlyToLunarLanding();
        }

        // Fast-forward automated flight
        if (currentPhase == SequencePhase.TranslunarCruise && (AstroInput.GetKeyDown(KeyCode.Return) || AstroInput.GetKeyDown(KeyCode.KeypadEnter) || AstroInput.GetKeyDown(KeyCode.F)))
        {
            fastForwardRequested = true;
        }

        // Camera shake vibration
        if (shakeIntensity > 0.001f && mainCamera != null)
        {
            float shakeX = (Mathf.PerlinNoise(Time.time * 26f, 0f) - 0.5f) * shakeIntensity;
            float shakeY = (Mathf.PerlinNoise(0f, Time.time * 26f) - 0.5f) * shakeIntensity;
            mainCamera.transform.localPosition = cameraOriginalLocalPos + new Vector3(shakeX, shakeY, 0f);
        }
        else if (mainCamera != null && currentPhase == SequencePhase.PadStaging)
        {
            // Keep steady pad camera
        }
    }

    /// <summary>
    /// Player clicked 'BOARD & LAUNCH [E]'.
    /// Begins the full automated sequence: Ingress -> Countdown -> Liftoff -> Ascent -> Translunar Flight -> Lunar Landing!
    /// </summary>
    public void OnBoardingButtonClicked()
    {
        if (currentPhase != SequencePhase.PadStaging) return;

        SafeStartCoroutine(FullAutomatedLaunchToMoonRoutine());
    }

    private IEnumerator FullAutomatedLaunchToMoonRoutine()
    {
        // ----------------------------------------------------
        // PHASE 1: BOARDING TRANSITION & GANTRY INGRESS
        // ----------------------------------------------------
        SetPhase(SequencePhase.BoardingTransition);

        if (stagingUIPanel != null) stagingUIPanel.SetActive(false);
        if (engineAudio != null) engineAudio.TriggerCountdownBeep(720f, 0.20f);

        // Glide camera smoothly from the ground pad view up along the umbilical tower
        if (mainCamera != null && padViewAnchor != null && cockpitViewAnchor != null)
        {
            Vector3 startPos = padViewAnchor.position;
            Quaternion startRot = padViewAnchor.rotation;
            Vector3 endPos = cockpitViewAnchor.position;
            Quaternion endRot = cockpitViewAnchor.rotation;

            float elapsed = 0f;
            float duration = 2.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            mainCamera.transform.position = endPos;
            mainCamera.transform.rotation = endRot;
            cameraOriginalLocalPos = mainCamera.transform.localPosition;
        }

        // ----------------------------------------------------
        // PHASE 2: COCKPIT PRE-LAUNCH CHECKS
        // ----------------------------------------------------
        SetPhase(SequencePhase.CockpitPreLaunch);

        if (cockpitHUD != null)
        {
            cockpitHUD.gameObject.SetActive(true);
            cockpitHUD.SetSystemsStatus("[LIFE SUPPORT: NOMINAL] [AUTOMATED TLI GUIDANCE: ARMED] [CRYOTANKS: 100%]", new Color(0.35f, 1f, 0.5f));
            cockpitHUD.UpdateFlightMetrics(0f, 0.05f, 0f);
        }

        yield return new WaitForSeconds(1.8f);

        // ----------------------------------------------------
        // PHASE 3: COUNTDOWN T-10 TO T-0
        // ----------------------------------------------------
        SetPhase(SequencePhase.Countdown);

        for (int sec = 10; sec >= 1; sec--)
        {
            string statusLog = countdownTelemetryLogs[10 - sec];

            if (cockpitHUD != null)
            {
                cockpitHUD.SetCountdown(sec, statusLog);
            }

            float beepFreq = sec <= 3 ? 1200f : 880f;
            if (engineAudio != null) engineAudio.TriggerCountdownBeep(beepFreq, 0.12f);

            // Engine pre-ignition at T-5
            if (sec == 5)
            {
                currentThrust = 0.25f;
                if (engineAudio != null) engineAudio.SetThrust(currentThrust);
                SetEngineEffectsActive(true, currentThrust);
                shakeIntensity = 0.02f;
            }
            else if (sec <= 2)
            {
                currentThrust = 0.60f;
                if (engineAudio != null) engineAudio.SetThrust(currentThrust);
                SetEngineEffectsActive(true, currentThrust);
                shakeIntensity = 0.04f;
            }

            if (cockpitHUD != null)
            {
                cockpitHUD.UpdateFlightMetrics(0f, 0.05f, currentThrust);
            }

            yield return new WaitForSeconds(1.0f);
        }

        // ----------------------------------------------------
        // PHASE 4: LIFTOFF BURN & TOWER CLEARANCE
        // ----------------------------------------------------
        SetPhase(SequencePhase.LiftoffBurn);

        currentThrust = 1.0f;
        if (engineAudio != null)
        {
            engineAudio.TriggerCountdownBeep(1600f, 0.35f);
            engineAudio.SetThrust(currentThrust);
        }
        SetEngineEffectsActive(true, 1.0f);
        shakeIntensity = 0.08f;

        if (cockpitHUD != null)
        {
            cockpitHUD.SetCountdown(0, "ALL ENGINES AT 104% // LIFTOFF CONFIRMED!");
            cockpitHUD.SetSystemsStatus("[PROPULSION: FULL POWER] [TOWER CLEARANCE: NOMINAL] [G-FORCE: +2.2G]", new Color(1f, 0.8f, 0.2f));
        }

        // Smooth switch to external tracking camera showing the rocket rising above the facility
        if (mainCamera != null && externalTrackingAnchor != null)
        {
            mainCamera.transform.position = externalTrackingAnchor.position;
            mainCamera.transform.rotation = externalTrackingAnchor.rotation;
            cameraOriginalLocalPos = mainCamera.transform.localPosition;
        }

        // Ascend vertically from launch pad to clear 95m umbilical tower
        float liftoffElapsed = 0f;
        float liftoffDuration = 4.0f;
        Vector3 padPos = spacecraftRoot != null ? spacecraftRoot.position : initialRocketPosition;

        while (liftoffElapsed < liftoffDuration)
        {
            liftoffElapsed += Time.deltaTime;
            float t = liftoffElapsed / liftoffDuration;

            // Pure vertical climb: rocket stands 100% vertical, thrusters pushing straight up (+Y)
            float verticalRise = Mathf.Lerp(0f, 150f, t * t);
            if (spacecraftRoot != null)
            {
                spacecraftRoot.position = padPos + Vector3.up * verticalRise;
                spacecraftRoot.rotation = Quaternion.Euler(-90f, 0f, 0f); // 100% strictly vertical upright
            }

            currentSpeed = Mathf.Lerp(0f, 140f, t);
            currentAltitudeKm = 0.05f + (verticalRise * 0.001f);

            if (cockpitHUD != null)
            {
                cockpitHUD.UpdateFlightMetrics(currentSpeed, currentAltitudeKm, 1.0f);
            }

            // External tracking camera tracks rocket ascending straight up into the sky
            if (mainCamera != null && spacecraftRoot != null)
            {
                mainCamera.transform.LookAt(spacecraftRoot.position + Vector3.up * 6f);
            }

            yield return null;
        }

        // ----------------------------------------------------
        // PHASE 5: ATMOSPHERIC ASCENT & GRAVITY TURN
        // ----------------------------------------------------
        SetPhase(SequencePhase.AtmosphericAscent);

        if (flightControlsBar != null)
        {
            flightControlsBar.SetActive(true);
        }

        float ascentElapsed = 0f;
        float ascentDuration = 7.0f;

        while (ascentElapsed < ascentDuration)
        {
            ascentElapsed += Time.deltaTime;
            float t = ascentElapsed / ascentDuration;

            // Physics Gravity Turn: pitchover angle theta smoothly increases from 0 (vertical) to 75 deg (orbital insertion)
            float theta = Mathf.Lerp(0f, 75f, Mathf.Pow(t, 1.35f));
            float rad = theta * Mathf.Deg2Rad;

            // Flight direction vector in world coordinates (arcing from straight UP to downrange +Z)
            Vector3 flightDirection = new Vector3(0f, Mathf.Cos(rad), Mathf.Sin(rad));

            // Rocket rotation: nose strictly aligns with flight direction (-90 deg + theta on X)
            Quaternion targetRot = Quaternion.Euler(-90f + theta, 0f, 0f);

            if (spacecraftRoot != null)
            {
                spacecraftRoot.rotation = targetRot;
                // Forward thrust along flight direction: obeys real rocket propulsion
                spacecraftRoot.position += flightDirection * (currentSpeed * 0.05f * Time.deltaTime);
            }

            // Accelerate through atmosphere: 140 m/s -> 7,850 m/s orbital speed
            currentSpeed = Mathf.Lerp(140f, 7850f, t * t);
            currentAltitudeKm = Mathf.Lerp(0.20f, 180f, t);

            if (cockpitHUD != null)
            {
                cockpitHUD.UpdateFlightMetrics(currentSpeed, currentAltitudeKm, 1.0f);
            }

            if (flightControlsText != null)
            {
                flightControlsText.text =
                    $"AUTOMATED ASCENT // VELOCITY: {currentSpeed:N0} M/S   |   ALTITUDE: {currentAltitudeKm:N0} KM   |   PITCH: {theta:F1}°   |   GUIDANCE: GRAVITY TURN\n" +
                    $"<size=82%><color=#64D8FF>ATMOSPHERIC ASCENT // THRUSTERS VERTICAL-TO-ORBIT // ORBITAL INJECTION NOMINAL</color></size>";
            }

            // Cinematic chase camera following behind rocket flight path
            if (mainCamera != null && spacecraftRoot != null)
            {
                Vector3 chaseCamPos = spacecraftRoot.position - flightDirection * 48f + Vector3.up * 14f;
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, chaseCamPos, Time.deltaTime * 3.5f);
                mainCamera.transform.LookAt(spacecraftRoot.position + flightDirection * 8f);
            }

            // Sky transitions smoothly to deep space black
            if (RenderSettings.ambientLight.r > 0.06f)
            {
                RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, new Color(0.04f, 0.05f, 0.08f), Time.deltaTime * 0.8f);
            }

            yield return null;
        }

        // ----------------------------------------------------
        // PHASE 6: TRANSLUNAR INJECTION & AUTOMATED CRUISE TO MOON
        // ----------------------------------------------------
        SetPhase(SequencePhase.TranslunarCruise);

        shakeIntensity = 0.015f;
        currentSpeed = 10850f;
        distanceToMoonKm = 384400f;
        fastForwardRequested = false;

        // Align trajectory directly towards the Moon
        if (moonTransform != null && spacecraftRoot != null)
        {
            Vector3 dirToMoon = (moonTransform.position - spacecraftRoot.position).normalized;
            spacecraftRoot.rotation = Quaternion.LookRotation(dirToMoon, Vector3.up);
        }

        if (cockpitHUD != null)
        {
            cockpitHUD.SetCountdown(0, "TLI BURN COMPLETE // TRANSLUNAR TRAJECTORY LOCKED");
            cockpitHUD.SetSystemsStatus("[GUIDANCE: AUTOMATED FLIGHT TO MOON] [RCS: NOMINAL] [APPROACH: PETAVIUS CRATER]", new Color(0.35f, 1f, 0.55f));
        }

        // Automated cruise towards the Moon
        float cruiseElapsed = 0f;
        float cruiseTargetDuration = 10.0f; // 10 seconds of majestic automated spaceflight

        while (cruiseElapsed < cruiseTargetDuration && !fastForwardRequested)
        {
            cruiseElapsed += Time.deltaTime;
            float t = cruiseElapsed / cruiseTargetDuration;

            // Travel along vector to Moon
            if (spacecraftRoot != null)
            {
                spacecraftRoot.position += spacecraftRoot.forward * (Time.deltaTime * 18f);
            }

            // Telemetry count down distance to Moon
            currentSpeed = Mathf.Lerp(10850f, 11200f, t);
            currentAltitudeKm = Mathf.Lerp(180f, 384400f, t);
            distanceToMoonKm = Mathf.Lerp(384400f, 1500f, t);

            if (cockpitHUD != null)
            {
                cockpitHUD.UpdateFlightMetrics(currentSpeed, currentAltitudeKm, 0.85f);
            }

            if (flightControlsText != null)
            {
                flightControlsText.text =
                    $"AUTOMATED TRANSLUNAR GUIDANCE // DESTINATION: THE MOON // SPEED: {currentSpeed:N0} M/S // DISTANCE TO MOON: {distanceToMoonKm:N0} KM\n" +
                    $"<size=82%><color=#88EEFF>AUTOPILOT ONLINE: TRANSLUNAR COAST EN ROUTE   |   [ENTER / CLICK] Fast-Forward to Lunar Orbit   |   [ESC] Skip to Landing</color></size>";
            }

            // Camera tracks majestic spacecraft cruising to the glowing Moon
            if (mainCamera != null && spacecraftRoot != null)
            {
                Vector3 cruiseCamPos = spacecraftRoot.position - spacecraftRoot.forward * 32f + spacecraftRoot.up * 8f;
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, cruiseCamPos, Time.deltaTime * 4f);
                mainCamera.transform.LookAt(spacecraftRoot.position + spacecraftRoot.forward * 8f);
            }

            yield return null;
        }

        // ----------------------------------------------------
        // PHASE 7: LUNAR APPROACH & ORBITAL INSERTION
        // ----------------------------------------------------
        SetPhase(SequencePhase.LunarApproach);

        if (flightControlsText != null)
        {
            flightControlsText.text =
                $"LUNAR CAPTURE ACHIEVED // PERILUNE: 15 KM // SPEED: 1,680 M/S\n" +
                $"<size=82%><color=#44FF88>PETAVIUS CRATER LANDING SITE LOCKED // PREPARING MANUAL POWERED DESCENT...</color></size>";
        }

        yield return new WaitForSeconds(1.5f);

        // ----------------------------------------------------
        // PHASE 8: TRANSITION TO MANUAL LUNAR LANDING
        // ----------------------------------------------------
        yield return StartCoroutine(FadeToLunarLandingRoutine());
    }

    private IEnumerator FadeToLunarLandingRoutine()
    {
        SetPhase(SequencePhase.TransitionToLanding);
        shakeIntensity = 0f;

        if (fadeOverlay != null) fadeOverlay.gameObject.SetActive(true);

        if (transitionCalloutText != null)
        {
            transitionCalloutText.gameObject.SetActive(true);
            transitionCalloutText.text =
                "LUNAR CAPTURE ACHIEVED\n" +
                "<size=60%><color=#64D8FF>DESTINATION: PETAVIUS CRATER // ALTITUDE: 320 METERS\n" +
                "HANDING OVER TO MANUAL POWERED DESCENT CONTROL</color></size>";
        }

        float elapsed = 0f;
        float fadeDuration = 2.0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);

            if (fadeOverlay != null)
            {
                fadeOverlay.color = new Color(0.02f, 0.03f, 0.06f, alpha);
            }

            if (transitionCalloutText != null)
            {
                Color c = transitionCalloutText.color;
                c.a = alpha;
                transitionCalloutText.color = c;
            }

            if (engineAudio != null)
            {
                engineAudio.masterVolume = Mathf.Lerp(0.80f, 0f, alpha);
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.6f);

        // Load LunarLanding scene for manual landing
        string landingScene = "LunarLanding";
        if (Application.CanStreamedLevelBeLoaded(landingScene))
        {
            SceneManager.LoadScene(landingScene);
        }
        else
        {
            Debug.LogWarning($"[LaunchSequence] Scene '{landingScene}' not loaded in build settings. Falling back to MissionSelection.");
            SceneManager.LoadScene("MissionSelection");
        }
    }

    public void SkipDirectlyToLunarLanding()
    {
        if (isSkipping) return;
        isSkipping = true;

        StopAllCoroutines();
        if (engineAudio != null) engineAudio.SetThrust(0f);

        string landingScene = "LunarLanding";
        if (Application.CanStreamedLevelBeLoaded(landingScene))
        {
            SceneManager.LoadScene(landingScene);
        }
        else
        {
            SceneManager.LoadScene("MissionSelection");
        }
    }

    private void EnsureSpaceCenterEnvironment()
    {
        // Check if Space Center facility exists in the scene
        GameObject facility = GameObject.Find("SpaceCenter_RealisticLaunchFacility") ?? GameObject.Find("SpaceCenter_Facility");
        if (facility == null)
        {
            // Try loading prefab from Resources or direct scene instance
            GameObject prefab = Resources.Load<GameObject>("Environment/SpaceCenter_LaunchFacility");
            if (prefab != null)
            {
                facility = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                facility.name = "SpaceCenter_RealisticLaunchFacility";
            }
        }

        // Procedural fallback if model was not yet instantiated in scene
        if (facility == null)
        {
            facility = new GameObject("SpaceCenter_RealisticLaunchFacility");
            facility.transform.position = Vector3.zero;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Concrete Launch Apron Ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "LaunchPad_Apron";
            ground.transform.SetParent(facility.transform, false);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localScale = new Vector3(80f, 1f, 80f);
            Destroy(ground.GetComponent<Collider>());
            Material apronMat = new Material(litShader);
            apronMat.color = new Color(0.35f, 0.36f, 0.38f);
            ground.GetComponent<Renderer>().sharedMaterial = apronMat;

            // 2. Launch Mount Structure (Elevated Pad above flame trench)
            GameObject padBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            padBase.name = "Pad_ElevatedMount";
            padBase.transform.SetParent(facility.transform, false);
            padBase.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            padBase.transform.localScale = new Vector3(28f, 7.0f, 28f);
            Destroy(padBase.GetComponent<Collider>());
            Material padMat = new Material(litShader);
            padMat.color = new Color(0.55f, 0.55f, 0.58f);
            padBase.GetComponent<Renderer>().sharedMaterial = padMat;

            // 3. Iconic Umbilical Gantry Tower (NASA Red, 94.5m tall)
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "Umbilical_Gantry_Tower";
            tower.transform.SetParent(facility.transform, false);
            tower.transform.localPosition = new Vector3(12.5f, 47.25f, 0f);
            tower.transform.localScale = new Vector3(7.5f, 94.5f, 7.5f);
            Destroy(tower.GetComponent<Collider>());
            Material towerMat = new Material(litShader);
            towerMat.color = new Color(0.85f, 0.22f, 0.18f); // Iconic Gantry Red
            tower.GetComponent<Renderer>().sharedMaterial = towerMat;

            // Crew Access Arm swinging towards the rocket cabin
            GameObject accessArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            accessArm.name = "Crew_Access_Arm";
            accessArm.transform.SetParent(tower.transform, false);
            accessArm.transform.localPosition = new Vector3(-0.85f, 0.38f, 0.05f);
            accessArm.transform.localScale = new Vector3(1.2f, 0.04f, 0.4f);
            Destroy(accessArm.GetComponent<Collider>());
            Material whiteMat = new Material(litShader);
            whiteMat.color = new Color(0.92f, 0.92f, 0.95f);
            accessArm.GetComponent<Renderer>().sharedMaterial = whiteMat;

            // 4. Perimeter Lightning Masts (4 surrounding towers)
            Vector3[] mastPositions = new Vector3[]
            {
                new Vector3(45f, 45f, 45f),
                new Vector3(-45f, 45f, 45f),
                new Vector3(45f, 45f, -45f),
                new Vector3(-45f, 45f, -45f)
            };
            for (int i = 0; i < mastPositions.Length; i++)
            {
                GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mast.name = $"Lightning_Mast_{i + 1}";
                mast.transform.SetParent(facility.transform, false);
                mast.transform.localPosition = mastPositions[i];
                mast.transform.localScale = new Vector3(1.2f, 45f, 1.2f);
                Destroy(mast.GetComponent<Collider>());
                mast.GetComponent<Renderer>().sharedMaterial = whiteMat;
            }
        }

        if (facility != null)
        {
            launchFacilityRoot = facility.transform;
            launchFacilityRoot.position = Vector3.zero;
        }

        // Ensure Moon reference
        if (moonTransform == null)
        {
            GameObject m = GameObject.Find("Moon_Target");
            if (m != null) moonTransform = m.transform;
            else
            {
                GameObject moonGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                moonGO.name = "Moon_Target";
                Destroy(moonGO.GetComponent<Collider>());
                moonGO.transform.position = new Vector3(0f, 120f, 650f);
                moonGO.transform.localScale = Vector3.one * 55f;
                moonTransform = moonGO.transform;
            }
        }

        // Ensure Earth reference
        if (earthTransform == null)
        {
            GameObject e = GameObject.Find("Earth_NASA_Realistic") ?? GameObject.Find("Earth_Sphere");
            if (e != null) earthTransform = e.transform;
        }
    }

    private void SetupCameraAnchors()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Pad View Camera Anchor: Dramatic angle framing launch pad, rocket, and umbilical tower
        if (padViewAnchor == null)
        {
            if (airlockHatchAnchor != null)
            {
                padViewAnchor = airlockHatchAnchor;
            }
            else
            {
                GameObject padAnchorGO = new GameObject("Anchor_PadView");
                padAnchorGO.transform.position = new Vector3(-45f, 14f, -48f);
                padAnchorGO.transform.LookAt(new Vector3(0f, 22f, 0f));
                padViewAnchor = padAnchorGO.transform;
            }
        }
        if (padViewAnchor != null)
        {
            padViewAnchor.position = new Vector3(-45f, 14f, -48f);
            padViewAnchor.LookAt(new Vector3(0f, 22f, 0f));
        }

        // Cockpit Camera Anchor: Upper crew cabin near rocket nose cone
        if (cockpitViewAnchor == null && spacecraftRoot != null)
        {
            GameObject cockpitGO = new GameObject("Anchor_CockpitView");
            cockpitGO.transform.SetParent(spacecraftRoot, false);
            cockpitGO.transform.localPosition = new Vector3(0f, 1.2f, 6.5f);
            cockpitGO.transform.localRotation = Quaternion.identity;
            cockpitViewAnchor = cockpitGO.transform;
        }

        // External Tracking Camera Anchor: Frames vertical pad liftoff
        if (externalTrackingAnchor == null)
        {
            GameObject extGO = new GameObject("Anchor_ExternalTracking");
            extGO.transform.position = new Vector3(-65f, 16f, -65f);
            extGO.transform.LookAt(new Vector3(0f, 28f, 0f));
            externalTrackingAnchor = extGO.transform;
        }

        // Set camera initially at pad view
        if (mainCamera != null && padViewAnchor != null)
        {
            mainCamera.transform.position = padViewAnchor.position;
            mainCamera.transform.rotation = padViewAnchor.rotation;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.48f, 0.68f, 0.88f); // Daylight Florida sky
            mainCamera.farClipPlane = 3000f;
            cameraOriginalLocalPos = mainCamera.transform.localPosition;
        }
    }

    private void SetupAtmosphereAndLighting()
    {
        // Ambient lighting for Earth launch
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.65f, 0.72f, 0.82f);
    }

    private void SetPhase(SequencePhase newPhase)
    {
        currentPhase = newPhase;

        switch (newPhase)
        {
            case SequencePhase.PadStaging:
                if (stagingUIPanel != null) stagingUIPanel.SetActive(true);
                if (cockpitHUD != null) cockpitHUD.gameObject.SetActive(false);
                if (cockpitInteriorFrame != null) cockpitInteriorFrame.SetActive(false);
                if (flightControlsBar != null) flightControlsBar.SetActive(false);
                break;

            case SequencePhase.BoardingTransition:
                if (stagingUIPanel != null) stagingUIPanel.SetActive(false);
                break;

            case SequencePhase.CockpitPreLaunch:
            case SequencePhase.Countdown:
                if (cockpitHUD != null) cockpitHUD.gameObject.SetActive(true);
                if (cockpitInteriorFrame != null) cockpitInteriorFrame.SetActive(true);
                break;

            case SequencePhase.LiftoffBurn:
            case SequencePhase.AtmosphericAscent:
            case SequencePhase.TranslunarCruise:
            case SequencePhase.LunarApproach:
                if (cockpitInteriorFrame != null) cockpitInteriorFrame.SetActive(false);
                break;
        }
    }

    private void SetEngineEffectsActive(bool active, float intensity)
    {
        if (engineThrusterLights != null)
        {
            foreach (Light l in engineThrusterLights)
            {
                if (l != null)
                {
                    l.enabled = active;
                    l.intensity = intensity * 5f;
                }
            }
        }

        if (engineExhaustPlumes != null)
        {
            foreach (GameObject p in engineExhaustPlumes)
            {
                if (p != null)
                {
                    p.SetActive(active);
                    p.transform.localScale = new Vector3(
                        1f * (0.6f + intensity * 0.4f),
                        1f * (0.5f + intensity * 1.5f),
                        1f * (0.6f + intensity * 0.4f));
                }
            }
        }

        if (engineExhaustParticles != null)
        {
            foreach (ParticleSystem ps in engineExhaustParticles)
            {
                if (ps != null)
                {
                    if (active && !ps.isPlaying) ps.Play();
                    else if (!active && ps.isPlaying) ps.Stop();
                }
            }
        }
    }

    private void SafeStartCoroutine(IEnumerator routine)
    {
        if (gameObject != null && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        this.enabled = true;

        if (isActiveAndEnabled)
        {
            StartCoroutine(routine);
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null && canvas.isActiveAndEnabled)
        {
            canvas.GetComponent<MonoBehaviour>().StartCoroutine(routine);
            return;
        }

        gameObject.SetActive(true);
        StartCoroutine(routine);
    }
}

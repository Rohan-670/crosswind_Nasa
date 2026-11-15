using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Automation builder for the complete SpacecraftFlight gameplay scene:
/// constructs the arcade flight test environment, 3D checkpoints, hazard zones,
/// route guide line, first-person cockpit camera, top-right mini-map, and HUD telemetry.
/// </summary>
public class SpacecraftFlightSceneBuilder
{
    private const string ScenesPath = "Assets/Scenes";
    private const string MaterialsPath = "Assets/Materials";
    private const string PrefabsPath = "Assets/Prefabs/Flight";

    [MenuItem("ASTROBASE/Missions/Build SpacecraftFlight Scene")]
    public static void BuildSceneInteractive()
    {
        ExecuteBuild(false);
    }

    public static void BuildSceneBatch()
    {
        ExecuteBuild(true);
    }

    [InitializeOnLoadMethod]
    private static void AutoBuildOnFirstLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(ScenesPath + "/SpacecraftFlight.unity"))
            {
                Debug.Log("[ASTROBASE] Initializing SpacecraftFlight gameplay scene...");
                ExecuteBuild(true);
            }
        };
    }

    public static void ExecuteBuild(bool isBatch)
    {
        try
        {
            EnsureDirectory(ScenesPath);
            EnsureDirectory(MaterialsPath);
            EnsureDirectory(PrefabsPath);

            TMP_FontAsset font = FindTMPFont();

            BuildGameplayScene(font);
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: SpacecraftFlight Gameplay Built!",
                    "Scene successfully created at:\n  Assets/Scenes/SpacecraftFlight.unity\n\n" +
                    "  ✓ Arcade Spacecraft Controller (W/S/A/D, Space/Ctrl, Shift Boost, V View Toggle)\n" +
                    "  ✓ First-Person Cockpit Camera & Dashboard\n" +
                    "  ✓ Top-Right Navigation Mini-Map with Route, Checkpoints & Hazards\n" +
                    "  ✓ RouteManager with Cross-Track Deviation Detection & Recovery Warning\n" +
                    "  ✓ CheckpointManager with 4 Guided Gates & Final Destination Beacon\n" +
                    "  ✓ HazardManager with Debris & Radiation Storms inflicting Hull Damage\n" +
                    "  ✓ Dynamic 3D Screen Marker & Mission Success/Failure Modals\n\n" +
                    "Press Play ▶ in Unity Editor to test flight controls!",
                    "OK");
            }
            Debug.Log("[ASTROBASE] SpacecraftFlight gameplay scene built successfully.");
        }
        catch (System.Exception ex)
        {
            if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", ex.Message, "OK");
            Debug.LogError("[ASTROBASE] SpacecraftFlight build failed: " + ex);
        }
    }

    private static void BuildGameplayScene(TMP_FontAsset font)
    {
        string scenePath = ScenesPath + "/SpacecraftFlight.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Lighting & Environment
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.04f, 0.05f, 0.08f);

        GameObject sunGO = new GameObject("Sunlight");
        Light sunLight = sunGO.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1.0f, 0.97f, 0.90f);
        sunLight.intensity = 1.35f;
        sunGO.transform.rotation = Quaternion.Euler(35f, -40f, 0f);

        // Starfield
        BuildStarfield("DeepSpaceStarfield", 3000, 500f);

        // Distant Earth & Moon
        BuildDistantEarth(new Vector3(0f, -40f, -380f), 85f);
        BuildDistantMoon(new Vector3(-60f, 35f, 750f), 55f);

        // 2. Spacecraft Player Object
        GameObject spacecraftRoot = new GameObject("Spacecraft_Player");
        spacecraftRoot.transform.position = Vector3.zero;
        spacecraftRoot.transform.rotation = Quaternion.identity;

        SpacecraftController shipController = spacecraftRoot.AddComponent<SpacecraftController>();
        shipController.maxForwardSpeed = 55f;
        shipController.maxReverseSpeed = 22f;
        shipController.accelerationRate = 28f;
        shipController.naturalDamping = 12f;
        shipController.boostMultiplier = 1.65f;
        shipController.maxFuel = 100f;
        shipController.maxHull = 100f;

        // Visual Model
        BuildSpacecraftMesh(spacecraftRoot.transform);

        // Thruster FX & Audio
        List<Light> thrusterLights = new List<Light>();
        List<GameObject> plumeObjects = new List<GameObject>();
        BuildEngineThrusters(spacecraftRoot.transform, thrusterLights, plumeObjects);
        shipController.thrusterLights = thrusterLights.ToArray();
        shipController.exhaustPlumes = plumeObjects.ToArray();

        ProceduralEngineAudio engineAudio = spacecraftRoot.AddComponent<ProceduralEngineAudio>();
        engineAudio.masterVolume = 0.75f;
        shipController.engineAudio = engineAudio;

        // 3. Cockpit & Cameras
        GameObject cockpitAnchor = new GameObject("Anchor_CockpitEye");
        cockpitAnchor.transform.SetParent(spacecraftRoot.transform, false);
        cockpitAnchor.transform.localPosition = new Vector3(0f, 2.5f, 6.2f);
        cockpitAnchor.transform.localRotation = Quaternion.identity;
        shipController.cockpitEyeAnchor = cockpitAnchor.transform;
        shipController.startInCockpitView = false;

        // Cockpit Camera (First-Person)
        GameObject cockCamGO = new GameObject("Camera_Cockpit_FirstPerson");
        cockCamGO.transform.SetParent(cockpitAnchor.transform, false);
        cockCamGO.transform.localPosition = Vector3.zero;
        cockCamGO.transform.localRotation = Quaternion.identity;
        Camera cockCam = cockCamGO.AddComponent<Camera>();
        cockCam.clearFlags = CameraClearFlags.SolidColor;
        cockCam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
        cockCam.nearClipPlane = 0.05f;
        cockCam.farClipPlane = 2000f;
        cockCamGO.AddComponent<AudioListener>();
        cockCamGO.SetActive(false);
        shipController.cockpitCamera = cockCam;

        // Chase Camera (Third-Person)
        GameObject chaseCamGO = new GameObject("Camera_Chase_ThirdPerson");
        chaseCamGO.transform.SetParent(spacecraftRoot.transform, false);
        chaseCamGO.transform.localPosition = new Vector3(0f, 6.5f, -24.0f);
        chaseCamGO.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
        Camera chaseCam = chaseCamGO.AddComponent<Camera>();
        chaseCam.clearFlags = CameraClearFlags.SolidColor;
        chaseCam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
        chaseCam.nearClipPlane = 0.1f;
        chaseCam.farClipPlane = 2000f;
        chaseCamGO.SetActive(true);
        shipController.chaseCamera = chaseCam;

        // Cockpit Interior Frame
        GameObject cockpitFrame = BuildCockpitInteriorFrame(spacecraftRoot.transform);
        cockpitFrame.SetActive(false);
        shipController.cockpitInteriorFrame = cockpitFrame;

        // 4. Checkpoints & Route
        List<Checkpoint> checkpoints = new List<Checkpoint>();
        List<Transform> routeWaypoints = new List<Transform>();

        // Checkpoint 0: Earth Orbit Gate
        checkpoints.Add(CreateCheckpoint("Checkpoint_0_LEO_Gate", new Vector3(0f, 0f, 85f), 0, "LEO Departure Gate", false, 24f));
        // Checkpoint 1: Waypoint Alpha
        checkpoints.Add(CreateCheckpoint("Checkpoint_1_Alpha", new Vector3(65f, 15f, 210f), 1, "Translunar Waypoint Alpha", false, 24f));
        // Checkpoint 2: Nav Beacon Beta
        checkpoints.Add(CreateCheckpoint("Checkpoint_2_Beta", new Vector3(15f, -12f, 365f), 2, "Nav Beacon Beta", false, 24f));
        // Checkpoint 3: Destination Gate (Final)
        checkpoints.Add(CreateCheckpoint("Checkpoint_3_Destination", new Vector3(-45f, 22f, 530f), 3, "Lunar Insertion Gate", true, 30f));

        foreach (Checkpoint cp in checkpoints)
        {
            routeWaypoints.Add(cp.transform);
        }

        // Managers GameObject
        GameObject mgrsGO = new GameObject("Navigation_Managers");

        LineRenderer lr = mgrsGO.AddComponent<LineRenderer>();
        Material corridorMat = GetOrCreateUnlitMaterial("Route_Corridor_Mat", new Color(0.25f, 0.85f, 1.0f, 0.65f));
        lr.sharedMaterial = corridorMat;

        RouteManager routeMgr = mgrsGO.AddComponent<RouteManager>();
        routeMgr.routeLineRenderer = lr;
        routeMgr.spacecraft = shipController;
        routeMgr.routeWaypoints = routeWaypoints;
        routeMgr.warningCorridorRadius = 45f;
        routeMgr.criticalCorridorRadius = 85f;
        routeMgr.recoveryTimeLimit = 12f;

        CheckpointManager cpMgr = mgrsGO.AddComponent<CheckpointManager>();
        cpMgr.spacecraft = shipController;
        cpMgr.checkpoints = checkpoints;
        cpMgr.fuelBonusPerCheckpoint = 15f;

        // 5. Hazards
        List<HazardZone> hazardZones = new List<HazardZone>();
        hazardZones.Add(CreateHazardZone("Hazard_DebrisField", new Vector3(38f, 8f, 150f), 36f, "Micrometeorite Debris Cluster", HazardZone.HazardType.SpaceDebrisField, 7.5f));
        hazardZones.Add(CreateHazardZone("Hazard_RadiationStorm", new Vector3(-20f, -6f, 290f), 42f, "Solar Radiation Pocket", HazardZone.HazardType.RadiationStorm, 9.0f));

        HazardManager hazardMgr = mgrsGO.AddComponent<HazardManager>();
        hazardMgr.spacecraft = shipController;
        hazardMgr.hazardZones = hazardZones;

        // 6. UI Canvas & EventSystem
        EnsureEventSystem();
        GameObject canvasGO = BuildFlightCanvas(font);

        // 7. Wire FlightMiniMap
        FlightMiniMap miniMap = canvasGO.transform.Find("TopRightMiniMap").GetComponent<FlightMiniMap>();
        miniMap.spacecraft = shipController;
        miniMap.routeManager = routeMgr;
        miniMap.checkpointManager = cpMgr;
        miniMap.hazardManager = hazardMgr;

        // 8. Wire FlightHUDController
        FlightHUDController hud = canvasGO.GetComponent<FlightHUDController>();
        hud.spacecraft = shipController;
        hud.routeManager = routeMgr;
        hud.checkpointManager = cpMgr;
        hud.hazardManager = hazardMgr;
        hud.flightCamera = cockCam;

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    // ===================================================================
    // CHECKPOINT & HAZARD BUILDERS
    // ===================================================================

    private static Checkpoint CreateCheckpoint(string name, Vector3 pos, int index, string cpName, bool isFinal, float radius)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        // Holographic ring visual (Torus or Cylinder ring)
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "HoloRing";
        ring.transform.SetParent(go.transform, false);
        ring.transform.localScale = new Vector3(radius, 0.4f, radius);
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Object.DestroyImmediate(ring.GetComponent<Collider>());

        Color baseCol = isFinal ? new Color(1.0f, 0.85f, 0.25f, 0.75f) : new Color(0.25f, 0.85f, 1.0f, 0.75f);
        Material mat = GetOrCreateUnlitMaterial(isFinal ? "Checkpoint_Destination_Mat" : "Checkpoint_Ring_Mat", baseCol);
        ring.GetComponent<Renderer>().sharedMaterial = mat;

        // Beacon light
        GameObject lightGO = new GameObject("BeaconLight");
        lightGO.transform.SetParent(go.transform, false);
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = baseCol;
        l.range = radius * 1.8f;
        l.intensity = 2.0f;

        Checkpoint cp = go.AddComponent<Checkpoint>();
        cp.checkpointIndex = index;
        cp.checkpointName = cpName;
        cp.isFinalDestination = isFinal;
        cp.triggerRadius = radius;
        cp.ringRenderer = ring.GetComponent<Renderer>();
        cp.beaconLight = l;

        return cp;
    }

    private static HazardZone CreateHazardZone(string name, Vector3 pos, float radius, string hName, HazardZone.HazardType type, float dps)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        // Visual danger boundary sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "DangerBoundary";
        sphere.transform.SetParent(go.transform, false);
        sphere.transform.localScale = Vector3.one * (radius * 2f);
        Object.DestroyImmediate(sphere.GetComponent<Collider>());

        Color dangerCol = new Color(1.0f, 0.22f, 0.18f, 0.22f);
        Material mat = GetOrCreateUnlitMaterial("Hazard_Danger_Mat", dangerCol);
        sphere.GetComponent<Renderer>().sharedMaterial = mat;

        // Scattered debris rocks inside
        for (int i = 0; i < 6; i++)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = $"Debris_{i}";
            rock.transform.SetParent(go.transform, false);
            rock.transform.localPosition = Random.insideUnitSphere * (radius * 0.7f);
            rock.transform.localScale = Vector3.one * Random.Range(2.5f, 5.5f);
            rock.transform.localRotation = Random.rotation;
            Object.DestroyImmediate(rock.GetComponent<Collider>());

            Material rockMat = GetOrCreateLitMaterial("Hazard_Rock_Mat", new Color(0.25f, 0.24f, 0.26f), 0.2f, 0.2f, null);
            rock.GetComponent<Renderer>().sharedMaterial = rockMat;
        }

        // Danger pulsing light
        GameObject lightGO = new GameObject("DangerLight");
        lightGO.transform.SetParent(go.transform, false);
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1.0f, 0.25f, 0.20f);
        l.range = radius * 1.5f;
        l.intensity = 2.2f;

        HazardZone hz = go.AddComponent<HazardZone>();
        hz.hazardName = hName;
        hz.hazardType = type;
        hz.radius = radius;
        hz.damagePerSecond = dps;
        hz.boundaryRenderer = sphere.GetComponent<Renderer>();
        hz.dangerLight = l;

        return hz;
    }

    // ===================================================================
    // UI CANVAS & RADAR BUILDER
    // ===================================================================

    private static GameObject BuildFlightCanvas(TMP_FontAsset font)
    {
        GameObject canvasGO = new GameObject("FlightHUD_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        FlightHUDController hud = canvasGO.AddComponent<FlightHUDController>();

        // 1. TOP-LEFT: FLIGHT TELEMETRY & VITALS
        GameObject vitalsPanel = CreateUIObject("VitalsPanel", canvasGO.transform);
        SetRect(vitalsPanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.74f), new Vector2(0.32f, 0.98f));
        Image vBg = vitalsPanel.AddComponent<Image>();
        vBg.color = new Color(0.04f, 0.07f, 0.12f, 0.88f);
        vitalsPanel.AddComponent<Outline>().effectColor = new Color(0.25f, 0.75f, 1.0f, 0.75f);

        // Header
        GameObject titleGO = CreateUIObject("Title", vitalsPanel.transform);
        SetRect(titleGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.96f));
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTMP.font = font;
        titleTMP.text = "ASTROBASE // TRANSLUNAR FLIGHT CORRIDOR";
        titleTMP.fontSize = 15f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(0.35f, 0.90f, 1.0f);

        // Speed Text
        GameObject spdGO = CreateUIObject("SpeedText", vitalsPanel.transform);
        SetRect(spdGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.65f), new Vector2(0.96f, 0.80f));
        TextMeshProUGUI spdTMP = spdGO.AddComponent<TextMeshProUGUI>();
        if (font != null) spdTMP.font = font;
        spdTMP.text = "SPEED: 0.0 M/S";
        spdTMP.fontSize = 15f;
        spdTMP.color = Color.white;
        hud.speedText = spdTMP;

        // Throttle Text & Bar
        GameObject thrGO = CreateUIObject("ThrottleText", vitalsPanel.transform);
        SetRect(thrGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.48f), new Vector2(0.40f, 0.63f));
        TextMeshProUGUI thrTMP = thrGO.AddComponent<TextMeshProUGUI>();
        if (font != null) thrTMP.font = font;
        thrTMP.text = "THROTTLE: 0%";
        thrTMP.fontSize = 13f;
        thrTMP.color = new Color(0.85f, 0.9f, 0.95f);
        hud.throttleText = thrTMP;

        GameObject thrBarBg = CreateUIObject("ThrottleBarBg", vitalsPanel.transform);
        SetRect(thrBarBg.GetComponent<RectTransform>(), new Vector2(0.42f, 0.50f), new Vector2(0.96f, 0.61f));
        thrBarBg.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f);
        GameObject thrBarFill = CreateUIObject("Fill", thrBarBg.transform);
        StretchFull(thrBarFill.GetComponent<RectTransform>());
        Image tbFill = thrBarFill.AddComponent<Image>();
        tbFill.type = Image.Type.Filled;
        tbFill.fillMethod = Image.FillMethod.Horizontal;
        tbFill.fillAmount = 0f;
        tbFill.color = new Color(0.2f, 0.7f, 1.0f);
        hud.throttleBar = tbFill;

        // Fuel Text & Bar
        GameObject fuelGO = CreateUIObject("FuelText", vitalsPanel.transform);
        SetRect(fuelGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.31f), new Vector2(0.40f, 0.46f));
        TextMeshProUGUI fuelTMP = fuelGO.AddComponent<TextMeshProUGUI>();
        if (font != null) fuelTMP.font = font;
        fuelTMP.text = "FUEL: 100%";
        fuelTMP.fontSize = 13f;
        fuelTMP.color = new Color(0.35f, 0.9f, 1f);
        hud.fuelText = fuelTMP;

        GameObject fuelBarBg = CreateUIObject("FuelBarBg", vitalsPanel.transform);
        SetRect(fuelBarBg.GetComponent<RectTransform>(), new Vector2(0.42f, 0.33f), new Vector2(0.96f, 0.44f));
        fuelBarBg.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f);
        GameObject fuelBarFill = CreateUIObject("Fill", fuelBarBg.transform);
        StretchFull(fuelBarFill.GetComponent<RectTransform>());
        Image fbFill = fuelBarFill.AddComponent<Image>();
        fbFill.type = Image.Type.Filled;
        fbFill.fillMethod = Image.FillMethod.Horizontal;
        fbFill.fillAmount = 1f;
        fbFill.color = new Color(0.2f, 0.8f, 1.0f);
        hud.fuelBar = fbFill;

        // Hull Text & Bar
        GameObject hullGO = CreateUIObject("HullText", vitalsPanel.transform);
        SetRect(hullGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.14f), new Vector2(0.40f, 0.29f));
        TextMeshProUGUI hullTMP = hullGO.AddComponent<TextMeshProUGUI>();
        if (font != null) hullTMP.font = font;
        hullTMP.text = "HULL: 100%";
        hullTMP.fontSize = 13f;
        hullTMP.color = new Color(0.35f, 1f, 0.55f);
        hud.hullText = hullTMP;

        GameObject hullBarBg = CreateUIObject("HullBarBg", vitalsPanel.transform);
        SetRect(hullBarBg.GetComponent<RectTransform>(), new Vector2(0.42f, 0.16f), new Vector2(0.96f, 0.27f));
        hullBarBg.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f);
        GameObject hullBarFill = CreateUIObject("Fill", hullBarBg.transform);
        StretchFull(hullBarFill.GetComponent<RectTransform>());
        Image hbFill = hullBarFill.AddComponent<Image>();
        hbFill.type = Image.Type.Filled;
        hbFill.fillMethod = Image.FillMethod.Horizontal;
        hbFill.fillAmount = 1f;
        hbFill.color = new Color(0.25f, 1f, 0.45f);
        hud.hullBar = hbFill;

        // 2. TOP-CENTER: OBJECTIVE BANNER
        GameObject objPanel = CreateUIObject("ObjectivePanel", canvasGO.transform);
        SetRect(objPanel.GetComponent<RectTransform>(), new Vector2(0.34f, 0.90f), new Vector2(0.70f, 0.98f));
        Image oBg = objPanel.AddComponent<Image>();
        oBg.color = new Color(0.04f, 0.08f, 0.14f, 0.88f);
        objPanel.AddComponent<Outline>().effectColor = new Color(0.25f, 0.75f, 1.0f, 0.6f);

        GameObject objTextGO = CreateUIObject("ObjectiveText", objPanel.transform);
        SetRect(objTextGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.45f), new Vector2(0.96f, 0.90f));
        TextMeshProUGUI objTMP = objTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) objTMP.font = font;
        objTMP.text = "OBJECTIVE [1/4]: NAVIGATE THROUGH LEO DEPARTURE GATE";
        objTMP.fontSize = 14f;
        objTMP.fontStyle = FontStyles.Bold;
        objTMP.alignment = TextAlignmentOptions.Center;
        objTMP.color = new Color(1.0f, 0.88f, 0.35f);
        hud.objectiveText = objTMP;

        GameObject distTextGO = CreateUIObject("DistanceText", objPanel.transform);
        SetRect(distTextGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.45f));
        TextMeshProUGUI distTMP = distTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) distTMP.font = font;
        distTMP.text = "TARGET DISTANCE: 85 M";
        distTMP.fontSize = 12f;
        distTMP.alignment = TextAlignmentOptions.Center;
        distTMP.color = new Color(0.75f, 0.88f, 1.0f);
        hud.checkpointDistanceText = distTMP;

        // 3. TOP-RIGHT: NAVIGATION MINI-MAP (RADAR)
        GameObject miniMapGO = CreateUIObject("TopRightMiniMap", canvasGO.transform);
        RectTransform mapRT = miniMapGO.GetComponent<RectTransform>();
        mapRT.anchorMin = new Vector2(1f, 1f);
        mapRT.anchorMax = new Vector2(1f, 1f);
        mapRT.pivot = new Vector2(1f, 1f);
        mapRT.anchoredPosition = new Vector2(-25f, -25f);
        mapRT.sizeDelta = new Vector2(240f, 240f);

        Image mapBg = miniMapGO.AddComponent<Image>();
        mapBg.color = new Color(0.03f, 0.06f, 0.11f, 0.92f);
        miniMapGO.AddComponent<Outline>().effectColor = new Color(0.25f, 0.85f, 1.0f, 0.85f);

        // Radar grid circles
        GameObject gridGO = CreateUIObject("RadarGrid", miniMapGO.transform);
        StretchFull(gridGO.GetComponent<RectTransform>());

        // Map label
        GameObject mapLabel = CreateUIObject("RadarLabel", miniMapGO.transform);
        SetRect(mapLabel.GetComponent<RectTransform>(), new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.97f));
        TextMeshProUGUI mlTMP = mapLabel.AddComponent<TextMeshProUGUI>();
        if (font != null) mlTMP.font = font;
        mlTMP.text = "TACTICAL NAV RADAR // 220M";
        mlTMP.fontSize = 11f;
        mlTMP.color = new Color(0.4f, 0.85f, 1.0f, 0.8f);

        // Blip templates
        GameObject playerBlip = CreateUIObject("PlayerBlip", miniMapGO.transform);
        playerBlip.GetComponent<RectTransform>().sizeDelta = new Vector2(14f, 14f);
        Image pbImg = playerBlip.AddComponent<Image>();
        pbImg.color = new Color(0.25f, 1.0f, 0.5f); // Bright green player triangle

        GameObject cpTemplate = CreateUIObject("CheckpointTemplate", miniMapGO.transform);
        cpTemplate.GetComponent<RectTransform>().sizeDelta = new Vector2(12f, 12f);
        Image cpImg = cpTemplate.AddComponent<Image>();
        cpImg.color = new Color(0.25f, 0.85f, 1.0f);
        cpTemplate.SetActive(false);

        GameObject destTemplate = CreateUIObject("DestinationTemplate", miniMapGO.transform);
        destTemplate.GetComponent<RectTransform>().sizeDelta = new Vector2(15f, 15f);
        Image destImg = destTemplate.AddComponent<Image>();
        destImg.color = new Color(1.0f, 0.85f, 0.25f);
        destTemplate.SetActive(false);

        GameObject hzTemplate = CreateUIObject("HazardTemplate", miniMapGO.transform);
        hzTemplate.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 40f);
        Image hzImg = hzTemplate.AddComponent<Image>();
        hzImg.color = new Color(1.0f, 0.2f, 0.2f, 0.35f);
        hzTemplate.SetActive(false);

        GameObject dotTemplate = CreateUIObject("RouteDotTemplate", miniMapGO.transform);
        dotTemplate.GetComponent<RectTransform>().sizeDelta = new Vector2(5f, 5f);
        Image dotImg = dotTemplate.AddComponent<Image>();
        dotImg.color = new Color(0.25f, 0.75f, 1.0f, 0.6f);
        dotTemplate.SetActive(false);

        FlightMiniMap mm = miniMapGO.AddComponent<FlightMiniMap>();
        mm.mapContainer = mapRT;
        mm.mapWorldRange = 240f;
        mm.centerOnPlayer = true;
        mm.playerBlip = playerBlip.GetComponent<RectTransform>();
        mm.checkpointBlipTemplate = cpTemplate.GetComponent<RectTransform>();
        mm.destinationBlip = destTemplate.GetComponent<RectTransform>();
        mm.hazardBlipTemplate = hzTemplate.GetComponent<RectTransform>();
        mm.routeDotTemplate = dotTemplate.GetComponent<RectTransform>();

        // 4. CENTER SCREEN: 3D FLOATING DESTINATION MARKER
        GameObject markerGO = CreateUIObject("DestinationMarker", canvasGO.transform);
        RectTransform markerRT = markerGO.GetComponent<RectTransform>();
        markerRT.sizeDelta = new Vector2(120f, 60f);
        Image mBg = markerGO.AddComponent<Image>();
        mBg.color = new Color(0.04f, 0.08f, 0.15f, 0.65f);
        markerGO.AddComponent<Outline>().effectColor = new Color(0.25f, 0.85f, 1.0f, 0.8f);

        GameObject mLabel = CreateUIObject("Label", markerGO.transform);
        SetRect(mLabel.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 1f));
        TextMeshProUGUI mLabelTMP = mLabel.AddComponent<TextMeshProUGUI>();
        if (font != null) mLabelTMP.font = font;
        mLabelTMP.fontSize = 11f;
        mLabelTMP.fontStyle = FontStyles.Bold;
        mLabelTMP.alignment = TextAlignmentOptions.Center;
        mLabelTMP.color = new Color(0.35f, 0.9f, 1f);

        GameObject mDist = CreateUIObject("Distance", markerGO.transform);
        SetRect(mDist.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.5f));
        TextMeshProUGUI mDistTMP = mDist.AddComponent<TextMeshProUGUI>();
        if (font != null) mDistTMP.font = font;
        mDistTMP.fontSize = 13f;
        mDistTMP.fontStyle = FontStyles.Bold;
        mDistTMP.alignment = TextAlignmentOptions.Center;
        mDistTMP.color = Color.white;

        hud.destinationMarker = markerRT;
        hud.destinationMarkerLabel = mLabelTMP;
        hud.destinationMarkerDistance = mDistTMP;

        // 5. DEVIATION WARNING BANNER
        GameObject devBanner = CreateUIObject("DeviationWarningBanner", canvasGO.transform);
        SetRect(devBanner.GetComponent<RectTransform>(), new Vector2(0.28f, 0.68f), new Vector2(0.72f, 0.80f));
        Image devBg = devBanner.AddComponent<Image>();
        devBg.color = new Color(0.35f, 0.06f, 0.06f, 0.92f);
        devBanner.AddComponent<Outline>().effectColor = new Color(1.0f, 0.25f, 0.25f, 1.0f);

        GameObject devTitle = CreateUIObject("Title", devBanner.transform);
        SetRect(devTitle.GetComponent<RectTransform>(), new Vector2(0.04f, 0.50f), new Vector2(0.96f, 0.90f));
        TextMeshProUGUI devTitleTMP = devTitle.AddComponent<TextMeshProUGUI>();
        if (font != null) devTitleTMP.font = font;
        devTitleTMP.text = "[!] TRAJECTORY DEVIATION DETECTED // OFF PLANNED FLIGHT CORRIDOR";
        devTitleTMP.fontSize = 15f;
        devTitleTMP.fontStyle = FontStyles.Bold;
        devTitleTMP.alignment = TextAlignmentOptions.Center;
        devTitleTMP.color = new Color(1.0f, 0.45f, 0.40f);
        hud.deviationTitleText = devTitleTMP;

        GameObject devTimer = CreateUIObject("Timer", devBanner.transform);
        SetRect(devTimer.GetComponent<RectTransform>(), new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.50f));
        TextMeshProUGUI devTimerTMP = devTimer.AddComponent<TextMeshProUGUI>();
        if (font != null) devTimerTMP.font = font;
        devTimerTMP.text = "RETURN TO CORRIDOR: 00:12S (MISSION ABORT IMMINENT)";
        devTimerTMP.fontSize = 14f;
        devTimerTMP.alignment = TextAlignmentOptions.Center;
        devTimerTMP.color = Color.white;
        hud.deviationTimerText = devTimerTMP;
        hud.deviationBanner = devBanner;

        // 6. HAZARD WARNING BANNER
        GameObject hazBanner = CreateUIObject("HazardWarningBanner", canvasGO.transform);
        SetRect(hazBanner.GetComponent<RectTransform>(), new Vector2(0.28f, 0.81f), new Vector2(0.72f, 0.88f));
        Image hazBg = hazBanner.AddComponent<Image>();
        hazBg.color = new Color(0.40f, 0.15f, 0.04f, 0.92f);
        hazBanner.AddComponent<Outline>().effectColor = new Color(1.0f, 0.60f, 0.15f, 1.0f);

        GameObject hazTitle = CreateUIObject("Title", hazBanner.transform);
        StretchFull(hazTitle.GetComponent<RectTransform>());
        TextMeshProUGUI hazTitleTMP = hazTitle.AddComponent<TextMeshProUGUI>();
        if (font != null) hazTitleTMP.font = font;
        hazTitleTMP.text = "[!] DANGER: HAZARD ZONE PENETRATION // HULL DEGRADING";
        hazTitleTMP.fontSize = 14f;
        hazTitleTMP.fontStyle = FontStyles.Bold;
        hazTitleTMP.alignment = TextAlignmentOptions.Center;
        hazTitleTMP.color = new Color(1.0f, 0.75f, 0.25f);
        hud.hazardTitleText = hazTitleTMP;
        hud.hazardBanner = hazBanner;

        // 7. BOTTOM CONTROLS HINT
        GameObject hintGO = CreateUIObject("ControlsHint", canvasGO.transform);
        SetRect(hintGO.GetComponent<RectTransform>(), new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.07f));
        Image hintBg = hintGO.AddComponent<Image>();
        hintBg.color = new Color(0.04f, 0.07f, 0.12f, 0.82f);
        hintGO.AddComponent<Outline>().effectColor = new Color(0.2f, 0.5f, 0.8f, 0.5f);

        GameObject hintTextGO = CreateUIObject("Text", hintGO.transform);
        StretchFull(hintTextGO.GetComponent<RectTransform>());
        TextMeshProUGUI htTMP = hintTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) htTMP.font = font;
        htTMP.text = "CONTROLS: [W/S] Throttle | [A/D] Steer | [Space/Ctrl] Vertical | [Shift] Boost | [Q/E] Roll | [V] Camera View";
        htTMP.fontSize = 13f;
        htTMP.alignment = TextAlignmentOptions.Center;
        htTMP.color = new Color(0.75f, 0.88f, 1.0f);
        hud.controlsHintText = htTMP;

        GameObject camTextGO = CreateUIObject("CameraText", canvasGO.transform);
        SetRect(camTextGO.GetComponent<RectTransform>(), new Vector2(0.02f, 0.69f), new Vector2(0.32f, 0.73f));
        TextMeshProUGUI ctTMP = camTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) ctTMP.font = font;
        ctTMP.text = "VIEW: FIRST-PERSON COCKPIT [V TO TOGGLE]";
        ctTMP.fontSize = 12f;
        ctTMP.color = new Color(0.4f, 0.8f, 1.0f);
        hud.cameraModeText = ctTMP;

        // 8. MODALS (FAILURE & SUCCESS)
        hud.missionFailedModal = BuildFailureModal(canvasGO.transform, font, hud);
        hud.missionSuccessModal = BuildSuccessModal(canvasGO.transform, font, hud);

        return canvasGO;
    }

    private static GameObject BuildFailureModal(Transform parent, TMP_FontAsset font, FlightHUDController hud)
    {
        GameObject modalGO = CreateUIObject("MissionFailedModal", parent);
        StretchFull(modalGO.GetComponent<RectTransform>());

        Image backdrop = modalGO.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject card = CreateUIObject("Card", modalGO.transform);
        SetRect(card.GetComponent<RectTransform>(), new Vector2(0.30f, 0.30f), new Vector2(0.70f, 0.70f));
        Image cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.12f, 0.05f, 0.05f, 0.98f);
        card.AddComponent<Outline>().effectColor = new Color(1.0f, 0.25f, 0.25f, 0.95f);

        GameObject head = CreateUIObject("Header", card.transform);
        SetRect(head.GetComponent<RectTransform>(), new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.95f));
        TextMeshProUGUI hTMP = head.AddComponent<TextMeshProUGUI>();
        if (font != null) hTMP.font = font;
        hTMP.text = "[ MISSION ABORT // FLIGHT FAILURE ]";
        hTMP.fontSize = 18f;
        hTMP.fontStyle = FontStyles.Bold;
        hTMP.alignment = TextAlignmentOptions.Center;
        hTMP.color = new Color(1.0f, 0.35f, 0.35f);

        GameObject reason = CreateUIObject("Reason", card.transform);
        SetRect(reason.GetComponent<RectTransform>(), new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.75f));
        TextMeshProUGUI rTMP = reason.AddComponent<TextMeshProUGUI>();
        if (font != null) rTMP.font = font;
        rTMP.text = "Flight failure details.";
        rTMP.fontSize = 15f;
        rTMP.alignment = TextAlignmentOptions.Center;
        rTMP.color = new Color(0.88f, 0.88f, 0.92f);
        hud.failureReasonText = rTMP;

        GameObject btnRetry = CreateButton("RetryButton", "RETRY FLIGHT", font, card.transform,
            new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.24f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.15f, 0.50f, 0.85f, 0.95f));
        hud.retryFlightButton = btnRetry.GetComponent<Button>();
        hud.retrySceneName = "LaunchSequence";

        GameObject btnMenu = CreateButton("MenuButton", "RETURN TO MISSIONS", font, card.transform,
            new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.24f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.25f, 0.28f, 0.35f, 0.95f));
        hud.returnFromFailureButton = btnMenu.GetComponent<Button>();

        modalGO.SetActive(false);
        return modalGO;
    }

    private static GameObject BuildSuccessModal(Transform parent, TMP_FontAsset font, FlightHUDController hud)
    {
        GameObject modalGO = CreateUIObject("MissionSuccessModal", parent);
        StretchFull(modalGO.GetComponent<RectTransform>());

        Image backdrop = modalGO.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject card = CreateUIObject("Card", modalGO.transform);
        SetRect(card.GetComponent<RectTransform>(), new Vector2(0.30f, 0.28f), new Vector2(0.70f, 0.72f));
        Image cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.05f, 0.12f, 0.08f, 0.98f);
        card.AddComponent<Outline>().effectColor = new Color(0.30f, 1.0f, 0.55f, 0.95f);

        GameObject head = CreateUIObject("Header", card.transform);
        SetRect(head.GetComponent<RectTransform>(), new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f));
        TextMeshProUGUI hTMP = head.AddComponent<TextMeshProUGUI>();
        if (font != null) hTMP.font = font;
        hTMP.text = "[ MISSION SUCCESS // TRANSLUNAR INSERTION ACHIEVED ]";
        hTMP.fontSize = 17f;
        hTMP.fontStyle = FontStyles.Bold;
        hTMP.alignment = TextAlignmentOptions.Center;
        hTMP.color = new Color(0.35f, 1.0f, 0.55f);

        GameObject summary = CreateUIObject("Summary", card.transform);
        SetRect(summary.GetComponent<RectTransform>(), new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.78f));
        TextMeshProUGUI sTMP = summary.AddComponent<TextMeshProUGUI>();
        if (font != null) sTMP.font = font;
        sTMP.text = "Flight completed.";
        sTMP.fontSize = 15f;
        sTMP.alignment = TextAlignmentOptions.Center;
        sTMP.color = new Color(0.85f, 0.92f, 0.95f);
        hud.successSummaryText = sTMP;

        GameObject btnContinue = CreateButton("ContinueButton", "PROCEED TO LUNAR LANDING >", font, card.transform,
            new Vector2(0.20f, 0.08f), new Vector2(0.80f, 0.24f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.12f, 0.65f, 0.40f, 0.98f));
        hud.continueMissionButton = btnContinue.GetComponent<Button>();

        modalGO.SetActive(false);
        return modalGO;
    }

    // ===================================================================
    // SPACECRAFT & COCKPIT MESH
    // ===================================================================

    private static void BuildSpacecraftMesh(Transform parent)
    {
        string superHeavyPath = "Assets/Prefabs/Spacecraft/Nexus_SuperHeavyRocket.prefab";
        GameObject superHeavyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(superHeavyPath);

        if (superHeavyPrefab != null)
        {
            GameObject rocket = (GameObject)PrefabUtility.InstantiatePrefab(superHeavyPrefab, parent);
            rocket.name = "SuperHeavyRocket_Mesh";
            rocket.transform.localPosition = Vector3.zero;
            rocket.transform.localRotation = Quaternion.identity;
            return;
        }

        string superHeavyObjPath = "Assets/Art/Spacecraft/Nexus_SuperHeavyRocket.obj";
        GameObject superHeavyObj = AssetDatabase.LoadAssetAtPath<GameObject>(superHeavyObjPath);
        if (superHeavyObj != null)
        {
            GameObject rocket = (GameObject)Object.Instantiate(superHeavyObj, parent);
            rocket.name = "SuperHeavyRocket_Mesh";
            rocket.transform.localPosition = Vector3.zero;
            rocket.transform.localRotation = Quaternion.identity;
            return;
        }

        string shuttlePrefabPath = "Assets/Prefabs/Spacecraft/SpaceShuttle_Orbiter.prefab";
        GameObject shuttlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shuttlePrefabPath);

        if (shuttlePrefab != null)
        {
            GameObject shuttle = (GameObject)PrefabUtility.InstantiatePrefab(shuttlePrefab, parent);
            shuttle.name = "Shuttle_Mesh";
            shuttle.transform.localPosition = Vector3.zero;
            shuttle.transform.localRotation = Quaternion.identity;
            return;
        }

        // Clean primitive fallback
        Material hullWhite = GetOrCreateLitMaterial("Flight_Shuttle_White", new Color(0.92f, 0.92f, 0.94f), 0.1f, 0.4f, null);
        Material hullDark = GetOrCreateLitMaterial("Flight_Shuttle_Dark", new Color(0.12f, 0.13f, 0.15f), 0.2f, 0.3f, null);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Fuselage";
        body.transform.SetParent(parent, false);
        body.transform.localScale = new Vector3(2.2f, 3.8f, 2.2f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.GetComponent<Renderer>().sharedMaterial = hullWhite;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nose.name = "NoseCone";
        nose.transform.SetParent(parent, false);
        nose.transform.localPosition = new Vector3(0f, 0.3f, 3.6f);
        nose.transform.localScale = new Vector3(2.0f, 1.8f, 2.4f);
        nose.GetComponent<Renderer>().sharedMaterial = hullDark;
        Object.DestroyImmediate(nose.GetComponent<Collider>());

        GameObject wingL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wingL.name = "Wing_Port";
        wingL.transform.SetParent(parent, false);
        wingL.transform.localPosition = new Vector3(-2.8f, 0.1f, -0.6f);
        wingL.transform.localScale = new Vector3(4.2f, 0.18f, 4.5f);
        wingL.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
        wingL.GetComponent<Renderer>().sharedMaterial = hullWhite;
        Object.DestroyImmediate(wingL.GetComponent<Collider>());

        GameObject wingR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wingR.name = "Wing_Starboard";
        wingR.transform.SetParent(parent, false);
        wingR.transform.localPosition = new Vector3(2.8f, 0.1f, -0.6f);
        wingR.transform.localScale = new Vector3(4.2f, 0.18f, 4.5f);
        wingR.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
        wingR.GetComponent<Renderer>().sharedMaterial = hullWhite;
        Object.DestroyImmediate(wingR.GetComponent<Collider>());
    }

    private static void BuildEngineThrusters(Transform parent, List<Light> lights, List<GameObject> plumes)
    {
        Material nozzleMat = GetOrCreateLitMaterial("Flight_Nozzle_Mat", new Color(0.22f, 0.22f, 0.25f), 0.85f, 0.65f, null);
        Material plumeMat = GetOrCreateUnlitMaterial("Flight_Plume_Mat", new Color(0.25f, 0.75f, 1.0f, 0.85f));

        Vector3[] nozzlePositions = new Vector3[]
        {
            new Vector3(-0.65f, 0.45f, -4.2f),
            new Vector3(0.65f, 0.45f, -4.2f),
            new Vector3(0.0f, 1.35f, -4.2f)
        };

        for (int i = 0; i < nozzlePositions.Length; i++)
        {
            GameObject thrusterGO = new GameObject($"Thruster_{i + 1}");
            thrusterGO.transform.SetParent(parent, false);
            thrusterGO.transform.localPosition = nozzlePositions[i];

            GameObject bell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bell.name = "Bell";
            bell.transform.SetParent(thrusterGO.transform, false);
            bell.transform.localScale = new Vector3(0.65f, 0.45f, 0.65f);
            bell.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            bell.GetComponent<Renderer>().sharedMaterial = nozzleMat;
            Object.DestroyImmediate(bell.GetComponent<Collider>());

            GameObject lightGO = new GameObject("ThrusterLight");
            lightGO.transform.SetParent(thrusterGO.transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            Light l = lightGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.35f, 0.75f, 1.0f);
            l.range = 8f;
            l.intensity = 0f;
            l.enabled = false;
            lights.Add(l);

            GameObject plume = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plume.name = "Plume";
            plume.transform.SetParent(thrusterGO.transform, false);
            plume.transform.localPosition = new Vector3(0f, 0f, -1.8f);
            plume.transform.localScale = new Vector3(0.55f, 1.4f, 0.55f);
            plume.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            plume.GetComponent<Renderer>().sharedMaterial = plumeMat;
            Object.DestroyImmediate(plume.GetComponent<Collider>());
            plume.SetActive(false);
            plumes.Add(plume);
        }
    }

    private static GameObject BuildCockpitInteriorFrame(Transform parent)
    {
        GameObject frameRoot = new GameObject("Cockpit_Interior_Frame");
        frameRoot.transform.SetParent(parent, false);
        frameRoot.transform.localPosition = new Vector3(0f, 0.85f, 1.85f);

        Material frameMat = GetOrCreateLitMaterial("Flight_Cockpit_Frame_Mat", new Color(0.12f, 0.14f, 0.18f), 0.4f, 0.5f, null);

        // Dashboard Console
        GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dash.name = "Dashboard_Console";
        dash.transform.SetParent(frameRoot.transform, false);
        dash.transform.localPosition = new Vector3(0f, -0.45f, 0.95f);
        dash.transform.localScale = new Vector3(1.6f, 0.35f, 0.85f);
        dash.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
        dash.GetComponent<Renderer>().sharedMaterial = frameMat;
        Object.DestroyImmediate(dash.GetComponent<Collider>());

        // Pillars
        GameObject pillarL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillarL.name = "Pillar_Port";
        pillarL.transform.SetParent(frameRoot.transform, false);
        pillarL.transform.localPosition = new Vector3(-0.75f, 0.1f, 0.7f);
        pillarL.transform.localScale = new Vector3(0.12f, 0.9f, 0.12f);
        pillarL.transform.localRotation = Quaternion.Euler(0f, 25f, -15f);
        pillarL.GetComponent<Renderer>().sharedMaterial = frameMat;
        Object.DestroyImmediate(pillarL.GetComponent<Collider>());

        GameObject pillarR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillarR.name = "Pillar_Starboard";
        pillarR.transform.SetParent(frameRoot.transform, false);
        pillarR.transform.localPosition = new Vector3(0.75f, 0.1f, 0.7f);
        pillarR.transform.localScale = new Vector3(0.12f, 0.9f, 0.12f);
        pillarR.transform.localRotation = Quaternion.Euler(0f, -25f, 15f);
        pillarR.GetComponent<Renderer>().sharedMaterial = frameMat;
        Object.DestroyImmediate(pillarR.GetComponent<Collider>());

        // Soft interior light
        GameObject cockLightGO = new GameObject("Cockpit_Light");
        cockLightGO.transform.SetParent(frameRoot.transform, false);
        cockLightGO.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        Light cl = cockLightGO.AddComponent<Light>();
        cl.type = LightType.Point;
        cl.color = new Color(0.45f, 0.75f, 1.0f);
        cl.range = 3.5f;
        cl.intensity = 0.7f;

        return frameRoot;
    }

    // ===================================================================
    // CELESTIAL & STARFIELD
    // ===================================================================

    private static GameObject BuildDistantEarth(Vector3 position, float scale)
    {
        string earthPrefabPath = "Assets/Prefabs/Planets/Earth_NASA_Realistic.prefab";
        GameObject earthPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(earthPrefabPath);

        GameObject earthGO;
        if (earthPrefab != null)
        {
            earthGO = (GameObject)PrefabUtility.InstantiatePrefab(earthPrefab);
            earthGO.name = "Earth_Distant";
        }
        else
        {
            earthGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earthGO.name = "Earth_Distant";
            Material earthMat = GetOrCreateLitMaterial("Earth_Distant_Mat", new Color(0.18f, 0.45f, 0.85f), 0.1f, 0.4f, null);
            earthGO.GetComponent<Renderer>().sharedMaterial = earthMat;
            Object.DestroyImmediate(earthGO.GetComponent<Collider>());
        }

        earthGO.transform.position = position;
        earthGO.transform.localScale = Vector3.one * scale;
        earthGO.transform.rotation = Quaternion.Euler(20f, 80f, 0f);
        return earthGO;
    }

    private static GameObject BuildDistantMoon(Vector3 position, float scale)
    {
        GameObject moonGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moonGO.name = "Moon_Target";
        Object.DestroyImmediate(moonGO.GetComponent<Collider>());

        Material moonMat = GetOrCreateLitMaterial("Moon_Target_Mat", new Color(0.80f, 0.82f, 0.85f), 0.05f, 0.25f, null);
        moonGO.GetComponent<Renderer>().sharedMaterial = moonMat;

        moonGO.transform.position = position;
        moonGO.transform.localScale = Vector3.one * scale;
        moonGO.transform.rotation = Quaternion.Euler(15f, 45f, 0f);
        return moonGO;
    }

    private static GameObject BuildStarfield(string name, int count, float radius)
    {
        GameObject starsGO = new GameObject(name);
        ParticleSystem ps = starsGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psRenderer = starsGO.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startLifetime = 1000f;
        main.startSpeed = 0f;
        main.startSize = 0.8f;
        main.maxParticles = count;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        Material starMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
        starMat.color = Color.white;
        psRenderer.sharedMaterial = starMat;

        ps.Emit(count);
        return starsGO;
    }

    // ===================================================================
    // UI HELPERS
    // ===================================================================

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetRect(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void StretchFull(RectTransform rt)
    {
        SetRect(rt, Vector2.zero, Vector2.one);
    }

    private static GameObject CreateButton(string name, string label, TMP_FontAsset font,
        Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor)
    {
        GameObject btnGO = CreateUIObject(name, parent);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Image img = btnGO.AddComponent<Image>();
        img.color = bgColor;

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = bgColor * 1.25f;
        cb.pressedColor = bgColor * 0.8f;
        cb.disabledColor = new Color(0.2f, 0.2f, 0.25f, 0.5f);
        btn.colors = cb;

        btnGO.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.25f);

        GameObject textGO = CreateUIObject("Text", btnGO.transform);
        StretchFull(textGO.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 14f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnGO;
    }

    private static Material GetOrCreateLitMaterial(string matName, Color color, float metallic, float smoothness, string texName)
    {
        string path = MaterialsPath + "/" + matName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(litShader);
            mat.name = matName;
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateUnlitMaterial(string matName, Color color)
    {
        string path = MaterialsPath + "/" + matName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            mat = new Material(unlitShader);
            mat.name = matName;
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", color);
        mat.color = color;

        mat.SetFloat("_Surface", 1f); // Transparent
        mat.SetFloat("_Blend", 0f);   // Alpha blend
        mat.renderQueue = 3000;

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }
    }

    private static TMP_FontAsset FindTMPFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
        }
        return font;
    }

    private static void UpdateBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

        string[] desiredScenes = new string[]
        {
            ScenesPath + "/OpeningStory_VisualTest.unity",
            ScenesPath + "/MissionSelection.unity",
            ScenesPath + "/MissionPreparation.unity",
            ScenesPath + "/LaunchSequence.unity",
            ScenesPath + "/SpacecraftFlight.unity",
            ScenesPath + "/LunarLanding.unity",
            ScenesPath + "/LunarExploration.unity",
            ScenesPath + "/OpeningStory.unity",
            ScenesPath + "/VisualTest.unity"
        };

        foreach (string scPath in desiredScenes)
        {
            if (File.Exists(scPath))
            {
                scenes.Add(new EditorBuildSettingsScene(scPath, true));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[ASTROBASE] Updated EditorBuildSettings with {scenes.Count} active scenes.");
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}

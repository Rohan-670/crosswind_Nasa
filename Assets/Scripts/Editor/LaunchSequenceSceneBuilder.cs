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
/// Automation tool to build the LaunchSequence and SpacecraftFlight scenes,
/// setup 3D spacecraft, engine thrusters, procedural audio, cockpit HUD,
/// and update Build Settings.
/// </summary>
public class LaunchSequenceSceneBuilder
{
    private const string ScenesPath = "Assets/Scenes";
    private const string MaterialsPath = "Assets/Materials";
    private const string PrefabsPath = "Assets/Prefabs/Spacecraft";

    [MenuItem("ASTROBASE/Missions/Build Launch and Flight Scenes")]
    public static void BuildScenesInteractive()
    {
        ExecuteBuild(false);
    }

    public static void BuildScenesBatch()
    {
        ExecuteBuild(true);
    }

    [InitializeOnLoadMethod]
    private static void AutoBuildOnFirstLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(ScenesPath + "/LaunchSequence.unity"))
            {
                Debug.Log("[ASTROBASE] Initializing LaunchSequence and Flight scenes...");
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

            // 1. Build LaunchSequence.unity
            BuildLaunchSequenceScene(font);

            // 2. Build SpacecraftFlight.unity gameplay scene
            SpacecraftFlightSceneBuilder.BuildSceneBatch();

            // 3. Update Build Settings
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: Launch & Flight Scenes Built!",
                    "Scenes successfully built:\n" +
                    "  ✓ Assets/Scenes/LaunchSequence.unity\n" +
                    "    - Orbital Staging & Airlock Boarding interaction\n" +
                    "    - Cockpit camera glide & Flight HUD telemetry\n" +
                    "    - T-10 Countdown with procedural audio beeps\n" +
                    "    - Engine ignition, cockpit rumble & launch effects\n" +
                    "    - External tracking departure from Earth\n\n" +
                    "  ✓ Assets/Scenes/SpacecraftFlight.unity\n" +
                    "    - Translunar cruise flight between Earth & Moon\n" +
                    "    - Flight telemetry HUD & roll maneuvering controls\n\n" +
                    "Press Play ▶ in LaunchSequence to experience liftoff!",
                    "OK");
            }
            Debug.Log("[ASTROBASE] Launch and Flight scenes built successfully.");
        }
        catch (System.Exception ex)
        {
            if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", ex.Message, "OK");
            Debug.LogError("[ASTROBASE] Launch sequence build failed: " + ex);
        }
    }

    // ===================================================================
    // 1. BUILD LAUNCH SEQUENCE SCENE
    // ===================================================================

    private static void BuildLaunchSequenceScene(TMP_FontAsset font)
    {
        string scenePath = ScenesPath + "/LaunchSequence.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Lighting & Sky for Space Launch Complex on Earth
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.60f, 0.68f, 0.78f);

        // Sunlight (Florida daytime launch)
        GameObject sunGO = new GameObject("Sunlight");
        Light sunLight = sunGO.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1.0f, 0.98f, 0.90f);
        sunLight.intensity = 1.40f;
        sunLight.shadows = LightShadows.Soft;
        sunGO.transform.rotation = Quaternion.Euler(42f, -50f, 0f);

        // Space Center Realistic Launch Facility (Pad, Umbilical Tower, Hangars, Flame Trench)
        GameObject facilityGO = BuildSpaceCenterFacility();

        // Deep Starfield Background (becomes visible as rocket ascends into space)
        GameObject starsGO = BuildStarfield("DeepSpaceStarfield", 3000, 450f);

        // Earth in Background (for high altitude / translunar cruise view)
        GameObject earthGO = BuildEarthInOrbit();
        earthGO.transform.position = new Vector3(0f, -400f, 0f);

        // Moon in Sky (destination in view)
        GameObject moonGO = BuildDistantMoon(new Vector3(0f, 180f, 750f), 55f);

        // Spacecraft Root - Standing 100% vertically upright on the launch pad
        GameObject spacecraftRoot = new GameObject("Spacecraft_Orbiter");
        spacecraftRoot.SetActive(true);
        spacecraftRoot.transform.position = new Vector3(0f, 15.1f, 0f);
        spacecraftRoot.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        // Model
        BuildSpacecraftModel(spacecraftRoot.transform);

        // Engine Thruster Effects & Audio
        List<Light> thrusterLights = new List<Light>();
        List<GameObject> plumeObjects = new List<GameObject>();
        List<ParticleSystem> exhaustParticles = new List<ParticleSystem>();

        BuildEngineThrusters(spacecraftRoot.transform, thrusterLights, plumeObjects, exhaustParticles);

        ProceduralEngineAudio engineAudio = spacecraftRoot.AddComponent<ProceduralEngineAudio>();
        engineAudio.masterVolume = 0.80f;

        // Camera Anchors (Aligned for vertical rocket standing on the pad)
        GameObject padAnchor = new GameObject("Anchor_PadView");
        padAnchor.transform.position = new Vector3(-45f, 14f, -48f);
        padAnchor.transform.LookAt(new Vector3(0f, 22f, 0f));

        GameObject cockpitAnchor = new GameObject("Anchor_CockpitView");
        cockpitAnchor.transform.SetParent(spacecraftRoot.transform, false);
        cockpitAnchor.transform.localPosition = new Vector3(0f, 1.2f, 6.5f);
        cockpitAnchor.transform.localRotation = Quaternion.identity;

        GameObject externalAnchor = new GameObject("Anchor_ExternalTracking");
        externalAnchor.transform.position = new Vector3(-65f, 16f, -65f);
        externalAnchor.transform.LookAt(new Vector3(0f, 28f, 0f));

        // Cockpit Interior Frame
        GameObject cockpitFrame = BuildCockpitInteriorFrame(spacecraftRoot.transform);

        // Main Camera
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.48f, 0.68f, 0.88f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 3500f;
        camGO.AddComponent<AudioListener>();

        // Initially place camera at pad view
        camGO.transform.position = padAnchor.transform.position;
        camGO.transform.rotation = padAnchor.transform.rotation;

        // UI Canvas & EventSystem
        EnsureEventSystem();
        GameObject canvasGO = BuildLaunchCanvas(font);

        // Wire LaunchSequenceController
        LaunchSequenceController controller = spacecraftRoot.AddComponent<LaunchSequenceController>();
        controller.spacecraftRoot = spacecraftRoot.transform;
        controller.launchFacilityRoot = facilityGO != null ? facilityGO.transform : null;
        controller.earthTransform = earthGO.transform;
        controller.moonTransform = moonGO.transform;
        controller.padViewAnchor = padAnchor.transform;
        controller.airlockHatchAnchor = padAnchor.transform;
        controller.cockpitViewAnchor = cockpitAnchor.transform;
        controller.externalTrackingAnchor = externalAnchor.transform;
        controller.mainCamera = cam;
        controller.engineAudio = engineAudio;
        controller.engineThrusterLights = thrusterLights.ToArray();
        controller.engineExhaustPlumes = plumeObjects.ToArray();
        controller.engineExhaustParticles = exhaustParticles.ToArray();
        controller.cockpitInteriorFrame = cockpitFrame;

        // Wire UI elements
        controller.cockpitHUD = canvasGO.GetComponentInChildren<CockpitHUDController>(true);
        controller.stagingUIPanel = canvasGO.transform.Find("StagingUIPanel").gameObject;
        controller.boardSpacecraftButton = canvasGO.transform.Find("StagingUIPanel/CenterCard/BoardButton").GetComponent<Button>();
        controller.stagingPromptText = canvasGO.transform.Find("StagingUIPanel/CenterCard/PromptText").GetComponent<TextMeshProUGUI>();
        controller.skipSequenceButton = canvasGO.transform.Find("SkipButton").GetComponent<Button>();
        controller.fadeOverlay = canvasGO.transform.Find("FadeOverlay").GetComponent<Image>();
        controller.transitionCalloutText = canvasGO.transform.Find("FadeOverlay/CalloutText").GetComponent<TextMeshProUGUI>();

        Transform flightBarT = canvasGO.transform.Find("FlightControlsPanel");
        if (flightBarT != null)
        {
            controller.flightControlsBar = flightBarT.gameObject;
            controller.flightControlsText = flightBarT.Find("ControlsText").GetComponent<TextMeshProUGUI>();
            controller.fastForwardButton = flightBarT.Find("EngageCruiseButton").GetComponent<Button>();
        }

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    // ===================================================================
    // 2. BUILD SPACECRAFT FLIGHT SCENE (TRANSLUNAR CRUISE)
    // ===================================================================

    private static void BuildSpacecraftFlightScene(TMP_FontAsset font)
    {
        string scenePath = ScenesPath + "/SpacecraftFlight.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.03f, 0.04f, 0.07f);

        GameObject sunGO = new GameObject("Sunlight");
        Light sunLight = sunGO.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1.0f, 0.97f, 0.90f);
        sunLight.intensity = 1.3f;
        sunGO.transform.rotation = Quaternion.Euler(30f, -40f, 0f);

        // Starfield
        BuildStarfield("DeepSpaceStarfield", 3000, 450f);

        // Receding Earth (Behind)
        GameObject earthGO = BuildDistantEarth(new Vector3(0f, -30f, -480f), 65f);

        // Glowing Moon (Ahead)
        GameObject moonGO = BuildDistantMoon(new Vector3(0f, 15f, 420f), 38f);

        // Cruising Spacecraft
        GameObject spacecraftRoot = new GameObject("Spacecraft_Orbiter");
        spacecraftRoot.transform.position = Vector3.zero;
        spacecraftRoot.transform.rotation = Quaternion.identity;
        BuildSpacecraftModel(spacecraftRoot.transform);

        // Subtle cruising thruster glow
        List<Light> cruiseLights = new List<Light>();
        List<GameObject> cruisePlumes = new List<GameObject>();
        List<ParticleSystem> cruiseParticles = new List<ParticleSystem>();
        BuildEngineThrusters(spacecraftRoot.transform, cruiseLights, cruisePlumes, cruiseParticles);
        foreach (Light l in cruiseLights) { l.enabled = true; l.intensity = 1.2f; }
        foreach (GameObject p in cruisePlumes) { p.SetActive(true); p.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f); }

        // Camera behind spacecraft looking forward to the Moon
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 2000f;
        camGO.AddComponent<AudioListener>();
        camGO.transform.position = new Vector3(0f, 2.8f, -9.5f);
        camGO.transform.LookAt(spacecraftRoot.transform.position + Vector3.forward * 4f);

        // UI Canvas
        EnsureEventSystem();
        GameObject canvasGO = BuildFlightCanvas(font);

        // Wire flight controller
        SpacecraftFlightStarter flightController = spacecraftRoot.AddComponent<SpacecraftFlightStarter>();
        flightController.spacecraftTransform = spacecraftRoot.transform;
        flightController.earthTransform = earthGO.transform;
        flightController.moonTransform = moonGO.transform;
        flightController.missionTitleText = canvasGO.transform.Find("HUDPanel/TitleText").GetComponent<TextMeshProUGUI>();
        flightController.flightPhaseText = canvasGO.transform.Find("HUDPanel/PhaseText").GetComponent<TextMeshProUGUI>();
        flightController.speedText = canvasGO.transform.Find("HUDPanel/SpeedText").GetComponent<TextMeshProUGUI>();
        flightController.distanceToMoonText = canvasGO.transform.Find("HUDPanel/DistanceText").GetComponent<TextMeshProUGUI>();
        flightController.systemsStatusText = canvasGO.transform.Find("HUDPanel/SystemsText").GetComponent<TextMeshProUGUI>();
        flightController.returnToMissionsButton = canvasGO.transform.Find("ReturnButton").GetComponent<Button>();

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    // ===================================================================
    // 3. EARTH & CELESTIAL BUILDERS
    // ===================================================================

    private static GameObject BuildEarthInOrbit()
    {
        string earthPrefabPath = "Assets/Prefabs/Planets/Earth_NASA_Realistic.prefab";
        GameObject earthPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(earthPrefabPath);

        GameObject earthGO;
        if (earthPrefab != null)
        {
            earthGO = (GameObject)PrefabUtility.InstantiatePrefab(earthPrefab);
            earthGO.name = "Earth_NASA_Realistic";
        }
        else
        {
            earthGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earthGO.name = "Earth_Sphere";
            Material earthMat = GetOrCreateLitMaterial("Earth_Orbital_Mat", new Color(0.18f, 0.45f, 0.85f), 0.1f, 0.4f, null);
            earthGO.GetComponent<Renderer>().sharedMaterial = earthMat;
            Object.DestroyImmediate(earthGO.GetComponent<Collider>());
        }

        // Place Earth in lower orbital view
        earthGO.transform.position = new Vector3(0f, -85f, 240f);
        earthGO.transform.rotation = Quaternion.Euler(18f, 135f, 0f);
        earthGO.transform.localScale = new Vector3(95f, 95f, 95f);

        return earthGO;
    }

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
            Material earthMat = GetOrCreateLitMaterial("Earth_Distant_Mat", new Color(0.2f, 0.48f, 0.88f), 0.1f, 0.4f, null);
            earthGO.GetComponent<Renderer>().sharedMaterial = earthMat;
            Object.DestroyImmediate(earthGO.GetComponent<Collider>());
        }

        earthGO.transform.position = position;
        earthGO.transform.localScale = Vector3.one * scale;
        earthGO.transform.rotation = Quaternion.Euler(20f, 60f, 0f);
        return earthGO;
    }

    private static GameObject BuildDistantMoon(Vector3 position, float scale)
    {
        GameObject moonGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moonGO.name = "Moon_Target";
        Object.DestroyImmediate(moonGO.GetComponent<Collider>());

        Texture2D lunarTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/NASA_Textures/Mission_Preview_Lunar.png");
        Material moonMat = GetOrCreateLitMaterial("Moon_Surface_Mat", new Color(0.92f, 0.93f, 0.95f), 0.05f, 0.25f, lunarTex);
        moonGO.GetComponent<Renderer>().sharedMaterial = moonMat;

        moonGO.transform.position = position;
        moonGO.transform.localScale = Vector3.one * scale;
        moonGO.transform.rotation = Quaternion.Euler(12f, 40f, 0f);
        return moonGO;
    }

    private static GameObject BuildStarfield(string name, int count, float radius)
    {
        GameObject starsGO = new GameObject(name);
        ParticleSystem ps = starsGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psRenderer = starsGO.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.startLifetime = 9999f;
        main.startSpeed = 0f;
        main.startSize = 1.2f;
        main.maxParticles = count;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

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
    // 3B. SPACE CENTER FACILITY BUILDER
    // ===================================================================

    private static GameObject BuildSpaceCenterFacility()
    {
        string prefabPath = "Assets/Prefabs/Environment/SpaceCenter_LaunchFacility.prefab";
        GameObject facilityPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (facilityPrefab != null)
        {
            GameObject facility = (GameObject)PrefabUtility.InstantiatePrefab(facilityPrefab);
            facility.name = "SpaceCenter_RealisticLaunchFacility";
            facility.transform.position = Vector3.zero;
            facility.transform.rotation = Quaternion.identity;
            return facility;
        }

        string objPath = "Assets/Art/Environment/SpaceCenter_Facility.obj";
        GameObject facilityObj = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
        if (facilityObj != null)
        {
            GameObject facility = (GameObject)Object.Instantiate(facilityObj);
            facility.name = "SpaceCenter_RealisticLaunchFacility";
            facility.transform.position = Vector3.zero;
            facility.transform.rotation = Quaternion.identity;
            return facility;
        }

        return null;
    }

    // ===================================================================
    // 4. SPACECRAFT MODEL BUILDER
    // ===================================================================

    private static void BuildSpacecraftModel(Transform parent)
    {
        // 1. Check for realistic Space Center Launch Vehicle
        string rocketPrefabPath = "Assets/Prefabs/Spacecraft/SpaceCenter_LaunchVehicle.prefab";
        GameObject rocketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rocketPrefabPath);
        if (rocketPrefab != null)
        {
            GameObject rocket = (GameObject)PrefabUtility.InstantiatePrefab(rocketPrefab, parent);
            rocket.name = "SpaceCenter_LaunchVehicle";
            rocket.transform.localPosition = new Vector3(0f, 0f, -15.09f);
            rocket.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rocket.transform.localScale = Vector3.one;
            return;
        }

        string rocketObjPath = "Assets/Art/Environment/SpaceCenter_Rocket.obj";
        GameObject rocketObj = AssetDatabase.LoadAssetAtPath<GameObject>(rocketObjPath);
        if (rocketObj != null)
        {
            GameObject rocket = (GameObject)Object.Instantiate(rocketObj, parent);
            rocket.name = "SpaceCenter_LaunchVehicle";
            rocket.transform.localPosition = new Vector3(0f, 0f, -15.09f);
            rocket.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rocket.transform.localScale = Vector3.one;
            return;
        }

        // 2. Fallback to Super Heavy or Shuttle
        string superHeavyPrefabPath = "Assets/Prefabs/Spacecraft/Nexus_SuperHeavyRocket.prefab";
        GameObject superHeavyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(superHeavyPrefabPath);

        if (superHeavyPrefab != null)
        {
            GameObject rocket = (GameObject)PrefabUtility.InstantiatePrefab(superHeavyPrefab, parent);
            rocket.name = "SuperHeavyRocket_Mesh";
            rocket.transform.localPosition = Vector3.zero;
            rocket.transform.localRotation = Quaternion.identity;
            rocket.transform.localScale = Vector3.one;
            return;
        }

        // Direct OBJ loading fallback
        string superHeavyObjPath = "Assets/Art/Spacecraft/Nexus_SuperHeavyRocket.obj";
        GameObject superHeavyObj = AssetDatabase.LoadAssetAtPath<GameObject>(superHeavyObjPath);
        if (superHeavyObj != null)
        {
            GameObject rocket = (GameObject)Object.Instantiate(superHeavyObj, parent);
            rocket.name = "SuperHeavyRocket_Mesh";
            rocket.transform.localPosition = Vector3.zero;
            rocket.transform.localRotation = Quaternion.identity;
            rocket.transform.localScale = Vector3.one;
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
            shuttle.transform.localScale = Vector3.one;
            return;
        }

        // Clean primitive fallback
        Material hullWhite = GetOrCreateLitMaterial("Fallback_Shuttle_White", new Color(0.92f, 0.92f, 0.94f), 0.1f, 0.4f, null);
        Material hullBlack = GetOrCreateLitMaterial("Fallback_Shuttle_Black", new Color(0.12f, 0.13f, 0.15f), 0.2f, 0.3f, null);

        // Fuselage
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Fuselage";
        body.transform.SetParent(parent, false);
        body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        body.transform.localScale = new Vector3(2.2f, 3.8f, 2.2f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.GetComponent<Renderer>().sharedMaterial = hullWhite;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // Cockpit nose
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nose.name = "NoseCone";
        nose.transform.SetParent(parent, false);
        nose.transform.localPosition = new Vector3(0f, 0.4f, 3.6f);
        nose.transform.localScale = new Vector3(2.0f, 1.8f, 2.4f);
        nose.GetComponent<Renderer>().sharedMaterial = hullBlack;
        Object.DestroyImmediate(nose.GetComponent<Collider>());

        // Delta Wings
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

        // Vertical Stabilizer
        GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fin.name = "Vertical_Tail";
        fin.transform.SetParent(parent, false);
        fin.transform.localPosition = new Vector3(0f, 2.0f, -2.8f);
        fin.transform.localScale = new Vector3(0.24f, 2.5f, 2.4f);
        fin.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
        fin.GetComponent<Renderer>().sharedMaterial = hullWhite;
        Object.DestroyImmediate(fin.GetComponent<Collider>());
    }

    private static void BuildEngineThrusters(Transform parent, List<Light> lights, List<GameObject> plumes, List<ParticleSystem> particles)
    {
        Material nozzleMat = GetOrCreateLitMaterial("Engine_Nozzle_Mat", new Color(0.22f, 0.22f, 0.25f), 0.85f, 0.65f, null);
        Material plumeMat = GetOrCreateUnlitMaterial("Engine_Plume_Mat", new Color(1.0f, 0.65f, 0.25f, 0.90f));

        Vector3[] nozzlePositions = new Vector3[]
        {
            new Vector3(0.0f, 0.0f, -8.1f),   // Main Center Core
            new Vector3(2.4f, 0.0f, -8.1f),   // East Booster
            new Vector3(-2.4f, 0.0f, -8.1f),  // West Booster
            new Vector3(0.0f, 2.4f, -8.1f),   // North Booster
            new Vector3(0.0f, -2.4f, -8.1f),  // South Booster
            new Vector3(1.7f, 1.7f, -8.1f),
            new Vector3(-1.7f, 1.7f, -8.1f),
            new Vector3(1.7f, -1.7f, -8.1f),
            new Vector3(-1.7f, -1.7f, -8.1f)
        };

        for (int i = 0; i < nozzlePositions.Length; i++)
        {
            GameObject thrusterGO = new GameObject($"Thruster_{i + 1}");
            thrusterGO.transform.SetParent(parent, false);
            thrusterGO.transform.localPosition = nozzlePositions[i];

            // Bell mesh
            GameObject bell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bell.name = "NozzleBell";
            bell.transform.SetParent(thrusterGO.transform, false);
            bell.transform.localScale = new Vector3(0.65f, 0.45f, 0.65f);
            bell.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            bell.GetComponent<Renderer>().sharedMaterial = nozzleMat;
            Object.DestroyImmediate(bell.GetComponent<Collider>());

            // Thruster point light
            GameObject lightGO = new GameObject("ExhaustLight");
            lightGO.transform.SetParent(thrusterGO.transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            Light l = lightGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.65f, 0.20f);
            l.range = 25f;
            l.intensity = 0f;
            l.enabled = false;
            lights.Add(l);

            // Exhaust plume cone pointing out the back of the engine
            GameObject plume = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plume.name = "ExhaustPlume";
            plume.transform.SetParent(thrusterGO.transform, false);
            plume.transform.localPosition = new Vector3(0f, 0f, -2.5f);
            plume.transform.localScale = new Vector3(0.70f, 2.0f, 0.70f);
            plume.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            plume.GetComponent<Renderer>().sharedMaterial = plumeMat;
            Object.DestroyImmediate(plume.GetComponent<Collider>());
            plume.SetActive(false);
            plumes.Add(plume);

            // Exhaust particle trail firing backwards (-Z in local rocket space)
            GameObject particleGO = new GameObject("ExhaustParticles");
            particleGO.transform.SetParent(thrusterGO.transform, false);
            particleGO.transform.localPosition = new Vector3(0f, 0f, -1.0f);
            particleGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // shoot backwards out the nozzles
            ParticleSystem ps = particleGO.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = particleGO.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 16f;
            main.startSize = 0.45f;
            main.startColor = new Color(0.4f, 0.8f, 1f, 0.8f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 40f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.2f;

            psr.sharedMaterial = plumeMat;
            particles.Add(ps);
        }
    }

    private static GameObject BuildCockpitInteriorFrame(Transform parent)
    {
        GameObject frameRoot = new GameObject("Cockpit_Interior_Frame");
        frameRoot.transform.SetParent(parent, false);
        frameRoot.transform.localPosition = new Vector3(0f, 2.2f, 6.5f);

        Material frameMat = GetOrCreateLitMaterial("Cockpit_Frame_Mat", new Color(0.12f, 0.14f, 0.18f), 0.4f, 0.5f, null);
        Material glassMat = GetOrCreateUnlitMaterial("Cockpit_Glass_Mat", new Color(0.15f, 0.35f, 0.5f, 0.12f));

        // Front Dashboard Console
        GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dash.name = "Dashboard_Console";
        dash.transform.SetParent(frameRoot.transform, false);
        dash.transform.localPosition = new Vector3(0f, -0.45f, 0.95f);
        dash.transform.localScale = new Vector3(1.6f, 0.35f, 0.85f);
        dash.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
        dash.GetComponent<Renderer>().sharedMaterial = frameMat;
        Object.DestroyImmediate(dash.GetComponent<Collider>());

        // Left/Right Window Pillars
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

        // Center Strut
        GameObject centerStrut = GameObject.CreatePrimitive(PrimitiveType.Cube);
        centerStrut.name = "Center_Strut";
        centerStrut.transform.SetParent(frameRoot.transform, false);
        centerStrut.transform.localPosition = new Vector3(0f, 0.15f, 0.85f);
        centerStrut.transform.localScale = new Vector3(0.08f, 0.85f, 0.08f);
        centerStrut.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
        centerStrut.GetComponent<Renderer>().sharedMaterial = frameMat;
        Object.DestroyImmediate(centerStrut.GetComponent<Collider>());

        // Soft cockpit ambient light
        GameObject cockLightGO = new GameObject("Cockpit_Light");
        cockLightGO.transform.SetParent(frameRoot.transform, false);
        cockLightGO.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        Light cl = cockLightGO.AddComponent<Light>();
        cl.type = LightType.Point;
        cl.color = new Color(0.45f, 0.75f, 1.0f);
        cl.range = 3.5f;
        cl.intensity = 0.65f;

        frameRoot.SetActive(false);
        return frameRoot;
    }

    // ===================================================================
    // 5. UI CANVAS BUILDERS
    // ===================================================================

    private static GameObject BuildLaunchCanvas(TMP_FontAsset font)
    {
        GameObject canvasGO = new GameObject("LaunchUI_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // 1. Staging UI Panel (Airlock Ingress)
        GameObject stagingPanel = CreateUIObject("StagingUIPanel", canvasGO.transform);
        StretchFull(stagingPanel.GetComponent<RectTransform>());

        // Top Telemetry Header
        GameObject topHeader = CreateUIObject("TopHeader", stagingPanel.transform);
        SetRect(topHeader.GetComponent<RectTransform>(), new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f));
        TextMeshProUGUI topTMP = topHeader.AddComponent<TextMeshProUGUI>();
        if (font != null) topTMP.font = font;
        topTMP.text = "MISSION // LUNAR EXPEDITION: ARTEMIS BEYOND   |   KENNEDY SPACE CENTER LC-39";
        topTMP.fontSize = 18f;
        topTMP.fontStyle = FontStyles.Bold;
        topTMP.color = new Color(0.35f, 0.85f, 1.0f);

        // Center Card (Compact & sleek, placed at bottom center to leave rocket view clear)
        GameObject centerCard = CreateUIObject("CenterCard", stagingPanel.transform);
        SetRect(centerCard.GetComponent<RectTransform>(), new Vector2(0.34f, 0.05f), new Vector2(0.66f, 0.17f));
        Image cardBg = centerCard.AddComponent<Image>();
        cardBg.color = new Color(0.06f, 0.09f, 0.14f, 0.92f);
        centerCard.AddComponent<Outline>().effectColor = new Color(0.25f, 0.80f, 1.0f, 0.8f);

        // Prompt text (Clean single-line header, unnecessary comments removed)
        GameObject promptGO = CreateUIObject("PromptText", centerCard.transform);
        SetRect(promptGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.92f));
        TextMeshProUGUI promptTMP = promptGO.AddComponent<TextMeshProUGUI>();
        if (font != null) promptTMP.font = font;
        promptTMP.text = "LAUNCH FACILITY // PAD 39A PRE-LAUNCH STAGING";
        promptTMP.fontSize = 15f;
        promptTMP.fontStyle = FontStyles.Bold;
        promptTMP.alignment = TextAlignmentOptions.Center;
        promptTMP.color = new Color(0.35f, 0.88f, 1.0f);

        // Board Spacecraft Button (Compact button with clear label)
        CreateButton("BoardButton", "BOARD & LAUNCH [E]", font, centerCard.transform,
            new Vector2(0.20f, 0.10f), new Vector2(0.80f, 0.46f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.12f, 0.60f, 0.94f, 0.98f));

        // 2. Cockpit HUD
        GameObject cockpitHUDGO = CreateUIObject("CockpitHUD", canvasGO.transform);
        StretchFull(cockpitHUDGO.GetComponent<RectTransform>());
        CockpitHUDController hud = cockpitHUDGO.AddComponent<CockpitHUDController>();

        // Top Mission Callout
        GameObject mTitle = CreateUIObject("MissionTitleText", cockpitHUDGO.transform);
        SetRect(mTitle.GetComponent<RectTransform>(), new Vector2(0.04f, 0.90f), new Vector2(0.60f, 0.96f));
        TextMeshProUGUI mTitleTMP = mTitle.AddComponent<TextMeshProUGUI>();
        if (font != null) mTitleTMP.font = font;
        mTitleTMP.fontSize = 20f;
        mTitleTMP.fontStyle = FontStyles.Bold;
        mTitleTMP.color = new Color(0.35f, 0.90f, 1.0f);
        hud.missionTitleText = mTitleTMP;

        GameObject destGO = CreateUIObject("DestinationText", cockpitHUDGO.transform);
        SetRect(destGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.85f), new Vector2(0.60f, 0.90f));
        TextMeshProUGUI destTMP = destGO.AddComponent<TextMeshProUGUI>();
        if (font != null) destTMP.font = font;
        destTMP.fontSize = 15f;
        destTMP.color = new Color(0.75f, 0.88f, 0.98f);
        hud.destinationText = destTMP;

        GameObject vehGO = CreateUIObject("VehicleIdText", cockpitHUDGO.transform);
        SetRect(vehGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.80f), new Vector2(0.60f, 0.85f));
        TextMeshProUGUI vehTMP = vehGO.AddComponent<TextMeshProUGUI>();
        if (font != null) vehTMP.font = font;
        vehTMP.fontSize = 14f;
        vehTMP.color = new Color(0.35f, 1.0f, 0.65f);
        hud.vehicleIdText = vehTMP;

        // Big Center Countdown
        GameObject countGO = CreateUIObject("CountdownNumeral", cockpitHUDGO.transform);
        SetRect(countGO.GetComponent<RectTransform>(), new Vector2(0.20f, 0.52f), new Vector2(0.80f, 0.72f));
        TextMeshProUGUI countTMP = countGO.AddComponent<TextMeshProUGUI>();
        if (font != null) countTMP.font = font;
        countTMP.text = "T-10";
        countTMP.fontSize = 72f;
        countTMP.fontStyle = FontStyles.Bold;
        countTMP.alignment = TextAlignmentOptions.Center;
        countTMP.color = new Color(0.35f, 0.90f, 1.0f);
        hud.countdownNumeralText = countTMP;

        GameObject countSub = CreateUIObject("CountdownSubStatus", cockpitHUDGO.transform);
        SetRect(countSub.GetComponent<RectTransform>(), new Vector2(0.15f, 0.44f), new Vector2(0.85f, 0.52f));
        TextMeshProUGUI countSubTMP = countSub.AddComponent<TextMeshProUGUI>();
        if (font != null) countSubTMP.font = font;
        countSubTMP.text = "POLLING FLIGHT STATIONS FOR LAUNCH COMMIT";
        countSubTMP.fontSize = 18f;
        countSubTMP.alignment = TextAlignmentOptions.Center;
        countSubTMP.color = new Color(0.88f, 0.92f, 0.98f);
        hud.countdownSubStatusText = countSubTMP;

        // Bottom Dashboard Strip
        GameObject dashStrip = CreateUIObject("TelemetryStrip", cockpitHUDGO.transform);
        SetRect(dashStrip.GetComponent<RectTransform>(), new Vector2(0.12f, 0.04f), new Vector2(0.88f, 0.16f));
        Image dashBg = dashStrip.AddComponent<Image>();
        dashBg.color = new Color(0.04f, 0.07f, 0.11f, 0.90f);
        dashStrip.AddComponent<Outline>().effectColor = new Color(0.2f, 0.6f, 0.9f, 0.7f);

        // Velocity Text
        GameObject velGO = CreateUIObject("VelocityText", dashStrip.transform);
        SetRect(velGO.GetComponent<RectTransform>(), new Vector2(0.03f, 0.55f), new Vector2(0.32f, 0.90f));
        TextMeshProUGUI velTMP = velGO.AddComponent<TextMeshProUGUI>();
        if (font != null) velTMP.font = font;
        velTMP.text = "VELOCITY: 7,650 M/S";
        velTMP.fontSize = 16f;
        velTMP.color = Color.white;
        hud.velocityText = velTMP;

        // Altitude Text
        GameObject altGO = CreateUIObject("AltitudeText", dashStrip.transform);
        SetRect(altGO.GetComponent<RectTransform>(), new Vector2(0.35f, 0.55f), new Vector2(0.65f, 0.90f));
        TextMeshProUGUI altTMP = altGO.AddComponent<TextMeshProUGUI>();
        if (font != null) altTMP.font = font;
        altTMP.text = "ALTITUDE: 408 KM";
        altTMP.fontSize = 16f;
        altTMP.color = Color.white;
        hud.altitudeText = altTMP;

        // Throttle Text & Bar
        GameObject thrGO = CreateUIObject("ThrottleText", dashStrip.transform);
        SetRect(thrGO.GetComponent<RectTransform>(), new Vector2(0.68f, 0.55f), new Vector2(0.97f, 0.90f));
        TextMeshProUGUI thrTMP = thrGO.AddComponent<TextMeshProUGUI>();
        if (font != null) thrTMP.font = font;
        thrTMP.text = "THROTTLE: 0%";
        thrTMP.fontSize = 16f;
        thrTMP.color = new Color(1f, 0.7f, 0.2f);
        hud.throttleText = thrTMP;

        // Throttle bar bg & fill
        GameObject thrBarBg = CreateUIObject("ThrottleBarBg", dashStrip.transform);
        SetRect(thrBarBg.GetComponent<RectTransform>(), new Vector2(0.68f, 0.25f), new Vector2(0.97f, 0.45f));
        Image tbBg = thrBarBg.AddComponent<Image>();
        tbBg.color = new Color(0.15f, 0.2f, 0.28f);

        GameObject thrBarFill = CreateUIObject("Fill", thrBarBg.transform);
        StretchFull(thrBarFill.GetComponent<RectTransform>());
        Image tbFill = thrBarFill.AddComponent<Image>();
        tbFill.type = Image.Type.Filled;
        tbFill.fillMethod = Image.FillMethod.Horizontal;
        tbFill.fillAmount = 0f;
        tbFill.color = new Color(0.2f, 0.7f, 1.0f);
        hud.throttleBar = tbFill;

        // Systems Status Strip
        GameObject sysGO = CreateUIObject("SystemsStatusText", dashStrip.transform);
        SetRect(sysGO.GetComponent<RectTransform>(), new Vector2(0.03f, 0.15f), new Vector2(0.65f, 0.45f));
        TextMeshProUGUI sysTMP = sysGO.AddComponent<TextMeshProUGUI>();
        if (font != null) sysTMP.font = font;
        sysTMP.text = "[PWR: NOMINAL] [NAV: LOCKED] [O2: 100%] [RCS: ACTIVE]";
        sysTMP.fontSize = 13f;
        sysTMP.color = new Color(0.35f, 1f, 0.55f);
        hud.systemsStatusText = sysTMP;

        cockpitHUDGO.SetActive(false);

        // 3. Flight Controls & Trajectory Bar (Active during manual launch flight)
        GameObject flightControlsPanel = CreateUIObject("FlightControlsPanel", canvasGO.transform);
        SetRect(flightControlsPanel.GetComponent<RectTransform>(), new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.12f));
        Image fcBg = flightControlsPanel.AddComponent<Image>();
        fcBg.color = new Color(0.04f, 0.07f, 0.12f, 0.94f);
        flightControlsPanel.AddComponent<Outline>().effectColor = new Color(0.2f, 0.85f, 1.0f, 0.85f);

        GameObject fcTextGO = CreateUIObject("ControlsText", flightControlsPanel.transform);
        SetRect(fcTextGO.GetComponent<RectTransform>(), new Vector2(0.02f, 0.10f), new Vector2(0.74f, 0.90f));
        TextMeshProUGUI fcTMP = fcTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) fcTMP.font = font;
        fcTMP.fontSize = 14f;
        fcTMP.color = Color.white;
        fcTMP.text = "AUTOMATED TRANSLUNAR GUIDANCE // DESTINATION: THE MOON // SPEED: 11,200 M/S\n<size=82%><color=#88CCFF>AUTOPILOT ONLINE: TRANSLUNAR COAST EN ROUTE   |   [ENTER / CLICK] Fast-Forward to Lunar Orbit   |   [ESC] Skip to Landing</color></size>";

        // Fast-Forward Button
        CreateButton("EngageCruiseButton", "FAST-FORWARD [ENTER]", font, flightControlsPanel.transform,
            new Vector2(0.76f, 0.15f), new Vector2(0.98f, 0.85f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.10f, 0.65f, 0.95f, 0.98f));
        flightControlsPanel.SetActive(false);

        // 4. Skip Button (Top Right)
        CreateButton("SkipButton", "SKIP LAUNCH [ESC] >", font, canvasGO.transform,
            new Vector2(0.82f, 0.91f), new Vector2(0.97f, 0.97f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.20f, 0.24f, 0.32f, 0.85f));

        // 5. Fade Overlay
        GameObject fadeGO = CreateUIObject("FadeOverlay", canvasGO.transform);
        StretchFull(fadeGO.GetComponent<RectTransform>());
        Image fadeImg = fadeGO.AddComponent<Image>();
        fadeImg.color = new Color(0.02f, 0.03f, 0.06f, 0f);
        fadeGO.SetActive(false);

        GameObject calloutGO = CreateUIObject("CalloutText", fadeGO.transform);
        SetRect(calloutGO.GetComponent<RectTransform>(), new Vector2(0.15f, 0.35f), new Vector2(0.85f, 0.65f));
        TextMeshProUGUI calloutTMP = calloutGO.AddComponent<TextMeshProUGUI>();
        if (font != null) calloutTMP.font = font;
        calloutTMP.text = "TRANSLUNAR INJECTION TRAJECTORY ACHIEVED";
        calloutTMP.fontSize = 28f;
        calloutTMP.fontStyle = FontStyles.Bold;
        calloutTMP.alignment = TextAlignmentOptions.Center;
        calloutTMP.color = new Color(0.4f, 0.85f, 1.0f, 0f);

        return canvasGO;
    }

    private static GameObject BuildFlightCanvas(TMP_FontAsset font)
    {
        GameObject canvasGO = new GameObject("FlightUI_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Left HUD telemetry panel
        GameObject hudPanel = CreateUIObject("HUDPanel", canvasGO.transform);
        SetRect(hudPanel.GetComponent<RectTransform>(), new Vector2(0.03f, 0.68f), new Vector2(0.48f, 0.96f));
        Image pBg = hudPanel.AddComponent<Image>();
        pBg.color = new Color(0.04f, 0.07f, 0.12f, 0.85f);
        hudPanel.AddComponent<Outline>().effectColor = new Color(0.25f, 0.75f, 1.0f, 0.7f);

        // Title
        GameObject tGO = CreateUIObject("TitleText", hudPanel.transform);
        SetRect(tGO.GetComponent<RectTransform>(), new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f));
        TextMeshProUGUI tTMP = tGO.AddComponent<TextMeshProUGUI>();
        if (font != null) tTMP.font = font;
        tTMP.text = "LUNAR EXPEDITION: ARTEMIS BEYOND";
        tTMP.fontSize = 20f;
        tTMP.fontStyle = FontStyles.Bold;
        tTMP.color = new Color(0.35f, 0.90f, 1.0f);

        // Phase
        GameObject pGO = CreateUIObject("PhaseText", hudPanel.transform);
        SetRect(pGO.GetComponent<RectTransform>(), new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.78f));
        TextMeshProUGUI pTMP = pGO.AddComponent<TextMeshProUGUI>();
        if (font != null) pTMP.font = font;
        pTMP.text = "PHASE: TRANSLUNAR CRUISE COAST // TRAJECTORY NOMINAL";
        pTMP.fontSize = 14f;
        pTMP.color = new Color(0.35f, 1.0f, 0.55f);

        // Speed
        GameObject sGO = CreateUIObject("SpeedText", hudPanel.transform);
        SetRect(sGO.GetComponent<RectTransform>(), new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.58f));
        TextMeshProUGUI sTMP = sGO.AddComponent<TextMeshProUGUI>();
        if (font != null) sTMP.font = font;
        sTMP.text = "CRUISE VELOCITY: 1,080 M/S (3,888 KM/H)";
        sTMP.fontSize = 15f;
        sTMP.color = Color.white;

        // Distance
        GameObject dGO = CreateUIObject("DistanceText", hudPanel.transform);
        SetRect(dGO.GetComponent<RectTransform>(), new Vector2(0.05f, 0.22f), new Vector2(0.95f, 0.40f));
        TextMeshProUGUI dTMP = dGO.AddComponent<TextMeshProUGUI>();
        if (font != null) dTMP.font = font;
        dTMP.text = "DISTANCE TO MOON: 284,000 KM";
        dTMP.fontSize = 15f;
        dTMP.color = new Color(1f, 0.85f, 0.35f);

        // Systems
        GameObject sysGO = CreateUIObject("SystemsText", hudPanel.transform);
        SetRect(sysGO.GetComponent<RectTransform>(), new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.22f));
        TextMeshProUGUI sysTMP = sysGO.AddComponent<TextMeshProUGUI>();
        if (font != null) sysTMP.font = font;
        sysTMP.text = "[LIFE SUPPORT: NOMINAL] [POWER: 98%] [DSN: LOCKED]";
        sysTMP.fontSize = 13f;
        sysTMP.color = new Color(0.6f, 0.85f, 1.0f);

        // Bottom Controls Banner
        GameObject bottomBar = CreateUIObject("BottomControls", canvasGO.transform);
        SetRect(bottomBar.GetComponent<RectTransform>(), new Vector2(0.20f, 0.03f), new Vector2(0.80f, 0.10f));
        Image bBg = bottomBar.AddComponent<Image>();
        bBg.color = new Color(0.05f, 0.08f, 0.14f, 0.85f);
        bottomBar.AddComponent<Outline>().effectColor = new Color(0.2f, 0.6f, 0.9f, 0.6f);

        GameObject cHint = CreateUIObject("HintText", bottomBar.transform);
        StretchFull(cHint.GetComponent<RectTransform>());
        TextMeshProUGUI cTMP = cHint.AddComponent<TextMeshProUGUI>();
        if (font != null) cTMP.font = font;
        cTMP.text = "CONTROLS: [A / D] OR [LEFT / RIGHT ARROWS] TO EXECUTE ROLL MANEUVERS";
        cTMP.fontSize = 15f;
        cTMP.alignment = TextAlignmentOptions.Center;
        cTMP.color = new Color(0.75f, 0.90f, 1.0f);

        // Return Button (Top Right)
        CreateButton("ReturnButton", "RETURN TO MISSIONS", font, canvasGO.transform,
            new Vector2(0.80f, 0.90f), new Vector2(0.97f, 0.97f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.18f, 0.24f, 0.35f, 0.90f));

        return canvasGO;
    }

    // ===================================================================
    // 6. BUILD SETTINGS UPDATE
    // ===================================================================

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

    // ===================================================================
    // HELPER METHODS
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
        tmp.fontSize = 15f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnGO;
    }

    private static Material GetOrCreateLitMaterial(string matName, Color color, float metallic, float smoothness, Texture2D texture = null)
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
        if (texture != null)
        {
            mat.SetTexture("_BaseMap", texture);
            mat.mainTexture = texture;
        }
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

        // Enable alpha transparency
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

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}

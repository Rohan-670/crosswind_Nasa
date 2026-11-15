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
/// Scene automation builder for EarthReturnFlight:
/// constructs the complete trans-Earth return gameplay sequence including
/// receding Moon, approaching Earth, 5 return checkpoints, navigation hazards,
/// mini-map, atmospheric re-entry heat shield corridor, and runway landing approach.
/// </summary>
public class EarthReturnFlightSceneBuilder
{
    private const string ScenesPath = "Assets/Scenes";
    private const string MaterialsPath = "Assets/Materials";
    private const string PrefabsPath = "Assets/Prefabs";

    [MenuItem("ASTROBASE/Missions/Build Earth Return Flight Scene")]
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
            if (!File.Exists(ScenesPath + "/EarthReturnFlight.unity"))
            {
                Debug.Log("[ASTROBASE] Initializing EarthReturnFlight scene...");
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

            BuildReturnScene(font);
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: Earth Return Flight Built!",
                    "Scene created at:\n  Assets/Scenes/EarthReturnFlight.unity\n\n" +
                    "  ✓ Trans-Earth deep space flight corridor with receding Moon & approaching Earth\n" +
                    "  ✓ 5 Sequential return checkpoints & space hazards (debris swarm, radiation storm)\n" +
                    "  ✓ Top-Right Navigation Mini-Map with return route plotting\n" +
                    "  ✓ Atmospheric Re-entry with heat shield & pitch corridor monitoring\n" +
                    "  ✓ Final runway glide-slope approach with radar altimeter & sink rate control\n" +
                    "  ✓ Safe touchdown detection & transition to MissionResults!\n\n" +
                    "Press Play ▶ in EarthReturnFlight to test the complete return sequence!",
                    "OK");
            }
            Debug.Log("[ASTROBASE] Earth Return Flight gameplay scene built successfully.");
        }
        catch (System.Exception ex)
        {
            if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", ex.Message, "OK");
            Debug.LogError("[ASTROBASE] EarthReturnFlight build failed: " + ex);
        }
    }

    private static void BuildReturnScene(TMP_FontAsset font)
    {
        string scenePath = ScenesPath + "/EarthReturnFlight.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Lighting & Environment
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.04f, 0.05f, 0.08f);

        GameObject sunGO = new GameObject("Sunlight");
        Light sunLight = sunGO.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1.0f, 0.98f, 0.92f);
        sunLight.intensity = 1.35f;
        sunGO.transform.rotation = Quaternion.Euler(30f, -40f, 0f);

        // Starfield
        BuildStarfield("DeepSpaceStarfield", 3000, 500f);

        // Receding Moon behind spacecraft (z = -350)
        GameObject moonGO = BuildRecedingMoon(new Vector3(0f, -20f, -350f), 70f);

        // Approaching Earth in front (z = +950)
        GameObject earthGO = BuildApproachingEarth(new Vector3(0f, 0f, 950f), 90f);

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

        // Visual Spacecraft Mesh
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
        cockpitAnchor.transform.localPosition = new Vector3(0f, 0.85f, 1.85f);
        shipController.cockpitEyeAnchor = cockpitAnchor.transform;

        // Cockpit Camera (First-Person)
        GameObject cockCamGO = new GameObject("Camera_Cockpit_FirstPerson");
        cockCamGO.transform.SetParent(cockpitAnchor.transform, false);
        cockCamGO.transform.localPosition = Vector3.zero;
        Camera cockCam = cockCamGO.AddComponent<Camera>();
        cockCam.clearFlags = CameraClearFlags.SolidColor;
        cockCam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
        cockCam.nearClipPlane = 0.05f;
        cockCam.farClipPlane = 3000f;
        cockCamGO.AddComponent<AudioListener>();
        shipController.cockpitCamera = cockCam;

        // Chase Camera (Third-Person)
        GameObject chaseCamGO = new GameObject("Camera_Chase_ThirdPerson");
        chaseCamGO.transform.SetParent(spacecraftRoot.transform, false);
        chaseCamGO.transform.localPosition = new Vector3(0f, 3.2f, -10.5f);
        chaseCamGO.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
        Camera chaseCam = chaseCamGO.AddComponent<Camera>();
        chaseCam.clearFlags = CameraClearFlags.SolidColor;
        chaseCam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
        chaseCam.nearClipPlane = 0.1f;
        chaseCam.farClipPlane = 3000f;
        shipController.chaseCamera = chaseCam;

        // Specialized Return Flight VFX System (Speedlines, Re-entry Plasma & Touchdown Smoke)
        ReturnFlightVFXController vfxCtrl;
        GameObject plasmaRoot = BuildReturnVFXSystem(spacecraftRoot.transform, cockCam, chaseCam, shipController, out vfxCtrl);

        // 4. Return Flight Route & Checkpoints (5 Sequential Gates)
        Vector3[] returnPoints = new Vector3[]
        {
            new Vector3(0f, 0f, 0f),       // Departure
            new Vector3(0f, 0f, 100f),     // Gate 1: Trans-Earth Injection
            new Vector3(25f, 12f, 280f),   // Gate 2: Midcourse Correction
            new Vector3(-15f, -8f, 480f),  // Gate 3: Cislunar Gravity Boundary
            new Vector3(10f, 5f, 700f),    // Gate 4: Earth Orbital Interface
            new Vector3(0f, 0f, 900f)      // Gate 5: Atmospheric Entry Interface
        };

        string[] gateNames = new string[]
        {
            "TEI-01 (Trans-Earth Injection)",
            "MCC-02 (Midcourse Course Correction)",
            "CIS-03 (Cislunar Gravity Boundary)",
            "EAG-04 (Earth Orbital Interface)",
            "EI-05 (Atmospheric Entry Interface)"
        };

        GameObject routeRoot = new GameObject("Return_Route_System");
        RouteManager routeMgr = routeRoot.AddComponent<RouteManager>();
        routeMgr.spacecraft = shipController;
        routeMgr.warningCorridorRadius = 45f;
        routeMgr.criticalCorridorRadius = 80f;
        routeMgr.recoveryTimeLimit = 14f;

        GameObject checkpointRoot = new GameObject("Return_Checkpoint_System");
        CheckpointManager cpMgr = checkpointRoot.AddComponent<CheckpointManager>();
        cpMgr.spacecraft = shipController;
        cpMgr.fuelBonusPerCheckpoint = 15f;

        List<Transform> routeWaypoints = new List<Transform>();
        for (int i = 0; i < returnPoints.Length; i++)
        {
            GameObject wp = new GameObject($"Waypoint_{i:D2}");
            wp.transform.SetParent(routeRoot.transform, false);
            wp.transform.position = returnPoints[i];
            routeWaypoints.Add(wp.transform);

            if (i > 0)
            {
                bool isDest = (i == returnPoints.Length - 1);
                Checkpoint cp = BuildCheckpoint(checkpointRoot.transform, returnPoints[i], gateNames[i - 1], 18f, isDest);
                cpMgr.checkpoints.Add(cp);
            }
        }
        routeMgr.routeWaypoints = routeWaypoints;

        // 5. Deep Space Return Hazards
        GameObject hazardRoot = new GameObject("Return_Hazard_System");
        HazardManager hazardMgr = hazardRoot.AddComponent<HazardManager>();
        hazardMgr.spacecraft = shipController;

        HazardZone h1 = BuildHazardZone(hazardRoot.transform, "Orbital Debris Belt Alpha", HazardZone.HazardType.SpaceDebrisField, new Vector3(20f, 8f, 260f), 32f, 12f);
        HazardZone h2 = BuildHazardZone(hazardRoot.transform, "Solar Radiation Burst Beta", HazardZone.HazardType.RadiationStorm, new Vector3(-12f, -4f, 460f), 38f, 15f);
        hazardMgr.hazardZones.Add(h1);
        hazardMgr.hazardZones.Add(h2);

        // 6. Landing Runway System (at Earth landing site)
        GameObject runwayRoot = BuildLandingRunway(new Vector3(0f, -50f, 1350f));

        // 7. Return Flight UI Canvas
        EnsureEventSystem();
        GameObject canvasGO = BuildReturnCanvas(font, shipController, routeMgr, cpMgr, hazardMgr, cockCam);
        FlightHUDController hud = canvasGO.GetComponent<FlightHUDController>();
        hud.nextSceneName = "MissionResults";

        // 8. Earth Re-entry & Landing Controller
        EarthReentryAndLandingController reentryCtrl = spacecraftRoot.AddComponent<EarthReentryAndLandingController>();
        reentryCtrl.spacecraft = shipController;
        reentryCtrl.flightHUD = hud;
        reentryCtrl.checkpointManager = cpMgr;
        reentryCtrl.routeManager = routeMgr;
        reentryCtrl.earthModel = earthGO.transform;
        reentryCtrl.vfxController = vfxCtrl;
        reentryCtrl.plasmaFXRoot = plasmaRoot;
        reentryCtrl.plasmaLights = vfxCtrl != null ? vfxCtrl.plasmaLights : plasmaRoot.GetComponentsInChildren<Light>();
        reentryCtrl.plasmaParticles = vfxCtrl != null ? vfxCtrl.plasmaFireParticles : null;
        reentryCtrl.runwayRoot = runwayRoot.transform;
        reentryCtrl.touchdownTargetPoint = runwayRoot.transform.Find("TouchdownTarget");
        reentryCtrl.missionResultsSceneName = "MissionResults";

        // Wire Reentry HUD Overlays
        WireReentryHUD(canvasGO.transform, font, reentryCtrl);

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static GameObject BuildRecedingMoon(Vector3 pos, float radius)
    {
        GameObject moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moon.name = "Receding_Moon";
        moon.transform.position = pos;
        moon.transform.localScale = Vector3.one * radius;
        moon.GetComponent<Renderer>().sharedMaterial = GetOrCreateLitMaterial("Receding_Moon_Mat", new Color(0.72f, 0.74f, 0.76f), 0.05f, 0.2f, null);
        Object.DestroyImmediate(moon.GetComponent<Collider>());
        return moon;
    }

    private static GameObject BuildApproachingEarth(Vector3 pos, float initialRadius)
    {
        GameObject earthRoot = new GameObject("Approaching_Earth_Root");
        earthRoot.transform.position = pos;
        earthRoot.transform.localScale = Vector3.one * initialRadius;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Earth_Sphere";
        sphere.transform.SetParent(earthRoot.transform, false);
        sphere.transform.localScale = Vector3.one;

        // Try load realistic NASA earth texture if available
        Texture2D earthTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/NASA_Textures/earth_nasa_diffuse.jpg") ??
                             AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/NASA_Textures/earth_daymap.jpg");

        Material earthMat = GetOrCreateLitMaterial("Approaching_Earth_Mat", new Color(0.9f, 0.95f, 1f), 0.05f, 0.45f, earthTex);
        sphere.GetComponent<Renderer>().sharedMaterial = earthMat;
        Object.DestroyImmediate(sphere.GetComponent<Collider>());

        // Atmosphere glow rim
        GameObject atmo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        atmo.name = "Atmosphere_Glow";
        atmo.transform.SetParent(earthRoot.transform, false);
        atmo.transform.localScale = Vector3.one * 1.04f;
        Material atmoMat = GetOrCreateUnlitMaterial("Earth_Atmo_Glow_Mat", new Color(0.35f, 0.75f, 1.0f, 0.35f));
        atmo.GetComponent<Renderer>().sharedMaterial = atmoMat;
        Object.DestroyImmediate(atmo.GetComponent<Collider>());

        return earthRoot;
    }

    private static GameObject BuildReturnVFXSystem(Transform parent, Camera cockCam, Camera chaseCam, SpacecraftController ship, out ReturnFlightVFXController vfxCtrl)
    {
        // 1. Cosmic Dust Speedlines (Trans-Earth cruise)
        GameObject speedlinesGO = new GameObject("VFX_Speedlines");
        speedlinesGO.transform.SetParent(parent, false);
        speedlinesGO.transform.localPosition = new Vector3(0f, 0f, 15f);
        ParticleSystem speedlinesPS = speedlinesGO.AddComponent<ParticleSystem>();
        var sMain = speedlinesPS.main;
        sMain.loop = true;
        sMain.playOnAwake = true;
        sMain.startLifetime = 0.5f;
        sMain.startSpeed = 60f;
        sMain.startSize = 0.18f;
        sMain.startColor = new Color(0.6f, 0.92f, 1.0f, 0.85f);
        sMain.maxParticles = 500;
        sMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        var sEmission = speedlinesPS.emission;
        sEmission.rateOverTime = 60f;
        var sShape = speedlinesPS.shape;
        sShape.shapeType = ParticleSystemShapeType.Box;
        sShape.scale = new Vector3(24f, 16f, 10f);
        sShape.rotation = new Vector3(0f, 180f, 0f);

        var sRenderer = speedlinesGO.GetComponent<ParticleSystemRenderer>();
        sRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        sRenderer.cameraVelocityScale = 0f;
        sRenderer.velocityScale = 0.35f;
        sRenderer.lengthScale = 4.0f;
        sRenderer.sharedMaterial = GetOrCreateParticleMaterial("Return_Speedline_Mat", new Color(0.6f, 0.95f, 1f, 0.85f), true);

        // 2. Re-entry Plasma FX Root
        GameObject plasmaRoot = new GameObject("Reentry_Plasma_FX");
        plasmaRoot.transform.SetParent(parent, false);
        plasmaRoot.transform.localPosition = new Vector3(0f, 0.2f, 2.2f);

        // Plasma Flame Trail Particles
        GameObject flameGO = new GameObject("Plasma_Flame_Trail");
        flameGO.transform.SetParent(plasmaRoot.transform, false);
        flameGO.transform.localPosition = Vector3.zero;
        ParticleSystem flamePS = flameGO.AddComponent<ParticleSystem>();
        var fMain = flamePS.main;
        fMain.loop = true;
        fMain.playOnAwake = false;
        fMain.startLifetime = 0.65f;
        fMain.startSpeed = 24f;
        fMain.startSize = 2.4f;
        fMain.startColor = new Color(1.0f, 0.65f, 0.15f, 0.88f);
        fMain.maxParticles = 300;
        var fEmission = flamePS.emission;
        fEmission.rateOverTime = 90f;
        var fShape = flamePS.shape;
        fShape.shapeType = ParticleSystemShapeType.Cone;
        fShape.angle = 12f;
        fShape.radius = 1.2f;
        fShape.rotation = new Vector3(0f, 180f, 0f);

        var fRenderer = flameGO.GetComponent<ParticleSystemRenderer>();
        fRenderer.sharedMaterial = GetOrCreateParticleMaterial("Reentry_Flame_Mat", new Color(1.0f, 0.6f, 0.15f, 0.9f), true);

        // Plasma Ablation Sparks
        GameObject sparksGO = new GameObject("Plasma_Ablation_Sparks");
        sparksGO.transform.SetParent(plasmaRoot.transform, false);
        sparksGO.transform.localPosition = Vector3.zero;
        ParticleSystem sparksPS = sparksGO.AddComponent<ParticleSystem>();
        var spMain = sparksPS.main;
        spMain.loop = true;
        spMain.playOnAwake = false;
        spMain.startLifetime = 0.5f;
        spMain.startSpeed = 35f;
        spMain.startSize = 0.25f;
        spMain.startColor = new Color(1.0f, 0.95f, 0.5f, 0.95f);
        spMain.maxParticles = 200;
        var spEmission = sparksPS.emission;
        spEmission.rateOverTime = 50f;
        var spShape = sparksPS.shape;
        spShape.shapeType = ParticleSystemShapeType.Cone;
        spShape.angle = 20f;
        spShape.radius = 1.0f;
        spShape.rotation = new Vector3(0f, 180f, 0f);

        var spRenderer = sparksGO.GetComponent<ParticleSystemRenderer>();
        spRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        spRenderer.velocityScale = 0.2f;
        spRenderer.lengthScale = 2.5f;
        spRenderer.sharedMaterial = GetOrCreateParticleMaterial("Reentry_Spark_Mat", new Color(1f, 0.9f, 0.4f, 0.95f), true);

        // Plasma Lights
        GameObject light1 = new GameObject("PlasmaLight_Center");
        light1.transform.SetParent(plasmaRoot.transform, false);
        Light l1 = light1.AddComponent<Light>();
        l1.type = LightType.Point;
        l1.color = new Color(1.0f, 0.55f, 0.15f);
        l1.range = 20f;
        l1.intensity = 4.0f;

        GameObject light2 = new GameObject("PlasmaLight_Shield");
        light2.transform.SetParent(plasmaRoot.transform, false);
        light2.transform.localPosition = new Vector3(0f, -0.6f, -0.5f);
        Light l2 = light2.AddComponent<Light>();
        l2.type = LightType.Point;
        l2.color = new Color(1.0f, 0.85f, 0.35f);
        l2.range = 14f;
        l2.intensity = 3.0f;

        // Plasma Shield Shell Mesh
        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = "PlasmaShieldMesh";
        shell.transform.SetParent(plasmaRoot.transform, false);
        shell.transform.localPosition = new Vector3(0f, 0f, 0f);
        shell.transform.localScale = new Vector3(5.2f, 2.8f, 7.2f);
        Material pMat = GetOrCreateParticleMaterial("Reentry_Plasma_Mat", new Color(1.0f, 0.55f, 0.15f, 0.4f), true);
        Renderer shellRend = shell.GetComponent<Renderer>();
        shellRend.sharedMaterial = pMat;
        Object.DestroyImmediate(shell.GetComponent<Collider>());

        // 3. Touchdown Tire Smoke FX
        GameObject smokeGO = new GameObject("Touchdown_Tire_Smoke");
        smokeGO.transform.SetParent(parent, false);
        smokeGO.transform.localPosition = new Vector3(0f, -0.8f, -0.5f);
        ParticleSystem smokePS = smokeGO.AddComponent<ParticleSystem>();
        var smMain = smokePS.main;
        smMain.loop = false;
        smMain.playOnAwake = false;
        smMain.startLifetime = 1.2f;
        smMain.startSpeed = 8f;
        smMain.startSize = 2.0f;
        smMain.startColor = new Color(0.85f, 0.85f, 0.88f, 0.6f);
        smMain.maxParticles = 60;
        var smEmission = smokePS.emission;
        smEmission.rateOverTime = 0f;
        smEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 35) });
        var smShape = smokePS.shape;
        smShape.shapeType = ParticleSystemShapeType.Sphere;
        smShape.radius = 1.5f;

        var smRenderer = smokeGO.GetComponent<ParticleSystemRenderer>();
        smRenderer.sharedMaterial = GetOrCreateParticleMaterial("Touchdown_Smoke_Mat", new Color(0.85f, 0.85f, 0.88f, 0.5f), false);

        // 4. ReturnFlightVFXController Component
        vfxCtrl = parent.gameObject.AddComponent<ReturnFlightVFXController>();
        vfxCtrl.spacecraft = ship;
        vfxCtrl.cockpitCamera = cockCam;
        vfxCtrl.chaseCamera = chaseCam;
        vfxCtrl.cosmicSpeedlines = speedlinesPS;
        vfxCtrl.plasmaFXRoot = plasmaRoot;
        vfxCtrl.plasmaFireParticles = flamePS;
        vfxCtrl.plasmaSparksParticles = sparksPS;
        vfxCtrl.plasmaLights = new Light[] { l1, l2 };
        vfxCtrl.plasmaShieldRenderer = shellRend;
        vfxCtrl.touchdownSmokeParticles = smokePS;

        plasmaRoot.SetActive(false);
        return plasmaRoot;
    }

    private static GameObject BuildLandingRunway(Vector3 pos)
    {
        GameObject runwayRoot = new GameObject("Landing_Runway_KSC");
        runwayRoot.transform.position = pos;

        // 1. Runway Tarmac Surface (350m long, 35m wide)
        GameObject tarmac = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tarmac.name = "Tarmac";
        tarmac.transform.SetParent(runwayRoot.transform, false);
        tarmac.transform.localPosition = new Vector3(0f, -0.5f, 175f);
        tarmac.transform.localScale = new Vector3(35f, 1f, 350f);
        Material tarmacMat = GetOrCreateLitMaterial("Runway_Tarmac_Mat", new Color(0.18f, 0.18f, 0.20f), 0.1f, 0.25f, null);
        tarmac.GetComponent<Renderer>().sharedMaterial = tarmacMat;

        // 2. Centerline Stripes
        for (int i = 0; i < 14; i++)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = $"Centerline_{i}";
            stripe.transform.SetParent(runwayRoot.transform, false);
            stripe.transform.localPosition = new Vector3(0f, 0.05f, 25f + i * 24f);
            stripe.transform.localScale = new Vector3(1.2f, 0.05f, 14f);
            Material stripeMat = GetOrCreateUnlitMaterial("Runway_White_Stripe", Color.white);
            stripe.GetComponent<Renderer>().sharedMaterial = stripeMat;
            Object.DestroyImmediate(stripe.GetComponent<Collider>());
        }

        // 3. Green Threshold Approach Lights
        for (int i = -3; i <= 3; i++)
        {
            GameObject greenLight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            greenLight.name = $"ThresholdLight_{i}";
            greenLight.transform.SetParent(runwayRoot.transform, false);
            greenLight.transform.localPosition = new Vector3(i * 5f, 0.2f, 0f);
            greenLight.transform.localScale = new Vector3(0.6f, 0.3f, 0.6f);
            Material greenMat = GetOrCreateUnlitMaterial("Runway_Green_Light", new Color(0.2f, 1.0f, 0.4f, 1.0f));
            greenLight.GetComponent<Renderer>().sharedMaterial = greenMat;
            Object.DestroyImmediate(greenLight.GetComponent<Collider>());

            GameObject gl = new GameObject("G_Light");
            gl.transform.SetParent(greenLight.transform, false);
            Light l = gl.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.2f, 1.0f, 0.4f);
            l.range = 8f;
            l.intensity = 1.5f;
        }

        // 4. Red Safety Overrun Lights at Runway End
        for (int i = -3; i <= 3; i++)
        {
            GameObject redLight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            redLight.name = $"EndLight_{i}";
            redLight.transform.SetParent(runwayRoot.transform, false);
            redLight.transform.localPosition = new Vector3(i * 5f, 0.2f, 350f);
            redLight.transform.localScale = new Vector3(0.6f, 0.3f, 0.6f);
            Material redMat = GetOrCreateUnlitMaterial("Runway_Red_Light", new Color(1.0f, 0.2f, 0.2f, 1.0f));
            redLight.GetComponent<Renderer>().sharedMaterial = redMat;
            Object.DestroyImmediate(redLight.GetComponent<Collider>());
        }

        // 5. Touchdown Target Anchor
        GameObject targetAnchor = new GameObject("TouchdownTarget");
        targetAnchor.transform.SetParent(runwayRoot.transform, false);
        targetAnchor.transform.localPosition = new Vector3(0f, 0.1f, 80f);

        return runwayRoot;
    }

    private static Checkpoint BuildCheckpoint(Transform parent, Vector3 pos, string cpName, float radius, bool isDest)
    {
        GameObject go = new GameObject(cpName.Replace(" ", "_"));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "RingVisual";
        ring.transform.SetParent(go.transform, false);
        ring.transform.localScale = new Vector3(radius * 2f, 0.35f, radius * 2f);
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        Color c = isDest ? new Color(0.35f, 1.0f, 0.55f, 0.85f) : new Color(0.25f, 0.85f, 1.0f, 0.75f);
        Material mat = GetOrCreateUnlitMaterial(isDest ? "Checkpoint_Dest_Mat" : "Checkpoint_Gate_Mat", c);
        ring.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(ring.GetComponent<Collider>());

        Checkpoint cp = go.AddComponent<Checkpoint>();
        cp.checkpointName = cpName;
        cp.triggerRadius = radius;
        cp.isFinalDestination = isDest;
        cp.ringRenderer = ring.GetComponent<Renderer>();

        GameObject lightGO = new GameObject("BeaconLight");
        lightGO.transform.SetParent(go.transform, false);
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.range = radius * 1.8f;
        l.intensity = 2.0f;
        cp.beaconLight = l;

        return cp;
    }

    private static HazardZone BuildHazardZone(Transform parent, string hName, HazardZone.HazardType type, Vector3 pos, float radius, float dps)
    {
        GameObject go = new GameObject(hName.Replace(" ", "_"));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "HazardBoundary";
        sphere.transform.SetParent(go.transform, false);
        sphere.transform.localScale = Vector3.one * (radius * 2f);
        Object.DestroyImmediate(sphere.GetComponent<Collider>());

        Material hMat = GetOrCreateUnlitMaterial("Hazard_Return_Mat", new Color(1.0f, 0.35f, 0.15f, 0.22f));
        sphere.GetComponent<Renderer>().sharedMaterial = hMat;

        HazardZone hz = go.AddComponent<HazardZone>();
        hz.hazardName = hName;
        hz.hazardType = type;
        hz.radius = radius;
        hz.damagePerSecond = dps;
        hz.boundaryRenderer = sphere.GetComponent<Renderer>();

        return hz;
    }

    private static void BuildSpacecraftMesh(Transform parent)
    {
        string shuttlePrefabPath = "Assets/Prefabs/Spacecraft/SpaceShuttle.prefab";
        GameObject shuttlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(shuttlePrefabPath);
        if (shuttlePrefab != null)
        {
            GameObject ship = (GameObject)PrefabUtility.InstantiatePrefab(shuttlePrefab, parent);
            ship.name = "SpaceShuttle_Mesh";
            ship.transform.localPosition = Vector3.zero;
            ship.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Fallback primitive
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Fuselage";
            body.transform.SetParent(parent, false);
            body.transform.localScale = new Vector3(2.5f, 4.5f, 2.5f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = GetOrCreateLitMaterial("Shuttle_Fallback_Mat", new Color(0.9f, 0.9f, 0.92f), 0.1f, 0.4f, null);
        }
    }

    private static void BuildEngineThrusters(Transform parent, List<Light> lights, List<GameObject> plumes)
    {
        Vector3[] engineOffsets = new Vector3[]
        {
            new Vector3(0f, 0.4f, -2.8f),
            new Vector3(-0.75f, -0.3f, -2.8f),
            new Vector3(0.75f, -0.3f, -2.8f)
        };

        for (int i = 0; i < engineOffsets.Length; i++)
        {
            GameObject elGO = new GameObject($"EngineLight_{i}");
            elGO.transform.SetParent(parent, false);
            elGO.transform.localPosition = engineOffsets[i];
            Light l = elGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.35f, 0.85f, 1.0f);
            l.range = 10f;
            l.intensity = 0f;
            l.enabled = false;
            lights.Add(l);

            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p.name = $"Plume_{i}";
            p.transform.SetParent(parent, false);
            p.transform.localPosition = engineOffsets[i] + new Vector3(0f, 0f, -0.8f);
            p.transform.localScale = new Vector3(0.5f, 1.0f, 0.5f);
            p.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            p.GetComponent<Renderer>().sharedMaterial = GetOrCreateUnlitMaterial("Return_Plume_Mat", new Color(0.35f, 0.85f, 1.0f, 0.85f));
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.SetActive(false);
            plumes.Add(p);
        }
    }

    private static void BuildStarfield(string name, int count, float radius)
    {
        GameObject starGO = new GameObject(name);
        ParticleSystem ps = starGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = starGO.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = count;

        var emission = ps.emission;
        emission.enabled = false;

        Material starMat = GetOrCreateUnlitMaterial("Star_Point_Mat", Color.white);
        psr.sharedMaterial = starMat;

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[count];
        for (int i = 0; i < count; i++)
        {
            particles[i].position = Random.onUnitSphere * radius;
            particles[i].startSize = Random.Range(1.0f, 2.8f);
            particles[i].startColor = Random.value > 0.8f ? new Color(0.8f, 0.9f, 1.0f) : Color.white;
            particles[i].remainingLifetime = 99999f;
        }
        ps.SetParticles(particles, count);
    }

    private static GameObject BuildReturnCanvas(TMP_FontAsset font, SpacecraftController ship, RouteManager route, CheckpointManager cp, HazardManager hz, Camera cockCam)
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
        hud.spacecraft = ship;
        hud.routeManager = route;
        hud.checkpointManager = cp;
        hud.hazardManager = hz;
        hud.flightCamera = cockCam;

        // Vitals Panel (Speed, Throttle, Fuel, Hull)
        GameObject vitalsPanel = CreateUIObject("VitalsPanel", canvasGO.transform);
        SetRect(vitalsPanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.74f), new Vector2(0.32f, 0.98f));
        Image vBg = vitalsPanel.AddComponent<Image>();
        vBg.color = new Color(0.04f, 0.07f, 0.12f, 0.88f);
        vitalsPanel.AddComponent<Outline>().effectColor = new Color(0.25f, 0.75f, 1.0f, 0.75f);

        GameObject titleGO = CreateUIObject("Title", vitalsPanel.transform);
        SetRect(titleGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.96f));
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTMP.font = font;
        titleTMP.text = "ASTROBASE // TRANS-EARTH RETURN CORRIDOR";
        titleTMP.fontSize = 15f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(0.35f, 0.90f, 1.0f);

        GameObject spdGO = CreateUIObject("SpeedText", vitalsPanel.transform);
        SetRect(spdGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.65f), new Vector2(0.96f, 0.80f));
        TextMeshProUGUI spdTMP = spdGO.AddComponent<TextMeshProUGUI>();
        if (font != null) spdTMP.font = font;
        spdTMP.text = "SPEED: 0.0 M/S";
        spdTMP.fontSize = 15f;
        spdTMP.color = Color.white;
        hud.speedText = spdTMP;

        // Fuel & Hull Bars
        GameObject fuelTextGO = CreateUIObject("FuelText", vitalsPanel.transform);
        SetRect(fuelTextGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.44f), new Vector2(0.40f, 0.58f));
        TextMeshProUGUI fTMP = fuelTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) fTMP.font = font;
        fTMP.text = "FUEL: 100%";
        fTMP.fontSize = 13f;
        fTMP.color = new Color(0.35f, 0.9f, 1f);
        hud.fuelText = fTMP;

        hud.fuelBar = CreateBar("FuelBar", vitalsPanel.transform, new Vector2(0.42f, 0.46f), new Vector2(0.96f, 0.56f), new Color(0.2f, 0.85f, 1f));

        GameObject hullTextGO = CreateUIObject("HullText", vitalsPanel.transform);
        SetRect(hullTextGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.26f), new Vector2(0.40f, 0.40f));
        TextMeshProUGUI hTMP = hullTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) hTMP.font = font;
        hTMP.text = "HULL: 100%";
        hTMP.fontSize = 13f;
        hTMP.color = new Color(0.4f, 1f, 0.5f);
        hud.hullText = hTMP;

        hud.hullBar = CreateBar("HullBar", vitalsPanel.transform, new Vector2(0.42f, 0.28f), new Vector2(0.96f, 0.38f), new Color(0.35f, 1.0f, 0.55f));

        // Navigation mini-map (Top-Right)
        BuildMiniMap(canvasGO.transform, hud, ship, route, cp, hz);

        // Banners
        hud.deviationBanner = BuildWarningBanner("DeviationBanner", canvasGO.transform, font, "TRAJECTORY DEVIATION WARNING", out hud.deviationTitleText, out hud.deviationTimerText);
        hud.hazardBanner = BuildHazardBanner("HazardBanner", canvasGO.transform, font, out hud.hazardTitleText);

        // Bottom Controls Hint Bar
        GameObject hintGO = CreateUIObject("ControlsHint", canvasGO.transform);
        SetRect(hintGO.GetComponent<RectTransform>(), new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.07f));
        Image hintBg = hintGO.AddComponent<Image>();
        hintBg.color = new Color(0.04f, 0.07f, 0.12f, 0.85f);
        hintGO.AddComponent<Outline>().effectColor = new Color(0.25f, 0.65f, 0.9f, 0.6f);

        GameObject htGO = CreateUIObject("HintText", hintGO.transform);
        StretchFull(htGO.GetComponent<RectTransform>());
        TextMeshProUGUI htTMP = htGO.AddComponent<TextMeshProUGUI>();
        if (font != null) htTMP.font = font;
        htTMP.text = "CONTROLS: [W/S] Throttle | [A/D] Steer Yaw | [R/F or I/K] Pitch Corridor | [Space/Ctrl] Vertical | [Shift] Boost | [V] Camera View";
        htTMP.fontSize = 13f;
        htTMP.fontStyle = FontStyles.Bold;
        htTMP.alignment = TextAlignmentOptions.Center;
        htTMP.color = new Color(0.80f, 0.92f, 1.0f);
        hud.controlsHintText = htTMP;

        return canvasGO;
    }

    private static void WireReentryHUD(Transform canvasTransform, TMP_FontAsset font, EarthReentryAndLandingController ctrl)
    {
        // 1. Re-entry Corridor & Heat Shield Overlay (Center-Left)
        GameObject rePanel = CreateUIObject("ReentryOverlayPanel", canvasTransform);
        SetRect(rePanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.48f), new Vector2(0.32f, 0.72f));
        Image rBg = rePanel.AddComponent<Image>();
        rBg.color = new Color(0.12f, 0.05f, 0.02f, 0.92f);
        rePanel.AddComponent<Outline>().effectColor = new Color(1.0f, 0.55f, 0.2f, 0.85f);

        GameObject rTitle = CreateUIObject("Title", rePanel.transform);
        SetRect(rTitle.GetComponent<RectTransform>(), new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.95f));
        TextMeshProUGUI rtTMP = rTitle.AddComponent<TextMeshProUGUI>();
        if (font != null) rtTMP.font = font;
        rtTMP.text = "[ ATMOSPHERIC RE-ENTRY CORRIDOR ]";
        rtTMP.fontSize = 14f;
        rtTMP.fontStyle = FontStyles.Bold;
        rtTMP.color = new Color(1.0f, 0.65f, 0.25f);

        GameObject pPitch = CreateUIObject("PitchCorridorText", rePanel.transform);
        SetRect(pPitch.GetComponent<RectTransform>(), new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.75f));
        TextMeshProUGUI ppTMP = pPitch.AddComponent<TextMeshProUGUI>();
        if (font != null) ppTMP.font = font;
        ppTMP.text = "RE-ENTRY PITCH: +10.0° [CORRIDOR OPTIMAL]";
        ppTMP.fontSize = 13f;
        ppTMP.color = new Color(0.35f, 1.0f, 0.55f);
        ctrl.reentryCorridorText = ppTMP;

        GameObject pHeat = CreateUIObject("HeatShieldText", rePanel.transform);
        SetRect(pHeat.GetComponent<RectTransform>(), new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.48f));
        TextMeshProUGUI phTMP = pHeat.AddComponent<TextMeshProUGUI>();
        if (font != null) phTMP.font = font;
        phTMP.text = "HEAT SHIELD: 650°C";
        phTMP.fontSize = 14f;
        phTMP.color = new Color(1.0f, 0.85f, 0.35f);
        ctrl.heatShieldTempText = phTMP;

        ctrl.heatShieldBar = CreateBar("HeatBar", rePanel.transform, new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.24f), new Color(1.0f, 0.45f, 0.15f));
        ctrl.reentryOverlayPanel = rePanel;
        rePanel.SetActive(false);

        // 2. Landing Guidance Panel (Center-Bottom)
        GameObject landPanel = CreateUIObject("LandingGuidancePanel", canvasTransform);
        SetRect(landPanel.GetComponent<RectTransform>(), new Vector2(0.35f, 0.04f), new Vector2(0.65f, 0.22f));
        Image lBg = landPanel.AddComponent<Image>();
        lBg.color = new Color(0.03f, 0.08f, 0.14f, 0.90f);
        landPanel.AddComponent<Outline>().effectColor = new Color(0.35f, 0.85f, 1.0f, 0.85f);

        GameObject altGO = CreateUIObject("AltimeterText", landPanel.transform);
        SetRect(altGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.92f));
        TextMeshProUGUI aTMP = altGO.AddComponent<TextMeshProUGUI>();
        if (font != null) aTMP.font = font;
        aTMP.text = "RADAR ALTITUDE: 120.0 M";
        aTMP.fontSize = 15f;
        aTMP.fontStyle = FontStyles.Bold;
        aTMP.alignment = TextAlignmentOptions.Center;
        aTMP.color = Color.white;
        ctrl.radarAltimeterText = aTMP;

        GameObject sinkGO = CreateUIObject("SinkRateText", landPanel.transform);
        SetRect(sinkGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.38f), new Vector2(0.96f, 0.64f));
        TextMeshProUGUI sTMP = sinkGO.AddComponent<TextMeshProUGUI>();
        if (font != null) sTMP.font = font;
        sTMP.text = "SINK RATE: 3.2 M/S [NOMINAL]";
        sTMP.fontSize = 14f;
        sTMP.alignment = TextAlignmentOptions.Center;
        sTMP.color = new Color(0.35f, 1.0f, 0.55f);
        ctrl.sinkRateText = sTMP;

        GameObject glideGO = CreateUIObject("GlideSlopeText", landPanel.transform);
        SetRect(glideGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.34f));
        TextMeshProUGUI gTMP = glideGO.AddComponent<TextMeshProUGUI>();
        if (font != null) gTMP.font = font;
        gTMP.text = "GLIDESLOPE: CENTERLINE ALIGNED";
        gTMP.fontSize = 13f;
        gTMP.alignment = TextAlignmentOptions.Center;
        gTMP.color = new Color(0.35f, 0.9f, 1.0f);
        ctrl.glideSlopeStatusText = gTMP;

        ctrl.landingGuidancePanel = landPanel;
        landPanel.SetActive(false);

        // 3. Touchdown Confirmed Modal
        GameObject sModal = CreateUIObject("TouchdownSuccessModal", canvasTransform);
        StretchFull(sModal.GetComponent<RectTransform>());
        Image smBg = sModal.AddComponent<Image>();
        smBg.color = new Color(0f, 0f, 0f, 0.88f);

        GameObject scCard = CreateUIObject("Card", sModal.transform);
        SetRect(scCard.GetComponent<RectTransform>(), new Vector2(0.28f, 0.25f), new Vector2(0.72f, 0.75f));
        Image scImg = scCard.AddComponent<Image>();
        scImg.color = new Color(0.04f, 0.12f, 0.08f, 0.98f);
        scCard.AddComponent<Outline>().effectColor = new Color(0.35f, 1.0f, 0.55f, 0.95f);

        GameObject scHead = CreateUIObject("Header", scCard.transform);
        SetRect(scHead.GetComponent<RectTransform>(), new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f));
        TextMeshProUGUI shTMP = scHead.AddComponent<TextMeshProUGUI>();
        if (font != null) shTMP.font = font;
        shTMP.text = "[ TOUCHDOWN CONFIRMED // WELCOME HOME ]";
        shTMP.fontSize = 18f;
        shTMP.fontStyle = FontStyles.Bold;
        shTMP.alignment = TextAlignmentOptions.Center;
        shTMP.color = new Color(0.35f, 1.0f, 0.55f);

        GameObject scBody = CreateUIObject("Summary", scCard.transform);
        SetRect(scBody.GetComponent<RectTransform>(), new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.80f));
        TextMeshProUGUI sbTMP = scBody.AddComponent<TextMeshProUGUI>();
        if (font != null) sbTMP.font = font;
        sbTMP.text = "Touchdown telemetry summary.";
        sbTMP.fontSize = 14f;
        sbTMP.alignment = TextAlignmentOptions.Center;
        sbTMP.color = new Color(0.88f, 0.92f, 0.95f);
        ctrl.touchdownSummaryText = sbTMP;

        GameObject procBtn = CreateButton("ProceedButton", "VIEW MISSION RESULTS >", font, scCard.transform,
            new Vector2(0.20f, 0.08f), new Vector2(0.80f, 0.22f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.12f, 0.65f, 0.40f, 0.98f));
        ctrl.proceedToResultsButton = procBtn.GetComponent<Button>();

        ctrl.touchdownSuccessModal = sModal;
        sModal.SetActive(false);

        // 4. Touchdown Failure Modal
        GameObject fModal = CreateUIObject("TouchdownFailureModal", canvasTransform);
        StretchFull(fModal.GetComponent<RectTransform>());
        Image fmBg = fModal.AddComponent<Image>();
        fmBg.color = new Color(0.08f, 0f, 0f, 0.90f);

        GameObject fcCard = CreateUIObject("Card", fModal.transform);
        SetRect(fcCard.GetComponent<RectTransform>(), new Vector2(0.30f, 0.30f), new Vector2(0.70f, 0.70f));
        Image fcImg = fcCard.AddComponent<Image>();
        fcImg.color = new Color(0.14f, 0.04f, 0.04f, 0.98f);
        fcCard.AddComponent<Outline>().effectColor = new Color(1.0f, 0.3f, 0.3f, 0.95f);

        GameObject fcHead = CreateUIObject("Header", fcCard.transform);
        SetRect(fcHead.GetComponent<RectTransform>(), new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f));
        TextMeshProUGUI fhTMP = fcHead.AddComponent<TextMeshProUGUI>();
        if (font != null) fhTMP.font = font;
        fhTMP.text = "[ MISSION FAILURE // IMPACT DETECTED ]";
        fhTMP.fontSize = 17f;
        fhTMP.fontStyle = FontStyles.Bold;
        fhTMP.alignment = TextAlignmentOptions.Center;
        fhTMP.color = new Color(1.0f, 0.3f, 0.3f);

        GameObject fcBody = CreateUIObject("Details", fcCard.transform);
        SetRect(fcBody.GetComponent<RectTransform>(), new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.78f));
        TextMeshProUGUI fbTMP = fcBody.AddComponent<TextMeshProUGUI>();
        if (font != null) fbTMP.font = font;
        fbTMP.text = "Impact reason.";
        fbTMP.fontSize = 14f;
        fbTMP.alignment = TextAlignmentOptions.Center;
        fbTMP.color = new Color(0.95f, 0.85f, 0.85f);
        ctrl.failureDetailsText = fbTMP;

        GameObject retBtn = CreateButton("RetryButton", "RETRY APPROACH", font, fcCard.transform,
            new Vector2(0.10f, 0.10f), new Vector2(0.48f, 0.28f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.75f, 0.25f, 0.20f, 0.98f));
        ctrl.retryApproachButton = retBtn.GetComponent<Button>();

        GameObject exitBtn = CreateButton("ExitButton", "RETURN TO MISSIONS", font, fcCard.transform,
            new Vector2(0.52f, 0.10f), new Vector2(0.90f, 0.28f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.25f, 0.28f, 0.35f, 0.98f));
        ctrl.returnToMissionsButton = exitBtn.GetComponent<Button>();

        ctrl.touchdownFailureModal = fModal;
        fModal.SetActive(false);
    }

    private static void BuildMiniMap(Transform parent, FlightHUDController hud, SpacecraftController ship, RouteManager route, CheckpointManager cp, HazardManager hz)
    {
        GameObject mapRoot = CreateUIObject("MiniMapPanel", parent);
        SetRect(mapRoot.GetComponent<RectTransform>(), new Vector2(0.82f, 0.72f), new Vector2(0.98f, 0.98f));
        Image mBg = mapRoot.AddComponent<Image>();
        mBg.color = new Color(0.03f, 0.06f, 0.10f, 0.92f);
        mapRoot.AddComponent<Outline>().effectColor = new Color(0.25f, 0.85f, 1.0f, 0.85f);

        FlightMiniMap miniMap = mapRoot.AddComponent<FlightMiniMap>();
        miniMap.mapContainer = mapRoot.GetComponent<RectTransform>();
        miniMap.spacecraft = ship;
        miniMap.routeManager = route;
        miniMap.checkpointManager = cp;
        miniMap.hazardManager = hz;
        miniMap.mapWorldRange = 260f;

        // Player Blip
        GameObject pBlip = CreateUIObject("PlayerBlip", mapRoot.transform);
        pBlip.GetComponent<RectTransform>().sizeDelta = new Vector2(14f, 14f);
        Image pbImg = pBlip.AddComponent<Image>();
        pbImg.color = new Color(0.35f, 1.0f, 0.55f);
        miniMap.playerBlip = pBlip.GetComponent<RectTransform>();

        // Checkpoint Template
        GameObject cpBlip = CreateUIObject("CP_Template", mapRoot.transform);
        cpBlip.GetComponent<RectTransform>().sizeDelta = new Vector2(12f, 12f);
        Image cpImg = cpBlip.AddComponent<Image>();
        cpImg.color = new Color(0.25f, 0.85f, 1.0f);
        cpBlip.SetActive(false);
        miniMap.checkpointBlipTemplate = cpBlip.GetComponent<RectTransform>();

        // Hazard Template
        GameObject hzBlip = CreateUIObject("HZ_Template", mapRoot.transform);
        hzBlip.GetComponent<RectTransform>().sizeDelta = new Vector2(28f, 28f);
        Image hzImg = hzBlip.AddComponent<Image>();
        hzImg.color = new Color(1.0f, 0.35f, 0.2f, 0.45f);
        hzBlip.SetActive(false);
        miniMap.hazardBlipTemplate = hzBlip.GetComponent<RectTransform>();

        // Route Dot Template
        GameObject dotBlip = CreateUIObject("Dot_Template", mapRoot.transform);
        dotBlip.GetComponent<RectTransform>().sizeDelta = new Vector2(4f, 4f);
        Image dotImg = dotBlip.AddComponent<Image>();
        dotImg.color = new Color(0.25f, 0.85f, 1.0f, 0.45f);
        dotBlip.SetActive(false);
        miniMap.routeDotTemplate = dotBlip.GetComponent<RectTransform>();
    }

    private static GameObject BuildWarningBanner(string name, Transform parent, TMP_FontAsset font, string title, out TextMeshProUGUI titleTMP, out TextMeshProUGUI timerTMP)
    {
        GameObject banner = CreateUIObject(name, parent);
        SetRect(banner.GetComponent<RectTransform>(), new Vector2(0.30f, 0.80f), new Vector2(0.70f, 0.92f));
        Image bg = banner.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.04f, 0.04f, 0.95f);
        banner.AddComponent<Outline>().effectColor = new Color(1.0f, 0.25f, 0.2f, 0.95f);

        GameObject tGO = CreateUIObject("Title", banner.transform);
        SetRect(tGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.48f), new Vector2(0.96f, 0.92f));
        titleTMP = tGO.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTMP.font = font;
        titleTMP.text = title;
        titleTMP.fontSize = 15f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(1.0f, 0.35f, 0.3f);

        GameObject subGO = CreateUIObject("Timer", banner.transform);
        SetRect(subGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.44f));
        timerTMP = subGO.AddComponent<TextMeshProUGUI>();
        if (font != null) timerTMP.font = font;
        timerTMP.text = "RETURN TO CORRIDOR IN 14s";
        timerTMP.fontSize = 13f;
        timerTMP.alignment = TextAlignmentOptions.Center;
        timerTMP.color = Color.white;

        banner.SetActive(false);
        return banner;
    }

    private static GameObject BuildHazardBanner(string name, Transform parent, TMP_FontAsset font, out TextMeshProUGUI titleTMP)
    {
        GameObject banner = CreateUIObject(name, parent);
        SetRect(banner.GetComponent<RectTransform>(), new Vector2(0.32f, 0.68f), new Vector2(0.68f, 0.77f));
        Image bg = banner.AddComponent<Image>();
        bg.color = new Color(0.20f, 0.08f, 0.02f, 0.92f);
        banner.AddComponent<Outline>().effectColor = new Color(1.0f, 0.6f, 0.2f, 0.95f);

        GameObject tGO = CreateUIObject("Title", banner.transform);
        SetRect(tGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f));
        titleTMP = tGO.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTMP.font = font;
        titleTMP.text = "DANGER // SPACE HAZARD PROXIMITY";
        titleTMP.fontSize = 14f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(1.0f, 0.75f, 0.25f);

        banner.SetActive(false);
        return banner;
    }

    // Helper UI & Material Factories
    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
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

    private static Image CreateBar(string name, Transform parent, Vector2 min, Vector2 max, Color fillColor)
    {
        GameObject bgGO = CreateUIObject(name + "_BG", parent);
        SetRect(bgGO.GetComponent<RectTransform>(), min, max);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.15f, 0.2f, 0.75f);

        GameObject fillGO = CreateUIObject("Fill", bgGO.transform);
        StretchFull(fillGO.GetComponent<RectTransform>());
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = fillColor;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1.0f;

        return fillImg;
    }

    private static GameObject CreateButton(string name, string text, TMP_FontAsset font, Transform parent, Vector2 min, Vector2 max, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax, Color normalColor)
    {
        GameObject btnGO = CreateUIObject(name, parent);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        Image img = btnGO.AddComponent<Image>();
        img.color = normalColor;

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = normalColor;
        cb.highlightedColor = normalColor * 1.2f;
        cb.pressedColor = normalColor * 0.8f;
        btn.colors = cb;

        GameObject txtGO = CreateUIObject("Text", btnGO.transform);
        StretchFull(txtGO.GetComponent<RectTransform>());
        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = 14f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnGO;
    }

    private static Material GetOrCreateLitMaterial(string matName, Color color, float metallic, float smoothness, Texture2D tex)
    {
        string path = MaterialsPath + "/" + matName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        if (tex != null) mat.SetTexture("_BaseMap", tex);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateParticleMaterial(string matName, Color color, bool additive)
    {
        string path = MaterialsPath + "/" + matName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            mat = new Material(shader);
            mat.name = matName;
            AssetDatabase.CreateAsset(mat, path);
        }

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // Transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", additive ? 1 : 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", additive ? (int)UnityEngine.Rendering.BlendMode.One : (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateUnlitMaterial(string matName, Color color)
    {
        string path = MaterialsPath + "/" + matName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    

    private static TMP_FontAsset FindTMPFont()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }
        return null;
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }
    }

    private static void UpdateBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        string[] requiredScenes = new string[]
        {
            ScenesPath + "/OpeningStory_VisualTest.unity",
            ScenesPath + "/MissionSelection.unity",
            ScenesPath + "/MissionPreparation.unity",
            ScenesPath + "/LaunchSequence.unity",
            ScenesPath + "/SpacecraftFlight.unity",
            ScenesPath + "/LunarLanding.unity",
            ScenesPath + "/LunarExploration.unity",
            ScenesPath + "/EarthReturnFlight.unity",
            ScenesPath + "/MissionResults.unity"
        };

        foreach (string req in requiredScenes)
        {
            if (File.Exists(req))
            {
                bool exists = scenes.Exists(s => s.path == req);
                if (!exists)
                {
                    scenes.Add(new EditorBuildSettingsScene(req, true));
                }
            }
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}

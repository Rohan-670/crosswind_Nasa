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
/// Automation builder for the Lunar Exploration surface scene:
/// constructs the lunar regolith terrain, places the touchdown lander,
/// instantiates the authentic 3D NASA EMU astronaut with third-person controls,
/// spawns geological sample objects, scannable anomalies, photo targets, and HUD.
/// </summary>
public class LunarExplorationSceneBuilder
{
    private const string ScenesPath = "Assets/Scenes";
    private const string MaterialsPath = "Assets/Materials";
    private const string PrefabsPath = "Assets/Prefabs/Astronaut";

    [MenuItem("ASTROBASE/Missions/Build Lunar Exploration Scene")]
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
            if (!File.Exists(ScenesPath + "/LunarExploration.unity"))
            {
                Debug.Log("[ASTROBASE] Initializing LunarExploration scene...");
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

            string moonBasePrefabPath = "Assets/Prefabs/Environment/MoonBase_Complex.prefab";
            if (!File.Exists(moonBasePrefabPath))
            {
                MoonBaseIntegrator.IntegrateMoonBaseBatch();
            }

            TMP_FontAsset font = FindTMPFont();

            // Ensure upright astronaut EMU prefab is loaded/updated
            MissionPreparationSceneBuilder.CreateOrUpdateAstronautPrefab();

            BuildExplorationScene(font);
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: Lunar Exploration Scene Built!",
                    "Scene created at:\n  Assets/Scenes/LunarExploration.unity\n\n" +
                    "  ✓ Authentic 3D NASA EMU Spacesuit with Third-Person Controls\n" +
                    "  ✓ Low-Gravity Bounding Jump [Space] & Sprint [Shift]\n" +
                    "  ✓ 3 Collectible Geological Regolith & Volatile Ice Samples [E]\n" +
                    "  ✓ Multispectral Geological Scanner [F / Right-Click]\n" +
                    "  ✓ Scientific Camera Viewfinder & Photo Capture [C]\n" +
                    "  ✓ Dynamic Objective Checklist & Spacesuit Oxygen Meter\n" +
                    "  ✓ Return to Lunar Lander to complete expedition!\n\n" +
                    "Press Play ▶ in LunarExploration to begin surface EVA!",
                    "OK");
            }
            Debug.Log("[ASTROBASE] Lunar Exploration scene built successfully.");
        }
        catch (System.Exception ex)
        {
            if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", ex.Message, "OK");
            Debug.LogError("[ASTROBASE] LunarExploration build failed: " + ex);
        }
    }

    private static void BuildExplorationScene(TMP_FontAsset font)
    {
        string scenePath = ScenesPath + "/LunarExploration.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Lighting & Environment
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.04f, 0.05f, 0.08f);

        GameObject sunGO = new GameObject("Sunlight");
        Light sunLight = sunGO.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1.0f, 0.98f, 0.92f);
        sunLight.intensity = 1.45f;
        sunGO.transform.rotation = Quaternion.Euler(28f, -50f, 0f);

        // Starfield & Earth in sky
        BuildStarfield("DeepSpaceStarfield", 3000, 500f);
        GameObject earthGO = BuildDistantEarth(new Vector3(80f, 160f, 320f), 60f);

        // 2. Terrain & Crater Surface
        GameObject terrainGO = BuildLunarSurfaceTerrain();

        // 3D Authentic NASA Moon Base Complex
        string moonBasePrefabPath = "Assets/Prefabs/Environment/MoonBase_Complex.prefab";
        GameObject moonBasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(moonBasePrefabPath);
        Transform baseTransform = null;
        if (moonBasePrefab != null)
        {
            GameObject baseInstance = (GameObject)PrefabUtility.InstantiatePrefab(moonBasePrefab);
            baseInstance.name = "MoonBase_Complex";
            baseInstance.transform.position = Vector3.zero;
            baseInstance.transform.rotation = Quaternion.identity;
            baseTransform = baseInstance.transform;
        }

        // 3. Parked Lunar Lander Base (at base touchdown pad)
        GameObject landerGO = BuildParkedLander(new Vector3(-14.2f, 0.02f, -7.6f));

        // Lander Return Zone Trigger
        GameObject returnZoneGO = new GameObject("Lander_Return_Zone");
        returnZoneGO.transform.position = new Vector3(-14.2f, 0.5f, -7.6f);

        // 4. Astronaut Player Character (outside NASA Module airlock, facing biodomes)
        GameObject astronautGO = BuildAstronautPlayer(new Vector3(14.3f, 0.15f, -4.5f));
        astronautGO.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        AstronautController astronaut = astronautGO.GetComponent<AstronautController>();
        AstronautScanner scanner = astronautGO.GetComponent<AstronautScanner>();
        PhotoCaptureSystem photoSystem = astronautGO.GetComponent<PhotoCaptureSystem>();
        photoSystem.earthInSkyTarget = earthGO.transform;
        photoSystem.lunarLanderTarget = landerGO.transform;
        if (baseTransform != null)
        {
            Transform domeTarget = baseTransform.Find("Dome_Front_Center_node_8");
            photoSystem.moonBaseHabitatTarget = domeTarget != null ? domeTarget : baseTransform;
        }

        // Third-Person Camera
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 2000f;
        camGO.AddComponent<AudioListener>();

        AstronautCameraController camOrbit = camGO.AddComponent<AstronautCameraController>();
        camOrbit.target = astronautGO.transform;
        camOrbit.targetOffset = new Vector3(0f, 1.5f, 0f);
        astronaut.cameraTransform = camGO.transform;
        photoSystem.photoCamera = cam;

        // 5. Scientific Samples (3 Collectibles distributed around Moon Base sites)
        CreateSample("Sample_01_RegolithCanister", new Vector3(2.5f, 0.4f, 4.0f), "Regolith Core Canister Alpha", new Color(0.35f, 0.90f, 1.0f));
        CreateSample("Sample_02_VolatileIce", new Vector3(-22.0f, 0.4f, 6.0f), "Volatile Ice Specimen Beta", new Color(0.40f, 0.80f, 1.0f));
        CreateSample("Sample_03_BasaltBreccia", new Vector3(24.0f, 0.4f, -12.0f), "Basalt Crust Breccia Gamma", new Color(1.0f, 0.75f, 0.35f));

        // 6. Scannable Anomaly (Crater perimeter fissure)
        CreateScannableAnomaly("Anomaly_SubsurfaceVolatiles", new Vector3(0.0f, 1.0f, 18.0f), "Subsurface Volatiles Fissure");

        // 7. Exploration Manager
        GameObject mgrGO = new GameObject("Exploration_Manager");
        ExplorationManager manager = mgrGO.AddComponent<ExplorationManager>();
        manager.astronaut = astronaut;
        manager.scanner = scanner;
        manager.photoSystem = photoSystem;
        manager.landerReturnZone = returnZoneGO.transform;
        manager.landerReturnRadius = 6.5f;
        manager.landerFX = landerGO.GetComponent<LanderProximityAndAscentFX>();
        if (manager.landerFX != null)
        {
            manager.landerFX.astronautTransform = astronautGO.transform;
            manager.landerFX.explorationManager = manager;
        }
        manager.requiredSampleCount = 3;
        manager.missionDurationSeconds = 480f;

        // 8. Exploration UI Canvas
        EnsureEventSystem();
        GameObject canvasGO = BuildExplorationCanvas(font, manager, scanner, photoSystem, astronaut);

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static GameObject BuildAstronautPlayer(Vector3 startPos)
    {
        GameObject astroRoot = new GameObject("Astronaut_Player");
        astroRoot.transform.position = startPos;

        CharacterController cc = astroRoot.AddComponent<CharacterController>();
        cc.height = 2.0f;
        cc.radius = 0.45f;
        cc.center = new Vector3(0f, 1.0f, 0f);

        // Load 3D NASA EMU Spacesuit Prefab
        string prefabPath = PrefabsPath + "/Astronaut_EMU_Rig.prefab";
        GameObject suitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        GameObject suitInstance;
        if (suitPrefab != null)
        {
            suitInstance = (GameObject)PrefabUtility.InstantiatePrefab(suitPrefab, astroRoot.transform);
            suitInstance.name = "EMU_Spacesuit_Mesh";
            suitInstance.transform.localPosition = Vector3.zero;
            suitInstance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Fallback capsule
            suitInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            suitInstance.name = "Spacesuit_Mesh";
            suitInstance.transform.SetParent(astroRoot.transform, false);
            suitInstance.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            suitInstance.transform.localScale = new Vector3(0.7f, 1.0f, 0.7f);
            Object.DestroyImmediate(suitInstance.GetComponent<Collider>());
            suitInstance.GetComponent<Renderer>().sharedMaterial = GetOrCreateLitMaterial("Fallback_Suit_Mat", new Color(0.9f, 0.9f, 0.92f), 0.1f, 0.35f, null);
        }

        AstronautController controller = astroRoot.AddComponent<AstronautController>();
        controller.spacesuitMeshRoot = suitInstance.transform;
        controller.locomotionAnimator = suitInstance.GetComponent<AstronautLocomotionAnimator>();
        if (controller.locomotionAnimator != null)
        {
            controller.locomotionAnimator.astronautController = controller;
        }
        controller.gravity = 1.62f; // Lunar gravity
        controller.walkSpeed = 3.2f;
        controller.sprintSpeed = 5.4f;
        controller.jumpVelocity = 4.4f;

        // Scanner tool
        AstronautScanner scanner = astroRoot.AddComponent<AstronautScanner>();
        scanner.scanEmitterOrigin = astroRoot.transform;

        // Scanner Beam Renderer
        GameObject beamGO = new GameObject("ScannerBeam");
        beamGO.transform.SetParent(astroRoot.transform, false);
        LineRenderer lr = beamGO.AddComponent<LineRenderer>();
        lr.startWidth = 0.08f;
        lr.endWidth = 0.25f;
        Material beamMat = GetOrCreateUnlitMaterial("Scanner_Beam_Mat", new Color(0.25f, 0.85f, 1.0f, 0.85f));
        lr.sharedMaterial = beamMat;
        lr.enabled = false;
        scanner.scanBeamRenderer = lr;

        // Photo system
        astroRoot.AddComponent<PhotoCaptureSystem>();

        return astroRoot;
    }

    private static GameObject BuildParkedLander(Vector3 pos)
    {
        GameObject landerRoot = new GameObject("Lunar_Lander_Base");
        landerRoot.transform.position = pos;

        Material landerGold = GetOrCreateLitMaterial("Lander_Foil_Gold", new Color(0.95f, 0.75f, 0.20f), 0.9f, 0.85f, null);
        Material landerWhite = GetOrCreateLitMaterial("Lander_Cabin_White", new Color(0.90f, 0.90f, 0.92f), 0.1f, 0.4f, null);
        Material landerDark = GetOrCreateLitMaterial("Lander_Metal_Dark", new Color(0.20f, 0.22f, 0.25f), 0.7f, 0.5f, null);

        // Descent Stage
        GameObject descentStage = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        descentStage.name = "Descent_Stage";
        descentStage.transform.SetParent(landerRoot.transform, false);
        descentStage.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        descentStage.transform.localScale = new Vector3(4.5f, 0.8f, 4.5f);
        descentStage.GetComponent<Renderer>().sharedMaterial = landerGold;

        // Cabin
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Ascent_Cabin";
        cabin.transform.SetParent(landerRoot.transform, false);
        cabin.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        cabin.transform.localScale = new Vector3(3.2f, 2.0f, 3.2f);
        cabin.GetComponent<Renderer>().sharedMaterial = landerWhite;

        // Legs
        Vector3[] legAngles = new Vector3[]
        {
            new Vector3(2.4f, 0.4f, 2.4f),
            new Vector3(-2.4f, 0.4f, 2.4f),
            new Vector3(2.4f, 0.4f, -2.4f),
            new Vector3(-2.4f, 0.4f, -2.4f)
        };

        for (int i = 0; i < legAngles.Length; i++)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = $"Leg_{i + 1}";
            leg.transform.SetParent(landerRoot.transform, false);
            leg.transform.localPosition = legAngles[i];
            leg.transform.localScale = new Vector3(0.18f, 0.9f, 0.18f);
            leg.transform.localRotation = Quaternion.Euler(legAngles[i].z > 0 ? 30f : -30f, 0f, legAngles[i].x > 0 ? -30f : 30f);
            leg.GetComponent<Renderer>().sharedMaterial = landerDark;
        }

        // Ingress Ladder / Platform
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Ingress_Platform";
        platform.transform.SetParent(landerRoot.transform, false);
        platform.transform.localPosition = new Vector3(0f, 0.4f, 2.6f);
        platform.transform.localScale = new Vector3(2.0f, 0.2f, 1.2f);
        platform.GetComponent<Renderer>().sharedMaterial = landerDark;

        // Beacon light above ladder
        GameObject bLight = new GameObject("IngressLight");
        bLight.transform.SetParent(landerRoot.transform, false);
        bLight.transform.localPosition = new Vector3(0f, 3.2f, 1.6f);
        Light bl = bLight.AddComponent<Light>();
        bl.type = LightType.Point;
        bl.color = new Color(0.35f, 1.0f, 0.55f);
        bl.range = 10f;
        bl.intensity = 2.0f;

        // --- Proximity Visual Elements (Active ONLY when astronaut is near) ---
        GameObject proximityRoot = new GameObject("Proximity_VFX_Root");
        proximityRoot.transform.SetParent(landerRoot.transform, false);

        // Ground holographic beacon ring
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Lander_Proximity_Ring";
        ring.transform.SetParent(proximityRoot.transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        ring.transform.localScale = new Vector3(13f, 0.03f, 13f);
        Material ringMat = GetOrCreateParticleMaterial("Lander_Holo_Ring_Mat", new Color(0.35f, 0.95f, 1.0f, 0.75f), true);
        ring.GetComponent<Renderer>().sharedMaterial = ringMat;
        Object.DestroyImmediate(ring.GetComponent<Collider>());

        // Vertical holographic guide beam (marking ingress ladder)
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = "Lander_Ingress_Beam";
        beam.transform.SetParent(proximityRoot.transform, false);
        beam.transform.localPosition = new Vector3(0f, 4.0f, 2.6f);
        beam.transform.localScale = new Vector3(0.5f, 8.0f, 0.5f);
        Material beamMat = GetOrCreateParticleMaterial("Lander_Holo_Beam_Mat", new Color(0.3f, 0.85f, 1.0f, 0.35f), true);
        beam.GetComponent<Renderer>().sharedMaterial = beamMat;
        Object.DestroyImmediate(beam.GetComponent<Collider>());

        // --- Lunar Ascent Liftoff VFX ---
        // 1. Ascent Engine Flame Particle System (under cabin)
        GameObject flameGO = new GameObject("Ascent_Flame_FX");
        flameGO.transform.SetParent(cabin.transform, false);
        flameGO.transform.localPosition = new Vector3(0f, -1.0f, 0f);
        flameGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ParticleSystem flamePS = flameGO.AddComponent<ParticleSystem>();
        var fMain = flamePS.main;
        fMain.loop = true;
        fMain.playOnAwake = false;
        fMain.startLifetime = 0.4f;
        fMain.startSpeed = 16f;
        fMain.startSize = 1.4f;
        fMain.startColor = new Color(1.0f, 0.65f, 0.2f, 0.9f);
        fMain.maxParticles = 150;
        var fEmission = flamePS.emission;
        fEmission.rateOverTime = 80f;
        var fShape = flamePS.shape;
        fShape.shapeType = ParticleSystemShapeType.Cone;
        fShape.angle = 8f;
        fShape.radius = 0.4f;
        var fRenderer = flameGO.GetComponent<ParticleSystemRenderer>();
        fRenderer.sharedMaterial = GetOrCreateParticleMaterial("Ascent_Flame_Mat", new Color(1f, 0.75f, 0.25f, 0.9f), true);

        // 2. Radial Regolith Dust Blast (expands outward across the lunar surface upon liftoff)
        GameObject dustGO = new GameObject("Radial_Regolith_Dust_FX");
        dustGO.transform.SetParent(landerRoot.transform, false);
        dustGO.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        ParticleSystem dustPS = dustGO.AddComponent<ParticleSystem>();
        var dMain = dustPS.main;
        dMain.loop = true;
        dMain.playOnAwake = false;
        dMain.startLifetime = 1.8f;
        dMain.startSpeed = 18f;
        dMain.startSize = 2.2f;
        dMain.startColor = new Color(0.75f, 0.76f, 0.78f, 0.55f);
        dMain.maxParticles = 300;
        var dEmission = dustPS.emission;
        dEmission.rateOverTime = 120f;
        var dShape = dustPS.shape;
        dShape.shapeType = ParticleSystemShapeType.Circle;
        dShape.radius = 2.5f;
        dShape.rotation = new Vector3(90f, 0f, 0f);
        var dRenderer = dustGO.GetComponent<ParticleSystemRenderer>();
        dRenderer.sharedMaterial = GetOrCreateParticleMaterial("Lunar_Regolith_Dust_Mat", new Color(0.72f, 0.74f, 0.76f, 0.45f), false);

        // 3. Engine Ignition Point Light
        GameObject ignLightGO = new GameObject("Engine_Ignition_Light");
        ignLightGO.transform.SetParent(cabin.transform, false);
        ignLightGO.transform.localPosition = new Vector3(0f, -0.8f, 0f);
        Light ignLight = ignLightGO.AddComponent<Light>();
        ignLight.type = LightType.Point;
        ignLight.color = new Color(1.0f, 0.7f, 0.25f);
        ignLight.range = 25f;
        ignLight.intensity = 0f;
        ignLight.enabled = false;

        // 4. Cinematic Ascent Camera
        GameObject ascCamGO = new GameObject("Camera_Ascent_Cinematic");
        ascCamGO.transform.position = pos + new Vector3(16f, 8f, -22f);
        ascCamGO.transform.LookAt(pos + Vector3.up * 4.5f);
        Camera ascCam = ascCamGO.AddComponent<Camera>();
        ascCam.clearFlags = CameraClearFlags.SolidColor;
        ascCam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
        ascCam.nearClipPlane = 0.1f;
        ascCam.farClipPlane = 3000f;
        ascCamGO.SetActive(false);

        // 5. Wire LanderProximityAndAscentFX Component
        LanderProximityAndAscentFX fx = landerRoot.AddComponent<LanderProximityAndAscentFX>();
        fx.proximityVisualRoot = proximityRoot;
        fx.groundRingRenderer = ring.GetComponent<Renderer>();
        fx.verticalGuideBeam = beam.GetComponent<Renderer>();
        fx.ingressBeaconLight = bl;
        fx.ascentCabin = cabin.transform;
        fx.descentStage = descentStage.transform;
        fx.ascentFlameParticles = flamePS;
        fx.radialDustBlastParticles = dustPS;
        fx.engineIgnitionLight = ignLight;
        fx.ascentCinematicCamera = ascCam;
        fx.proximityRadius = 18f;

        proximityRoot.SetActive(false);

        return landerRoot;
    }

    private static void CreateSample(string name, Vector3 pos, string sampleName, Color glowColor)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        // 3D Crystal / Core Canister Mesh
        GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mesh.name = "CanisterMesh";
        mesh.transform.SetParent(go.transform, false);
        mesh.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
        mesh.transform.localRotation = Quaternion.Euler(15f, 20f, 0f);

        Material mat = GetOrCreateUnlitMaterial($"Sample_Glow_{name}", glowColor);
        mesh.GetComponent<Renderer>().sharedMaterial = mat;

        // Glow light
        GameObject lightGO = new GameObject("GlowLight");
        lightGO.transform.SetParent(go.transform, false);
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = glowColor;
        l.range = 5.0f;
        l.intensity = 1.8f;

        SampleObject sample = go.AddComponent<SampleObject>();
        sample.sampleId = name;
        sample.sampleName = sampleName;
        sample.sampleMeshRenderer = mesh.GetComponent<Renderer>();
        sample.sampleGlowLight = l;
        sample.glowColor = glowColor;
        sample.interactionRadius = 3.2f;
    }

    private static void CreateScannableAnomaly(string name, Vector3 pos, string anomalyName)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;

        // Geological Rock Formation with glowing fissures
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rock.name = "RockFormation";
        rock.transform.SetParent(go.transform, false);
        rock.transform.localScale = new Vector3(3.5f, 2.2f, 3.5f);
        rock.transform.localRotation = Quaternion.Euler(12f, 35f, -8f);

        Material rockMat = GetOrCreateLitMaterial("Anomaly_Rock_Mat", new Color(0.30f, 0.28f, 0.35f), 0.3f, 0.4f, null);
        rock.GetComponent<Renderer>().sharedMaterial = rockMat;

        // Fissure glow light
        GameObject lightGO = new GameObject("AnomalyMarkerLight");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.35f, 0.85f, 1.0f);
        l.range = 8.0f;
        l.intensity = 2.2f;

        ScannableAnomaly anomaly = go.AddComponent<ScannableAnomaly>();
        anomaly.anomalyId = name;
        anomaly.anomalyName = anomalyName;
        anomaly.anomalyRenderer = rock.GetComponent<Renderer>();
        anomaly.anomalyMarkerLight = l;
        anomaly.scanRadius = 16f;
        anomaly.scanDurationRequired = 1.8f;
    }

    private static GameObject BuildLunarSurfaceTerrain()
    {
        GameObject terrainRoot = new GameObject("Lunar_Surface_Terrain");
        Material moonMat = GetOrCreateLitMaterial("Lunar_Regolith_Ground_Mat", new Color(0.68f, 0.70f, 0.72f), 0.05f, 0.25f, null);

        // Check for real 3D Petavius Crater prefab / model
        string craterPrefabPath = "Assets/Prefabs/Environment/MoonEnvironment_PetaviusCrater.prefab";
        GameObject craterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(craterPrefabPath);
        bool hasCraterModel = false;

        if (craterPrefab != null)
        {
            GameObject crater = (GameObject)PrefabUtility.InstantiatePrefab(craterPrefab, terrainRoot.transform);
            crater.name = "Petavius_Crater_Model";
            crater.transform.localPosition = Vector3.zero;
            crater.transform.localRotation = Quaternion.identity;
            crater.transform.localScale = Vector3.one;
            hasCraterModel = true;
        }
        else
        {
            // Direct OBJ fallback
            string objPath = "Assets/Art/Environment/PetaviusCrater/Petavius_Crater.obj";
            if (!System.IO.File.Exists(objPath)) objPath = "Assets/Prefabs/Environment/Petavius_Crater.obj";
            GameObject craterObj = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            if (craterObj != null)
            {
                GameObject crater = (GameObject)Object.Instantiate(craterObj, terrainRoot.transform);
                crater.name = "Petavius_Crater_Model";
                crater.transform.localPosition = Vector3.zero;
                crater.transform.localRotation = Quaternion.identity;
                crater.transform.localScale = Vector3.one;

                Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Moon_PetaviusCrater_Mat.mat");
                if (mat != null)
                {
                    foreach (var mr in crater.GetComponentsInChildren<MeshRenderer>()) mr.sharedMaterial = mat;
                }
                foreach (var mf in crater.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh != null)
                    {
                        var mc = mf.gameObject.AddComponent<MeshCollider>();
                        mc.sharedMesh = mf.sharedMesh;
                    }
                }
                hasCraterModel = true;
            }
        }

        // Subsurface ground plane
        GameObject groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundPlane.name = "Ground_Plane";
        groundPlane.transform.SetParent(terrainRoot.transform, false);
        groundPlane.transform.localPosition = new Vector3(0f, hasCraterModel ? -34f : -0.45f, 0f);
        groundPlane.transform.localScale = new Vector3(100f, 1f, 100f);
        groundPlane.GetComponent<Renderer>().sharedMaterial = moonMat;

        // Scattered Boulders
        for (int i = 0; i < 24; i++)
        {
            Vector2 rPos = Random.insideUnitCircle * 80f;
            if (rPos.magnitude < 10f) continue; // Don't block lander center

            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = $"Boulder_{i}";
            rock.transform.SetParent(terrainRoot.transform, false);
            rock.transform.localPosition = new Vector3(rPos.x, Random.Range(0.4f, 1.2f), rPos.y);
            rock.transform.localScale = Vector3.one * Random.Range(1.5f, 4.5f);
            rock.transform.localRotation = Random.rotation;
            rock.GetComponent<Renderer>().sharedMaterial = moonMat;
        }

        // If no real 3D crater model was available, fallback to procedural hills
        if (!hasCraterModel)
        {
            for (int i = 0; i < 12; i++)
            {
                float angle = (i / 12f) * 360f * Mathf.Deg2Rad;
                float dist = Random.Range(180f, 240f);
                Vector3 hillPos = new Vector3(Mathf.Sin(angle) * dist, Random.Range(15f, 40f), Mathf.Cos(angle) * dist);

                GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hill.name = $"CraterRim_{i}";
                hill.transform.SetParent(terrainRoot.transform, false);
                hill.transform.localPosition = hillPos;
                hill.transform.localScale = new Vector3(Random.Range(70f, 120f), Random.Range(35f, 70f), Random.Range(70f, 120f));
                hill.GetComponent<Renderer>().sharedMaterial = moonMat;
            }
        }

        return terrainRoot;
    }

    private static GameObject BuildExplorationCanvas(TMP_FontAsset font, ExplorationManager mgr, AstronautScanner scanner, PhotoCaptureSystem photo, AstronautController astro)
    {
        GameObject canvasGO = new GameObject("ExplorationUI_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        ExplorationHUDController hud = canvasGO.AddComponent<ExplorationHUDController>();
        hud.explorationManager = mgr;
        hud.scanner = scanner;
        hud.photoSystem = photo;
        hud.astronaut = astro;

        // 1. TOP-LEFT: SPACESUIT LIFE SUPPORT & TIMER
        GameObject lifePanel = CreateUIObject("LifeSupportPanel", canvasGO.transform);
        SetRect(lifePanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.80f), new Vector2(0.30f, 0.98f));
        Image lpBg = lifePanel.AddComponent<Image>();
        lpBg.color = new Color(0.04f, 0.07f, 0.12f, 0.88f);
        lifePanel.AddComponent<Outline>().effectColor = new Color(0.25f, 0.85f, 1.0f, 0.8f);

        GameObject lpTitle = CreateUIObject("Title", lifePanel.transform);
        SetRect(lpTitle.GetComponent<RectTransform>(), new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.95f));
        TextMeshProUGUI lptTMP = lpTitle.AddComponent<TextMeshProUGUI>();
        if (font != null) lptTMP.font = font;
        lptTMP.text = "EMU SPACESUIT LIFE SUPPORT // PLSS NOMINAL";
        lptTMP.fontSize = 13f;
        lptTMP.fontStyle = FontStyles.Bold;
        lptTMP.color = new Color(0.35f, 0.9f, 1.0f);

        // Oxygen Text & Bar
        GameObject o2TextGO = CreateUIObject("OxygenText", lifePanel.transform);
        SetRect(o2TextGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.50f), new Vector2(0.45f, 0.74f));
        TextMeshProUGUI o2TMP = o2TextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) o2TMP.font = font;
        o2TMP.text = "OXYGEN: 100%";
        o2TMP.fontSize = 15f;
        o2TMP.fontStyle = FontStyles.Bold;
        o2TMP.color = new Color(0.35f, 0.9f, 1.0f);
        hud.oxygenText = o2TMP;

        GameObject o2BarBg = CreateUIObject("OxygenBarBg", lifePanel.transform);
        SetRect(o2BarBg.GetComponent<RectTransform>(), new Vector2(0.46f, 0.54f), new Vector2(0.96f, 0.70f));
        o2BarBg.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f);
        GameObject o2BarFill = CreateUIObject("Fill", o2BarBg.transform);
        StretchFull(o2BarFill.GetComponent<RectTransform>());
        Image obf = o2BarFill.AddComponent<Image>();
        obf.type = Image.Type.Filled;
        obf.fillMethod = Image.FillMethod.Horizontal;
        obf.fillAmount = 1f;
        obf.color = new Color(0.2f, 0.8f, 1.0f);
        hud.oxygenBar = obf;

        // Timer Text
        GameObject timerGO = CreateUIObject("TimerText", lifePanel.transform);
        SetRect(timerGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.15f), new Vector2(0.96f, 0.44f));
        TextMeshProUGUI tTMP = timerGO.AddComponent<TextMeshProUGUI>();
        if (font != null) tTMP.font = font;
        tTMP.text = "EVA TIMER: 08:00";
        tTMP.fontSize = 15f;
        tTMP.fontStyle = FontStyles.Bold;
        tTMP.color = Color.white;
        hud.missionTimerText = tTMP;

        // 2. TOP-RIGHT: OBJECTIVE CHECKLIST
        GameObject checkPanel = CreateUIObject("ChecklistPanel", canvasGO.transform);
        SetRect(checkPanel.GetComponent<RectTransform>(), new Vector2(0.68f, 0.74f), new Vector2(0.98f, 0.98f));
        Image cpBg = checkPanel.AddComponent<Image>();
        cpBg.color = new Color(0.04f, 0.07f, 0.12f, 0.88f);
        checkPanel.AddComponent<Outline>().effectColor = new Color(0.25f, 0.85f, 1.0f, 0.8f);

        GameObject cpTitle = CreateUIObject("Title", checkPanel.transform);
        SetRect(cpTitle.GetComponent<RectTransform>(), new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.95f));
        TextMeshProUGUI cptTMP = cpTitle.AddComponent<TextMeshProUGUI>();
        if (font != null) cptTMP.font = font;
        cptTMP.text = "SURFACE EVA OBJECTIVES";
        cptTMP.fontSize = 14f;
        cptTMP.fontStyle = FontStyles.Bold;
        cptTMP.color = new Color(1.0f, 0.85f, 0.35f);

        // Checklist items
        hud.samplesObjectiveText = CreateChecklistItem("Item_Samples", checkPanel.transform, new Vector2(0.04f, 0.60f), new Vector2(0.96f, 0.78f), "[  ] Geological Samples: (0/3)", font);
        hud.anomalyObjectiveText = CreateChecklistItem("Item_Anomaly", checkPanel.transform, new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.60f), "[  ] Multispectral Anomaly Scan", font);
        hud.photoObjectiveText = CreateChecklistItem("Item_Photo", checkPanel.transform, new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.42f), "[  ] Photograph Earthrise in Sky", font);
        hud.returnLanderObjectiveText = CreateChecklistItem("Item_Return", checkPanel.transform, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.24f), "[  ] Return to Lunar Lander", font);

        // 3. CENTER: INTERACTION PROMPT
        GameObject promptPanel = CreateUIObject("InteractionPromptPanel", canvasGO.transform);
        SetRect(promptPanel.GetComponent<RectTransform>(), new Vector2(0.35f, 0.28f), new Vector2(0.65f, 0.36f));
        Image ppBg = promptPanel.AddComponent<Image>();
        ppBg.color = new Color(0.05f, 0.10f, 0.18f, 0.90f);
        promptPanel.AddComponent<Outline>().effectColor = new Color(0.3f, 0.9f, 1.0f, 0.85f);

        GameObject pText = CreateUIObject("Text", promptPanel.transform);
        StretchFull(pText.GetComponent<RectTransform>());
        TextMeshProUGUI ptTMP = pText.AddComponent<TextMeshProUGUI>();
        if (font != null) ptTMP.font = font;
        ptTMP.text = "[E] COLLECT SAMPLE";
        ptTMP.fontSize = 16f;
        ptTMP.fontStyle = FontStyles.Bold;
        ptTMP.alignment = TextAlignmentOptions.Center;
        ptTMP.color = new Color(0.35f, 1f, 0.55f);
        hud.interactionPromptText = ptTMP;
        hud.interactionPromptPanel = promptPanel;

        // 4. SCANNER RETICLE OVERLAY
        GameObject scanGO = CreateUIObject("ScanOverlay", canvasGO.transform);
        StretchFull(scanGO.GetComponent<RectTransform>());

        GameObject scanRingBg = CreateUIObject("RingBg", scanGO.transform);
        SetRect(scanRingBg.GetComponent<RectTransform>(), new Vector2(0.45f, 0.42f), new Vector2(0.55f, 0.58f));
        Image srBg = scanRingBg.AddComponent<Image>();
        srBg.color = new Color(0.2f, 0.4f, 0.6f, 0.4f);

        GameObject scanRingFill = CreateUIObject("RingFill", scanRingBg.transform);
        StretchFull(scanRingFill.GetComponent<RectTransform>());
        Image srf = scanRingFill.AddComponent<Image>();
        srf.type = Image.Type.Filled;
        srf.fillMethod = Image.FillMethod.Radial360;
        srf.fillAmount = 0f;
        srf.color = new Color(0.35f, 1.0f, 0.55f);
        hud.scanProgressRing = srf;

        GameObject scanTag = CreateUIObject("TargetTag", scanGO.transform);
        SetRect(scanTag.GetComponent<RectTransform>(), new Vector2(0.30f, 0.34f), new Vector2(0.70f, 0.40f));
        TextMeshProUGUI stgTMP = scanTag.AddComponent<TextMeshProUGUI>();
        if (font != null) stgTMP.font = font;
        stgTMP.text = "ANALYZING TARGET...";
        stgTMP.fontSize = 15f;
        stgTMP.fontStyle = FontStyles.Bold;
        stgTMP.alignment = TextAlignmentOptions.Center;
        stgTMP.color = new Color(0.35f, 0.9f, 1f);
        hud.scanTargetText = stgTMP;

        GameObject scanAnalysis = CreateUIObject("AnalysisText", scanGO.transform);
        SetRect(scanAnalysis.GetComponent<RectTransform>(), new Vector2(0.25f, 0.22f), new Vector2(0.75f, 0.33f));
        TextMeshProUGUI saTMP = scanAnalysis.AddComponent<TextMeshProUGUI>();
        if (font != null) saTMP.font = font;
        saTMP.text = "HOLD [F] TO SCAN";
        saTMP.fontSize = 13f;
        saTMP.alignment = TextAlignmentOptions.Center;
        saTMP.color = Color.white;
        hud.scanAnalysisText = saTMP;
        hud.scanOverlay = scanGO;

        // 5. PHOTO VIEWFINDER OVERLAY
        GameObject photoGO = CreateUIObject("PhotoViewfinderOverlay", canvasGO.transform);
        StretchFull(photoGO.GetComponent<RectTransform>());

        // Viewfinder borders
        GameObject frameBorder = CreateUIObject("Border", photoGO.transform);
        SetRect(frameBorder.GetComponent<RectTransform>(), new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
        frameBorder.AddComponent<Outline>().effectColor = new Color(0.35f, 0.9f, 1.0f, 0.7f);

        GameObject photoTag = CreateUIObject("SubjectTag", photoGO.transform);
        SetRect(photoTag.GetComponent<RectTransform>(), new Vector2(0.30f, 0.89f), new Vector2(0.70f, 0.95f));
        TextMeshProUGUI pttTMP = photoTag.AddComponent<TextMeshProUGUI>();
        if (font != null) pttTMP.font = font;
        pttTMP.text = "PHOTO MODE // AIM AT EARTH IN SKY OR LANDER [SPACE TO CAPTURE]";
        pttTMP.fontSize = 14f;
        pttTMP.fontStyle = FontStyles.Bold;
        pttTMP.alignment = TextAlignmentOptions.Center;
        pttTMP.color = new Color(1.0f, 0.85f, 0.35f);
        hud.photoTargetTagText = pttTMP;

        // Flash image
        GameObject flashGO = CreateUIObject("ShutterFlash", photoGO.transform);
        StretchFull(flashGO.GetComponent<RectTransform>());
        Image fImg = flashGO.AddComponent<Image>();
        fImg.color = new Color(1f, 1f, 1f, 0f);
        flashGO.SetActive(false);
        hud.photoShutterFlash = fImg;
        hud.photoViewfinderOverlay = photoGO;

        // 6. BOTTOM CONTROLS HINT
        GameObject hintGO = CreateUIObject("BottomHint", canvasGO.transform);
        SetRect(hintGO.GetComponent<RectTransform>(), new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.07f));
        Image hBg = hintGO.AddComponent<Image>();
        hBg.color = new Color(0.04f, 0.07f, 0.12f, 0.85f);
        hintGO.AddComponent<Outline>().effectColor = new Color(0.2f, 0.5f, 0.8f, 0.5f);

        GameObject htGO = CreateUIObject("Text", hintGO.transform);
        StretchFull(htGO.GetComponent<RectTransform>());
        TextMeshProUGUI htTMP = htGO.AddComponent<TextMeshProUGUI>();
        if (font != null) htTMP.font = font;
        htTMP.text = "CONTROLS: [W/A/S/D] Walk | [SPACE] Low-G Jump | [SHIFT] Sprint | [E] Collect | [F] Scanner | [C] Photo Mode";
        htTMP.fontSize = 13f;
        htTMP.alignment = TextAlignmentOptions.Center;
        htTMP.color = new Color(0.75f, 0.90f, 1.0f);

        // 7. MODALS (SUCCESS & FAILURE)
        hud.missionSuccessModal = BuildSuccessModal(canvasGO.transform, font, hud);
        hud.missionFailedModal = BuildFailureModal(canvasGO.transform, font, hud);

        return canvasGO;
    }

    private static TextMeshProUGUI CreateChecklistItem(string name, Transform parent, Vector2 min, Vector2 max, string text, TMP_FontAsset font)
    {
        GameObject go = CreateUIObject(name, parent);
        SetRect(go.GetComponent<RectTransform>(), min, max);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = 12f;
        tmp.color = new Color(0.85f, 0.92f, 0.98f);
        return tmp;
    }

    private static GameObject BuildSuccessModal(Transform parent, TMP_FontAsset font, ExplorationHUDController hud)
    {
        GameObject modalGO = CreateUIObject("MissionSuccessModal", parent);
        StretchFull(modalGO.GetComponent<RectTransform>());

        Image backdrop = modalGO.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject card = CreateUIObject("Card", modalGO.transform);
        SetRect(card.GetComponent<RectTransform>(), new Vector2(0.28f, 0.28f), new Vector2(0.72f, 0.72f));
        Image cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.04f, 0.12f, 0.08f, 0.98f);
        card.AddComponent<Outline>().effectColor = new Color(0.30f, 1.0f, 0.55f, 0.95f);

        GameObject head = CreateUIObject("Header", card.transform);
        SetRect(head.GetComponent<RectTransform>(), new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.95f));
        TextMeshProUGUI hTMP = head.AddComponent<TextMeshProUGUI>();
        if (font != null) hTMP.font = font;
        hTMP.text = "[ LUNAR SURFACE EXPEDITION ACCOMPLISHED ]";
        hTMP.fontSize = 17f;
        hTMP.fontStyle = FontStyles.Bold;
        hTMP.alignment = TextAlignmentOptions.Center;
        hTMP.color = new Color(0.35f, 1.0f, 0.55f);

        GameObject body = CreateUIObject("Stats", card.transform);
        SetRect(body.GetComponent<RectTransform>(), new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.80f));
        TextMeshProUGUI bTMP = body.AddComponent<TextMeshProUGUI>();
        if (font != null) bTMP.font = font;
        bTMP.text = "Expedition stats.";
        bTMP.fontSize = 14f;
        bTMP.alignment = TextAlignmentOptions.Center;
        bTMP.color = new Color(0.88f, 0.92f, 0.95f);
        hud.successStatsText = bTMP;

        GameObject btn = CreateButton("ContinueButton", "BOARD SPACECRAFT // INITIATE RETURN FLIGHT >", font, card.transform,
            new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.24f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.12f, 0.65f, 0.40f, 0.98f));
        hud.successContinueButton = btn.GetComponent<Button>();

        modalGO.SetActive(false);
        return modalGO;
    }

    private static GameObject BuildFailureModal(Transform parent, TMP_FontAsset font, ExplorationHUDController hud)
    {
        GameObject modalGO = CreateUIObject("MissionFailedModal", parent);
        StretchFull(modalGO.GetComponent<RectTransform>());

        Image backdrop = modalGO.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject card = CreateUIObject("Card", modalGO.transform);
        SetRect(card.GetComponent<RectTransform>(), new Vector2(0.30f, 0.32f), new Vector2(0.70f, 0.68f));
        Image cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.14f, 0.05f, 0.05f, 0.98f);
        card.AddComponent<Outline>().effectColor = new Color(1.0f, 0.25f, 0.25f, 1.0f);

        GameObject head = CreateUIObject("Header", card.transform);
        SetRect(head.GetComponent<RectTransform>(), new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f));
        TextMeshProUGUI hTMP = head.AddComponent<TextMeshProUGUI>();
        if (font != null) hTMP.font = font;
        hTMP.text = "[ LIFE SUPPORT DEPLETED // EVA ABORT ]";
        hTMP.fontSize = 17f;
        hTMP.fontStyle = FontStyles.Bold;
        hTMP.alignment = TextAlignmentOptions.Center;
        hTMP.color = new Color(1.0f, 0.35f, 0.35f);

        GameObject body = CreateUIObject("Reason", card.transform);
        SetRect(body.GetComponent<RectTransform>(), new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.75f));
        TextMeshProUGUI bTMP = body.AddComponent<TextMeshProUGUI>();
        if (font != null) bTMP.font = font;
        bTMP.text = "Oxygen exhausted.";
        bTMP.fontSize = 15f;
        bTMP.alignment = TextAlignmentOptions.Center;
        bTMP.color = new Color(0.88f, 0.88f, 0.92f);
        hud.failureReasonText = bTMP;

        GameObject btnRetry = CreateButton("RetryButton", "RETRY SURFACE EVA", font, card.transform,
            new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.26f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.15f, 0.52f, 0.88f, 0.95f));
        hud.failureRetryButton = btnRetry.GetComponent<Button>();

        GameObject btnMenu = CreateButton("MenuButton", "RETURN TO MISSIONS", font, card.transform,
            new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.26f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.25f, 0.28f, 0.35f, 0.95f));
        hud.failureReturnButton = btnMenu.GetComponent<Button>();

        modalGO.SetActive(false);
        return modalGO;
    }

    private static GameObject BuildDistantEarth(Vector3 position, float scale)
    {
        string earthPrefabPath = "Assets/Prefabs/Planets/Earth_NASA_Realistic.prefab";
        GameObject earthPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(earthPrefabPath);

        GameObject earthGO;
        if (earthPrefab != null)
        {
            earthGO = (GameObject)PrefabUtility.InstantiatePrefab(earthPrefab);
            earthGO.name = "Earth_Sky";
        }
        else
        {
            earthGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earthGO.name = "Earth_Sky";
            Material earthMat = GetOrCreateLitMaterial("Earth_Sky_Mat", new Color(0.18f, 0.45f, 0.85f), 0.1f, 0.4f, null);
            earthGO.GetComponent<Renderer>().sharedMaterial = earthMat;
            Object.DestroyImmediate(earthGO.GetComponent<Collider>());
        }

        earthGO.transform.position = position;
        earthGO.transform.localScale = Vector3.one * scale;
        earthGO.transform.rotation = Quaternion.Euler(20f, 65f, 0f);
        return earthGO;
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
    // UI & MATERIAL HELPERS
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
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            mat = new Material(unlitShader);
            mat.name = matName;
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", color);
        mat.color = color;

        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
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

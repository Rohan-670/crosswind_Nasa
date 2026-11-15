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
/// Automation builder for the Lunar Landing scene:
/// constructs the Moon approach environment, cratered surface,
/// playable Lunar Lander with descent counter-thrust and RCS,
/// landing radar HUD, and safe touchdown detection.
/// </summary>
public class LunarLandingSceneBuilder
{
    private const string ScenesPath = "Assets/Scenes";
    private const string MaterialsPath = "Assets/Materials";

    [MenuItem("ASTROBASE/Missions/Build Lunar Landing Scene")]
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
            if (!File.Exists(ScenesPath + "/LunarLanding.unity"))
            {
                Debug.Log("[ASTROBASE] Initializing LunarLanding scene...");
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

            string moonBasePrefabPath = "Assets/Prefabs/Environment/MoonBase_Complex.prefab";
            if (!File.Exists(moonBasePrefabPath))
            {
                MoonBaseIntegrator.IntegrateMoonBaseBatch();
            }

            TMP_FontAsset font = FindTMPFont();

            BuildLandingScene(font);
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: Lunar Landing Scene Built!",
                    "Scene created at:\n  Assets/Scenes/LunarLanding.unity\n\n" +
                    "  ✓ Approaching Moon surface with crater terrain\n" +
                    "  ✓ Playable Lunar Lander with counter-thrust [Space/W] and RCS [A/D/S]\n" +
                    "  ✓ Radar Altimeter & Vertical Speed Indicator (VSI)\n" +
                    "  ✓ Safe Touchdown evaluation (< 4.0 m/s)\n" +
                    "  ✓ Automatic transition to LunarExploration upon landing!\n\n" +
                    "Press Play ▶ in LunarLanding to test descent controls!",
                    "OK");
            }
            Debug.Log("[ASTROBASE] Lunar Landing scene built successfully.");
        }
        catch (System.Exception ex)
        {
            if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", ex.Message, "OK");
            Debug.LogError("[ASTROBASE] LunarLanding build failed: " + ex);
        }
    }

    private static void BuildLandingScene(TMP_FontAsset font)
    {
        string scenePath = ScenesPath + "/LunarLanding.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Lighting & Environment
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.04f, 0.05f, 0.08f);

        GameObject sunGO = new GameObject("Sunlight");
        Light sunLight = sunGO.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1.0f, 0.98f, 0.92f);
        sunLight.intensity = 1.4f;
        sunGO.transform.rotation = Quaternion.Euler(30f, -45f, 0f);

        // Deep Starfield Background
        BuildStarfield("DeepSpaceStarfield", 3000, 500f);

        // Earth in black sky
        BuildDistantEarth(new Vector3(120f, 180f, 350f), 55f);

        // 2. Lunar Terrain Surface Below
        GameObject terrainGO = BuildLunarSurfaceTerrain();

        // 3D Authentic NASA Moon Base Complex
        string moonBasePrefabPath = "Assets/Prefabs/Environment/MoonBase_Complex.prefab";
        GameObject moonBasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(moonBasePrefabPath);
        if (moonBasePrefab != null)
        {
            GameObject baseInstance = (GameObject)PrefabUtility.InstantiatePrefab(moonBasePrefab);
            baseInstance.name = "MoonBase_Complex";
            baseInstance.transform.position = Vector3.zero;
            baseInstance.transform.rotation = Quaternion.identity;
        }

        // Landing Target Pad (at base landing sector)
        BuildLandingPad(new Vector3(-14.2f, 0.05f, -7.6f));

        // 3. Lunar Lander Player (descending directly towards base landing sector)
        GameObject landerGO = BuildLunarLander(new Vector3(-14.2f, 280f, -7.6f));
        LunarLanderController lander = landerGO.GetComponent<LunarLanderController>();

        // 4. Cockpit Descent Camera
        GameObject camGO = new GameObject("Lander_Descent_Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(landerGO.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 1.2f, 1.4f);
        camGO.transform.localRotation = Quaternion.Euler(25f, 0f, 0f); // Looking forward-downward at terrain

        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 2000f;
        camGO.AddComponent<AudioListener>();

        // 5. Landing UI Canvas
        EnsureEventSystem();
        GameObject canvasGO = BuildLandingCanvas(font, lander);

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static GameObject BuildLunarLander(Vector3 startPos)
    {
        GameObject landerRoot = new GameObject("Lunar_Lander");
        landerRoot.transform.position = startPos;

        Material landerGold = GetOrCreateLitMaterial("Lander_Foil_Gold", new Color(0.95f, 0.75f, 0.20f), 0.9f, 0.85f, null);
        Material landerWhite = GetOrCreateLitMaterial("Lander_Cabin_White", new Color(0.90f, 0.90f, 0.92f), 0.1f, 0.4f, null);
        Material landerDark = GetOrCreateLitMaterial("Lander_Metal_Dark", new Color(0.20f, 0.22f, 0.25f), 0.7f, 0.5f, null);

        // 1. Octagonal Descent Stage (Foil covered)
        GameObject descentStage = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        descentStage.name = "Descent_Stage";
        descentStage.transform.SetParent(landerRoot.transform, false);
        descentStage.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        descentStage.transform.localScale = new Vector3(4.5f, 0.8f, 4.5f);
        descentStage.GetComponent<Renderer>().sharedMaterial = landerGold;
        Object.DestroyImmediate(descentStage.GetComponent<Collider>());

        // 2. Ascent Cabin Stage
        GameObject ascentStage = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ascentStage.name = "Ascent_Cabin";
        ascentStage.transform.SetParent(landerRoot.transform, false);
        ascentStage.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        ascentStage.transform.localScale = new Vector3(3.2f, 2.0f, 3.2f);
        ascentStage.GetComponent<Renderer>().sharedMaterial = landerWhite;
        Object.DestroyImmediate(ascentStage.GetComponent<Collider>());

        // Triangular forward windows
        GameObject windowL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windowL.name = "Viewport_Port";
        windowL.transform.SetParent(ascentStage.transform, false);
        windowL.transform.localPosition = new Vector3(-0.25f, 0.15f, 0.51f);
        windowL.transform.localScale = new Vector3(0.35f, 0.35f, 0.05f);
        windowL.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
        windowL.GetComponent<Renderer>().sharedMaterial = landerDark;
        Object.DestroyImmediate(windowL.GetComponent<Collider>());

        GameObject windowR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windowR.name = "Viewport_Starboard";
        windowR.transform.SetParent(ascentStage.transform, false);
        windowR.transform.localPosition = new Vector3(0.25f, 0.15f, 0.51f);
        windowR.transform.localScale = new Vector3(0.35f, 0.35f, 0.05f);
        windowR.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
        windowR.GetComponent<Renderer>().sharedMaterial = landerDark;
        Object.DestroyImmediate(windowR.GetComponent<Collider>());

        // 3. Landing Legs (4 Corner Pods)
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
            Object.DestroyImmediate(leg.GetComponent<Collider>());

            // Footpad
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "Footpad";
            pad.transform.SetParent(leg.transform, false);
            pad.transform.localPosition = new Vector3(0f, -1.0f, 0f);
            pad.transform.localScale = new Vector3(4.0f, 0.15f, 4.0f);
            pad.GetComponent<Renderer>().sharedMaterial = landerGold;
            Object.DestroyImmediate(pad.GetComponent<Collider>());
        }

        // 4. Main Descent Engine Nozzle
        GameObject engineNozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        engineNozzle.name = "Main_Descent_Engine";
        engineNozzle.transform.SetParent(landerRoot.transform, false);
        engineNozzle.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        engineNozzle.transform.localScale = new Vector3(1.2f, 0.6f, 1.2f);
        engineNozzle.GetComponent<Renderer>().sharedMaterial = landerDark;
        Object.DestroyImmediate(engineNozzle.GetComponent<Collider>());

        // Engine Plume & Point Light
        GameObject lightGO = new GameObject("EngineLight");
        lightGO.transform.SetParent(engineNozzle.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, -0.6f, 0f);
        Light el = lightGO.AddComponent<Light>();
        el.type = LightType.Point;
        el.color = new Color(0.35f, 0.85f, 1.0f);
        el.range = 14f;
        el.intensity = 0f;
        el.enabled = false;

        GameObject plume = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        plume.name = "EnginePlume";
        plume.transform.SetParent(engineNozzle.transform, false);
        plume.transform.localPosition = new Vector3(0f, -1.4f, 0f);
        plume.transform.localScale = new Vector3(0.9f, 1.6f, 0.9f);
        plume.GetComponent<Renderer>().sharedMaterial = GetOrCreateUnlitMaterial("Lander_Plume_Mat", new Color(0.35f, 0.85f, 1.0f, 0.85f));
        Object.DestroyImmediate(plume.GetComponent<Collider>());
        plume.SetActive(false);

        // Ground Contact Raycast Origin
        GameObject rayOrigin = new GameObject("GroundContactOrigin");
        rayOrigin.transform.SetParent(landerRoot.transform, false);
        rayOrigin.transform.localPosition = Vector3.zero;

        // Controller
        LunarLanderController controller = landerRoot.AddComponent<LunarLanderController>();
        controller.groundContactRaycastOrigin = rayOrigin.transform;
        controller.mainEngineLights = new Light[] { el };
        controller.mainEnginePlumes = new GameObject[] { plume };
        controller.initialAltitude = 280f;

        ProceduralEngineAudio audio = landerRoot.AddComponent<ProceduralEngineAudio>();
        audio.masterVolume = 0.75f;
        controller.engineAudio = audio;

        return landerRoot;
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

        // Subsurface foundation plane
        GameObject groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        groundPlane.name = "Ground_Plane";
        groundPlane.transform.SetParent(terrainRoot.transform, false);
        groundPlane.transform.localPosition = new Vector3(0f, hasCraterModel ? -34f : -0.45f, 0f);
        groundPlane.transform.localScale = new Vector3(100f, 1f, 100f);
        groundPlane.GetComponent<Renderer>().sharedMaterial = moonMat;

        // If no real 3D crater model was available, fallback to procedural hills
        if (!hasCraterModel)
        {
            for (int i = 0; i < 16; i++)
            {
                float angle = (i / 16f) * 360f * Mathf.Deg2Rad;
                float dist = Random.Range(180f, 260f);
                Vector3 hillPos = new Vector3(Mathf.Sin(angle) * dist, Random.Range(15f, 45f), Mathf.Cos(angle) * dist);

                GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hill.name = $"CraterRim_{i}";
                hill.transform.SetParent(terrainRoot.transform, false);
                hill.transform.localPosition = hillPos;
                hill.transform.localScale = new Vector3(Random.Range(80f, 140f), Random.Range(40f, 90f), Random.Range(80f, 140f));
                hill.GetComponent<Renderer>().sharedMaterial = moonMat;
            }
        }

        return terrainRoot;
    }

    private static void BuildLandingPad(Vector3 pos)
    {
        GameObject padGO = new GameObject("Touchdown_Pad");
        padGO.transform.position = pos;

        Material padMat = GetOrCreateLitMaterial("Touchdown_Pad_Mat", new Color(0.25f, 0.28f, 0.35f), 0.5f, 0.6f, null);
        Material stripeMat = GetOrCreateUnlitMaterial("Touchdown_Target_Stripe", new Color(1.0f, 0.85f, 0.25f, 0.9f));

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "TargetCircle";
        ring.transform.SetParent(padGO.transform, false);
        ring.transform.localScale = new Vector3(25f, 0.1f, 25f);
        ring.GetComponent<Renderer>().sharedMaterial = padMat;

        // Target crosshair
        GameObject crossX = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crossX.name = "CrossX";
        crossX.transform.SetParent(padGO.transform, false);
        crossX.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        crossX.transform.localScale = new Vector3(22f, 0.05f, 1.2f);
        crossX.GetComponent<Renderer>().sharedMaterial = stripeMat;
        Object.DestroyImmediate(crossX.GetComponent<Collider>());

        GameObject crossZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crossZ.name = "CrossZ";
        crossZ.transform.SetParent(padGO.transform, false);
        crossZ.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        crossZ.transform.localScale = new Vector3(1.2f, 0.05f, 22f);
        crossZ.GetComponent<Renderer>().sharedMaterial = stripeMat;
        Object.DestroyImmediate(crossZ.GetComponent<Collider>());
    }

    private static GameObject BuildLandingCanvas(TMP_FontAsset font, LunarLanderController lander)
    {
        GameObject canvasGO = new GameObject("LandingUI_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        LunarLandingUI ui = canvasGO.AddComponent<LunarLandingUI>();
        ui.lander = lander;

        // Top Radar Telemetry Panel
        GameObject telemetryPanel = CreateUIObject("TelemetryPanel", canvasGO.transform);
        SetRect(telemetryPanel.GetComponent<RectTransform>(), new Vector2(0.03f, 0.74f), new Vector2(0.38f, 0.97f));
        Image tpBg = telemetryPanel.AddComponent<Image>();
        tpBg.color = new Color(0.04f, 0.07f, 0.12f, 0.90f);
        telemetryPanel.AddComponent<Outline>().effectColor = new Color(0.25f, 0.85f, 1.0f, 0.8f);

        // Header
        GameObject head = CreateUIObject("Header", telemetryPanel.transform);
        SetRect(head.GetComponent<RectTransform>(), new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.96f));
        TextMeshProUGUI hTMP = head.AddComponent<TextMeshProUGUI>();
        if (font != null) hTMP.font = font;
        hTMP.text = "LUNAR DESCENT RADAR // TERMINAL APPROACH";
        hTMP.fontSize = 15f;
        hTMP.fontStyle = FontStyles.Bold;
        hTMP.color = new Color(0.35f, 0.9f, 1.0f);

        // Altitude
        GameObject altGO = CreateUIObject("AltitudeText", telemetryPanel.transform);
        SetRect(altGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.64f), new Vector2(0.96f, 0.80f));
        TextMeshProUGUI aTMP = altGO.AddComponent<TextMeshProUGUI>();
        if (font != null) aTMP.font = font;
        aTMP.text = "RADAR ALTITUDE: 280.0 M";
        aTMP.fontSize = 16f;
        aTMP.fontStyle = FontStyles.Bold;
        aTMP.color = Color.white;
        ui.altitudeText = aTMP;

        // Vertical Speed & Bar
        GameObject vSpeedGO = CreateUIObject("VerticalSpeedText", telemetryPanel.transform);
        SetRect(vSpeedGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.46f), new Vector2(0.60f, 0.62f));
        TextMeshProUGUI vsTMP = vSpeedGO.AddComponent<TextMeshProUGUI>();
        if (font != null) vsTMP.font = font;
        vsTMP.text = "VERTICAL SPEED: ▼ 8.0 M/S";
        vsTMP.fontSize = 14f;
        vsTMP.color = new Color(0.35f, 1f, 0.55f);
        ui.verticalSpeedText = vsTMP;

        GameObject vsiBarBg = CreateUIObject("VSIBarBg", telemetryPanel.transform);
        SetRect(vsiBarBg.GetComponent<RectTransform>(), new Vector2(0.62f, 0.48f), new Vector2(0.96f, 0.60f));
        vsiBarBg.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f);
        GameObject vsiFill = CreateUIObject("Fill", vsiBarBg.transform);
        StretchFull(vsiFill.GetComponent<RectTransform>());
        Image vf = vsiFill.AddComponent<Image>();
        vf.type = Image.Type.Filled;
        vf.fillMethod = Image.FillMethod.Horizontal;
        vf.fillAmount = 0.5f;
        vf.color = new Color(0.3f, 0.9f, 0.5f);
        ui.verticalSpeedIndicatorBar = vf;

        // Horizontal Drift
        GameObject driftGO = CreateUIObject("HorizontalDriftText", telemetryPanel.transform);
        SetRect(driftGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.30f), new Vector2(0.96f, 0.44f));
        TextMeshProUGUI dTMP = driftGO.AddComponent<TextMeshProUGUI>();
        if (font != null) dTMP.font = font;
        dTMP.text = "HORIZONTAL DRIFT: 0.5 M/S";
        dTMP.fontSize = 13f;
        dTMP.color = new Color(0.75f, 0.88f, 1.0f);
        ui.horizontalDriftText = dTMP;

        // Fuel & Throttle
        GameObject fTextGO = CreateUIObject("FuelText", telemetryPanel.transform);
        SetRect(fTextGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.12f), new Vector2(0.40f, 0.26f));
        TextMeshProUGUI fTMP = fTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) fTMP.font = font;
        fTMP.text = "DESCENT FUEL: 100%";
        fTMP.fontSize = 12f;
        fTMP.color = new Color(0.35f, 0.9f, 1f);
        ui.fuelText = fTMP;

        GameObject fBarBg = CreateUIObject("FuelBarBg", telemetryPanel.transform);
        SetRect(fBarBg.GetComponent<RectTransform>(), new Vector2(0.42f, 0.14f), new Vector2(0.96f, 0.24f));
        fBarBg.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f);
        GameObject fFill = CreateUIObject("Fill", fBarBg.transform);
        StretchFull(fFill.GetComponent<RectTransform>());
        Image ff = fFill.AddComponent<Image>();
        ff.type = Image.Type.Filled;
        ff.fillMethod = Image.FillMethod.Horizontal;
        ff.fillAmount = 1f;
        ff.color = new Color(0.2f, 0.7f, 1.0f);
        ui.fuelBar = ff;

        // Landing Status Badge (Top-Center)
        GameObject badgeGO = CreateUIObject("LandingStatusBadge", canvasGO.transform);
        SetRect(badgeGO.GetComponent<RectTransform>(), new Vector2(0.32f, 0.90f), new Vector2(0.68f, 0.97f));
        Image bBg = badgeGO.AddComponent<Image>();
        bBg.color = new Color(0.04f, 0.12f, 0.22f, 0.85f);
        badgeGO.AddComponent<Outline>().effectColor = new Color(0.25f, 0.75f, 1.0f, 0.6f);
        ui.landingStatusBg = bBg;

        GameObject bText = CreateUIObject("Text", badgeGO.transform);
        StretchFull(bText.GetComponent<RectTransform>());
        TextMeshProUGUI btTMP = bText.AddComponent<TextMeshProUGUI>();
        if (font != null) btTMP.font = font;
        btTMP.text = "[ DESCENT IN PROGRESS // RADAR LOCKED ]";
        btTMP.fontSize = 14f;
        btTMP.fontStyle = FontStyles.Bold;
        btTMP.alignment = TextAlignmentOptions.Center;
        btTMP.color = new Color(0.35f, 0.85f, 1.0f);
        ui.landingStatusBadge = btTMP;

        // Bottom Controls Hint
        GameObject hintGO = CreateUIObject("ControlsHint", canvasGO.transform);
        SetRect(hintGO.GetComponent<RectTransform>(), new Vector2(0.20f, 0.03f), new Vector2(0.80f, 0.08f));
        Image hintBg = hintGO.AddComponent<Image>();
        hintBg.color = new Color(0.04f, 0.07f, 0.12f, 0.85f);
        hintGO.AddComponent<Outline>().effectColor = new Color(0.2f, 0.5f, 0.8f, 0.5f);

        GameObject htGO = CreateUIObject("Text", hintGO.transform);
        StretchFull(htGO.GetComponent<RectTransform>());
        TextMeshProUGUI htTMP = htGO.AddComponent<TextMeshProUGUI>();
        if (font != null) htTMP.font = font;
        htTMP.text = "DESCENT CONTROLS: [SPACE / W] Main Counter-Thrust | [A / D] Lateral RCS | [SHIFT] Full Burn";
        htTMP.fontSize = 13f;
        htTMP.alignment = TextAlignmentOptions.Center;
        htTMP.color = new Color(0.75f, 0.90f, 1.0f);

        // Success Banner
        GameObject succGO = CreateUIObject("TouchdownSuccessBanner", canvasGO.transform);
        SetRect(succGO.GetComponent<RectTransform>(), new Vector2(0.25f, 0.40f), new Vector2(0.75f, 0.60f));
        Image sBg = succGO.AddComponent<Image>();
        sBg.color = new Color(0.04f, 0.18f, 0.09f, 0.95f);
        succGO.AddComponent<Outline>().effectColor = new Color(0.30f, 1.0f, 0.55f, 1.0f);

        GameObject sText = CreateUIObject("Text", succGO.transform);
        StretchFull(sText.GetComponent<RectTransform>());
        TextMeshProUGUI stTMP = sText.AddComponent<TextMeshProUGUI>();
        if (font != null) stTMP.font = font;
        stTMP.text = "LUNAR TOUCHDOWN CONFIRMED";
        stTMP.fontSize = 20f;
        stTMP.fontStyle = FontStyles.Bold;
        stTMP.alignment = TextAlignmentOptions.Center;
        stTMP.color = new Color(0.35f, 1.0f, 0.55f);
        ui.touchdownSuccessText = stTMP;
        ui.touchdownSuccessBanner = succGO;
        succGO.SetActive(false);

        // Crash Failure Modal
        GameObject failModal = CreateUIObject("CrashFailureModal", canvasGO.transform);
        StretchFull(failModal.GetComponent<RectTransform>());
        Image fBackdrop = failModal.AddComponent<Image>();
        fBackdrop.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject fCard = CreateUIObject("Card", failModal.transform);
        SetRect(fCard.GetComponent<RectTransform>(), new Vector2(0.30f, 0.32f), new Vector2(0.70f, 0.68f));
        Image fcImg = fCard.AddComponent<Image>();
        fcImg.color = new Color(0.14f, 0.05f, 0.05f, 0.98f);
        fCard.AddComponent<Outline>().effectColor = new Color(1.0f, 0.25f, 0.25f, 1.0f);

        GameObject fhGO = CreateUIObject("Header", fCard.transform);
        SetRect(fhGO.GetComponent<RectTransform>(), new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f));
        TextMeshProUGUI fhTMP = fhGO.AddComponent<TextMeshProUGUI>();
        if (font != null) fhTMP.font = font;
        fhTMP.text = "[ HARD IMPACT // LANDER STRUCTURAL FAILURE ]";
        fhTMP.fontSize = 17f;
        fhTMP.fontStyle = FontStyles.Bold;
        fhTMP.alignment = TextAlignmentOptions.Center;
        fhTMP.color = new Color(1.0f, 0.35f, 0.35f);

        GameObject frGO = CreateUIObject("Reason", fCard.transform);
        SetRect(frGO.GetComponent<RectTransform>(), new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.75f));
        TextMeshProUGUI frTMP = frGO.AddComponent<TextMeshProUGUI>();
        if (font != null) frTMP.font = font;
        frTMP.text = "Impact velocity exceeded landing gear structural limits.";
        frTMP.fontSize = 15f;
        frTMP.alignment = TextAlignmentOptions.Center;
        frTMP.color = new Color(0.88f, 0.88f, 0.92f);
        ui.crashReasonText = frTMP;

        GameObject btnRetry = CreateButton("RetryButton", "RETRY DESCENT", font, fCard.transform,
            new Vector2(0.08f, 0.08f), new Vector2(0.48f, 0.26f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.15f, 0.52f, 0.88f, 0.95f));
        ui.retryDescentButton = btnRetry.GetComponent<Button>();

        GameObject btnMenu = CreateButton("MenuButton", "RETURN TO MISSIONS", font, fCard.transform,
            new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.26f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.25f, 0.28f, 0.35f, 0.95f));
        ui.returnToMissionsButton = btnMenu.GetComponent<Button>();

        ui.crashFailureModal = failModal;
        failModal.SetActive(false);

        return canvasGO;
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
        earthGO.transform.rotation = Quaternion.Euler(25f, 75f, 0f);
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

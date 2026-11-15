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
/// Scene automation builder for MissionResults:
/// creates the comprehensive NASA post-mission debriefing screen,
/// displaying payload samples recovered, flight performance ratings,
/// and mission selection / replay navigation.
/// </summary>
public class MissionResultsSceneBuilder
{
    private const string ScenesPath = "Assets/Scenes";
    private const string MaterialsPath = "Assets/Materials";

    [MenuItem("ASTROBASE/Missions/Build Mission Results Scene")]
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
            if (!File.Exists(ScenesPath + "/MissionResults.unity"))
            {
                Debug.Log("[ASTROBASE] Initializing MissionResults scene...");
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

            TMP_FontAsset font = FindTMPFont();

            BuildResultsScene(font);
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: Mission Results Built!",
                    "Scene created at:\n  Assets/Scenes/MissionResults.unity\n\n" +
                    "  ✓ Mission completion status badge (Accomplished vs Failed)\n" +
                    "  ✓ Navigation accuracy & waypoint corridor telemetry\n" +
                    "  ✓ Landing performance (touchdown sink rate, airspeed, centerline)\n" +
                    "  ✓ Surface exploration objectives & science survey verification\n" +
                    "  ✓ Number of samples collected (3 / 3)\n" +
                    "  ✓ Number of photographs captured (1 / 1 Earthrise)\n" +
                    "  ✓ Fuel remaining percentage with visual meter bar\n" +
                    "  ✓ Dynamic Three-Star Rating system ([ * ] [ * ] [ * ])\n" +
                    "  ✓ Functional Replay & Mission Selection buttons\n\n" +
                    "Press Play ▶ in MissionResults to view the debriefing screen!",
                    "OK");
            }
            Debug.Log("[ASTROBASE] MissionResults scene built successfully.");
        }
        catch (System.Exception ex)
        {
            if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", ex.Message, "OK");
            Debug.LogError("[ASTROBASE] MissionResults build failed: " + ex);
        }
    }

    private static void BuildResultsScene(TMP_FontAsset font)
    {
        string scenePath = ScenesPath + "/MissionResults.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Lighting & Environment
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.04f, 0.05f, 0.08f);

        GameObject sunGO = new GameObject("Sunlight");
        Light sunLight = sunGO.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1.0f, 0.98f, 0.92f);
        sunLight.intensity = 1.3f;
        sunGO.transform.rotation = Quaternion.Euler(25f, -35f, 0f);

        // Starfield
        BuildStarfield("DeepSpaceStarfield", 2500, 450f);

        // Distant Earth in background
        GameObject earthGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        earthGO.name = "Background_Earth";
        earthGO.transform.position = new Vector3(80f, 20f, 320f);
        earthGO.transform.localScale = Vector3.one * 110f;
        Object.DestroyImmediate(earthGO.GetComponent<Collider>());

        Texture2D earthTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/NASA_Textures/earth_nasa_diffuse.jpg") ??
                             AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/NASA_Textures/earth_daymap.jpg");
        Material earthMat = GetOrCreateLitMaterial("Debrief_Earth_Mat", new Color(0.9f, 0.95f, 1f), 0.05f, 0.45f, earthTex);
        earthGO.GetComponent<Renderer>().sharedMaterial = earthMat;

        // Slow rotation
        earthGO.AddComponent<PlanetRotation>().rotationSpeed = 0.5f;

        // Camera
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        camGO.AddComponent<AudioListener>();

        // 2. UI Canvas & Debriefing Panels
        EnsureEventSystem();
        GameObject canvasGO = BuildDebriefCanvas(font);

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static GameObject BuildDebriefCanvas(TMP_FontAsset font)
    {
        GameObject canvasGO = new GameObject("MissionResults_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        MissionResultsUI ui = canvasGO.AddComponent<MissionResultsUI>();

        // Central Main Frame
        GameObject mainPanel = CreateUIObject("MainPanel", canvasGO.transform);
        SetRect(mainPanel.GetComponent<RectTransform>(), new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.96f));
        Image pBg = mainPanel.AddComponent<Image>();
        pBg.color = new Color(0.02f, 0.05f, 0.09f, 0.95f);
        mainPanel.AddComponent<Outline>().effectColor = new Color(0.25f, 0.75f, 1.0f, 0.85f);

        // Header Section
        GameObject titleGO = CreateUIObject("Title", mainPanel.transform);
        SetRect(titleGO.GetComponent<RectTransform>(), new Vector2(0.03f, 0.90f), new Vector2(0.58f, 0.98f));
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTMP.font = font;
        titleTMP.text = "LUNAR EXPEDITION: TRANSLUNAR & RETURN TO EARTH";
        titleTMP.fontSize = 22f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Left;
        titleTMP.color = Color.white;
        ui.missionTitleText = titleTMP;

        GameObject subGO = CreateUIObject("Subtitle", mainPanel.transform);
        SetRect(subGO.GetComponent<RectTransform>(), new Vector2(0.03f, 0.85f), new Vector2(0.58f, 0.90f));
        TextMeshProUGUI subTMP = subGO.AddComponent<TextMeshProUGUI>();
        if (font != null) subTMP.font = font;
        subTMP.text = "MISSION DESIGNATION: APOLLO-NEXT // NASA FLIGHT CONTROL & KSC RECOVERY FACILITY";
        subTMP.fontSize = 12f;
        subTMP.alignment = TextAlignmentOptions.Left;
        subTMP.color = new Color(0.70f, 0.82f, 0.95f);
        ui.missionSubtitleText = subTMP;

        // Status Badge (Top-Right)
        GameObject badgeGO = CreateUIObject("StatusBadge", mainPanel.transform);
        SetRect(badgeGO.GetComponent<RectTransform>(), new Vector2(0.60f, 0.86f), new Vector2(0.97f, 0.96f));
        Image bBg = badgeGO.AddComponent<Image>();
        bBg.color = new Color(0.05f, 0.22f, 0.12f, 0.95f);
        badgeGO.AddComponent<Outline>().effectColor = new Color(0.35f, 1.0f, 0.55f, 0.95f);
        ui.statusBadgeBackground = bBg;

        GameObject badgeText = CreateUIObject("BadgeText", badgeGO.transform);
        StretchFull(badgeText.GetComponent<RectTransform>());
        TextMeshProUGUI bTMP = badgeText.AddComponent<TextMeshProUGUI>();
        if (font != null) bTMP.font = font;
        bTMP.text = "[SUCCESS] MISSION ACCOMPLISHED // RETURN TO EARTH CONFIRMED";
        bTMP.fontSize = 13f;
        bTMP.fontStyle = FontStyles.Bold;
        bTMP.alignment = TextAlignmentOptions.Center;
        bTMP.color = new Color(0.35f, 1.0f, 0.55f);
        ui.overallStatusBadgeText = bTMP;

        // 1. HERO THREE-STAR RATING CARD (Top Row: 0.67f to 0.84f)
        GameObject starCard = CreateUIObject("StarRatingCard", mainPanel.transform);
        SetRect(starCard.GetComponent<RectTransform>(), new Vector2(0.03f, 0.67f), new Vector2(0.97f, 0.84f));
        Image scBg = starCard.AddComponent<Image>();
        scBg.color = new Color(0.06f, 0.10f, 0.15f, 0.92f);
        starCard.AddComponent<Outline>().effectColor = new Color(1.0f, 0.85f, 0.25f, 0.85f);

        // Star Icons & Title (Left column)
        GameObject starIconsGO = CreateUIObject("StarIcons", starCard.transform);
        SetRect(starIconsGO.GetComponent<RectTransform>(), new Vector2(0.02f, 0.40f), new Vector2(0.34f, 0.95f));
        TextMeshProUGUI sIconsTMP = starIconsGO.AddComponent<TextMeshProUGUI>();
        if (font != null) sIconsTMP.font = font;
        sIconsTMP.text = "<color=#FFD700>[ * ]   [ * ]   [ * ]</color>";
        sIconsTMP.fontSize = 32f;
        sIconsTMP.fontStyle = FontStyles.Bold;
        sIconsTMP.alignment = TextAlignmentOptions.Center;
        ui.starRatingIconsText = sIconsTMP;

        GameObject starCommentGO = CreateUIObject("StarComment", starCard.transform);
        SetRect(starCommentGO.GetComponent<RectTransform>(), new Vector2(0.02f, 0.08f), new Vector2(0.34f, 0.42f));
        TextMeshProUGUI sCommTMP = starCommentGO.AddComponent<TextMeshProUGUI>();
        if (font != null) sCommTMP.font = font;
        sCommTMP.text = "[***] THREE-STAR EXPEDITION";
        sCommTMP.fontSize = 12.5f;
        sCommTMP.fontStyle = FontStyles.Bold;
        sCommTMP.alignment = TextAlignmentOptions.Center;
        sCommTMP.color = new Color(1.0f, 0.85f, 0.30f);
        ui.starRatingCommentText = sCommTMP;

        // Star Requirements Breakdown (Right column)
        GameObject starReqGO = CreateUIObject("StarRequirements", starCard.transform);
        SetRect(starReqGO.GetComponent<RectTransform>(), new Vector2(0.36f, 0.06f), new Vector2(0.98f, 0.94f));
        TextMeshProUGUI sReqTMP = starReqGO.AddComponent<TextMeshProUGUI>();
        if (font != null) sReqTMP.font = font;
        sReqTMP.fontSize = 12f;
        sReqTMP.lineSpacing = 10f;
        sReqTMP.color = new Color(0.88f, 0.92f, 0.98f);
        ui.starRequirementsBreakdownText = sReqTMP;

        // 2. MIDDLE-LEFT CARD: FLIGHT, NAVIGATION & LANDING TELEMETRY (0.03f to 0.485f, Y: 0.14f to 0.65f)
        GameObject navCard = CreateUIObject("FlightAndLandingCard", mainPanel.transform);
        SetRect(navCard.GetComponent<RectTransform>(), new Vector2(0.03f, 0.14f), new Vector2(0.485f, 0.65f));
        Image ncBg = navCard.AddComponent<Image>();
        ncBg.color = new Color(0.04f, 0.08f, 0.13f, 0.92f);
        navCard.AddComponent<Outline>().effectColor = new Color(0.25f, 0.85f, 1.0f, 0.65f);

        GameObject ncTitleGO = CreateUIObject("Title", navCard.transform);
        SetRect(ncTitleGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.98f));
        TextMeshProUGUI nctTMP = ncTitleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) nctTMP.font = font;
        nctTMP.text = "[ SPACEFLIGHT, NAVIGATION & LANDING TELEMETRY ]";
        nctTMP.fontSize = 13f;
        nctTMP.fontStyle = FontStyles.Bold;
        nctTMP.color = new Color(0.35f, 0.90f, 1.0f);

        // Section A: Navigation Accuracy
        GameObject navAccGO = CreateUIObject("NavigationAccuracy", navCard.transform);
        SetRect(navAccGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.54f), new Vector2(0.96f, 0.86f));
        TextMeshProUGUI naTMP = navAccGO.AddComponent<TextMeshProUGUI>();
        if (font != null) naTMP.font = font;
        naTMP.fontSize = 12.5f;
        naTMP.lineSpacing = 10f;
        naTMP.color = new Color(0.88f, 0.92f, 0.96f);
        ui.navigationAccuracyText = naTMP;

        // Section B: Landing Performance
        GameObject landPerfGO = CreateUIObject("LandingPerformance", navCard.transform);
        SetRect(landPerfGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.52f));
        TextMeshProUGUI lpTMP = landPerfGO.AddComponent<TextMeshProUGUI>();
        if (font != null) lpTMP.font = font;
        lpTMP.fontSize = 12.5f;
        lpTMP.lineSpacing = 10f;
        lpTMP.color = new Color(0.88f, 0.92f, 0.96f);
        ui.landingPerformanceText = lpTMP;

        // Section C: Remaining Fuel
        GameObject fuelTextGO = CreateUIObject("FuelText", navCard.transform);
        SetRect(fuelTextGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.08f), new Vector2(0.50f, 0.18f));
        TextMeshProUGUI ftTMP = fuelTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) ftTMP.font = font;
        ftTMP.text = "REMAINING FLIGHT FUEL: 68.5%";
        ftTMP.fontSize = 12f;
        ftTMP.fontStyle = FontStyles.Bold;
        ftTMP.color = new Color(0.35f, 0.90f, 1.0f);
        ui.fuelRemainingText = ftTMP;

        ui.fuelRemainingBar = CreateBar("FuelBar", navCard.transform, new Vector2(0.52f, 0.09f), new Vector2(0.96f, 0.17f), new Color(0.25f, 0.85f, 1.0f));

        // 3. MIDDLE-RIGHT CARD: SURFACE EXPLORATION, SAMPLES & PHOTOGRAPHY (0.515f to 0.97f, Y: 0.14f to 0.65f)
        GameObject expCard = CreateUIObject("ExplorationAndScienceCard", mainPanel.transform);
        SetRect(expCard.GetComponent<RectTransform>(), new Vector2(0.515f, 0.14f), new Vector2(0.97f, 0.65f));
        Image ecBg = expCard.AddComponent<Image>();
        ecBg.color = new Color(0.04f, 0.08f, 0.13f, 0.92f);
        expCard.AddComponent<Outline>().effectColor = new Color(0.35f, 1.0f, 0.55f, 0.65f);

        GameObject ecTitleGO = CreateUIObject("Title", expCard.transform);
        SetRect(ecTitleGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.98f));
        TextMeshProUGUI ectTMP = ecTitleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) ectTMP.font = font;
        ectTMP.text = "[ SURFACE EVA OBJECTIVES & SCIENTIFIC RECOVERIES ]";
        ectTMP.fontSize = 13f;
        ectTMP.fontStyle = FontStyles.Bold;
        ectTMP.color = new Color(0.35f, 1.0f, 0.55f);

        // Section A: Exploration Objectives
        GameObject expObjGO = CreateUIObject("ExplorationObjectives", expCard.transform);
        SetRect(expObjGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.56f), new Vector2(0.96f, 0.86f));
        TextMeshProUGUI eoTMP = expObjGO.AddComponent<TextMeshProUGUI>();
        if (font != null) eoTMP.font = font;
        eoTMP.fontSize = 12.5f;
        eoTMP.lineSpacing = 10f;
        eoTMP.color = new Color(0.88f, 0.92f, 0.96f);
        ui.explorationObjectivesText = eoTMP;

        // Section B: Number of Samples Collected
        GameObject samplesGO = CreateUIObject("SamplesCollected", expCard.transform);
        SetRect(samplesGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.26f), new Vector2(0.96f, 0.54f));
        TextMeshProUGUI scTMP = samplesGO.AddComponent<TextMeshProUGUI>();
        if (font != null) scTMP.font = font;
        scTMP.fontSize = 12.5f;
        scTMP.lineSpacing = 10f;
        scTMP.color = new Color(0.88f, 0.92f, 0.96f);
        ui.samplesCollectedText = scTMP;

        // Section C: Number of Photographs
        GameObject photosGO = CreateUIObject("PhotographsCaptured", expCard.transform);
        SetRect(photosGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.24f));
        TextMeshProUGUI pTMP = photosGO.AddComponent<TextMeshProUGUI>();
        if (font != null) pTMP.font = font;
        pTMP.fontSize = 12.5f;
        pTMP.lineSpacing = 10f;
        pTMP.color = new Color(0.88f, 0.92f, 0.96f);
        ui.photographsCapturedText = pTMP;

        // 4. ACTION BUTTONS (Bottom Row: 0.03f to 0.11f)
        GameObject replayBtn = CreateButton("ReplayButton", "REPLAY LUNAR EXPEDITION", font, mainPanel.transform,
            new Vector2(0.515f, 0.03f), new Vector2(0.735f, 0.11f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.18f, 0.26f, 0.38f, 0.98f));
        ui.replayMissionButton = replayBtn.GetComponent<Button>();

        GameObject exitBtn = CreateButton("SelectionButton", "MISSION SELECTION >", font, mainPanel.transform,
            new Vector2(0.75f, 0.03f), new Vector2(0.97f, 0.11f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.12f, 0.65f, 0.40f, 0.98f));
        ui.returnToMissionSelectionButton = exitBtn.GetComponent<Button>();

        ui.PopulateDebriefingData();

        return canvasGO;
    }

    private static Image CreateBar(string name, Transform parent, Vector2 min, Vector2 max, Color fillColor)
    {
        GameObject bgGO = CreateUIObject(name + "_Bg", parent);
        SetRect(bgGO.GetComponent<RectTransform>(), min, max);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.10f, 0.14f, 0.20f, 0.90f);

        GameObject fillGO = CreateUIObject("Fill", bgGO.transform);
        StretchFull(fillGO.GetComponent<RectTransform>());
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1.0f;
        fillImg.color = fillColor;

        return fillImg;
    }

    private static TextMeshProUGUI BuildDebriefCard(Transform parent, TMP_FontAsset font, string cardTitle, Vector2 min, Vector2 max, Color bgCol, Color outlineCol)
    {
        GameObject card = CreateUIObject(cardTitle.Replace(" ", "_"), parent);
        SetRect(card.GetComponent<RectTransform>(), min, max);
        Image img = card.AddComponent<Image>();
        img.color = bgCol;
        card.AddComponent<Outline>().effectColor = outlineCol;

        GameObject titleGO = CreateUIObject("Title", card.transform);
        SetRect(titleGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.95f));
        TextMeshProUGUI tTMP = titleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) tTMP.font = font;
        tTMP.text = $"[ {cardTitle} ]";
        tTMP.fontSize = 14f;
        tTMP.fontStyle = FontStyles.Bold;
        tTMP.color = new Color(0.35f, 0.90f, 1.0f);

        GameObject bodyGO = CreateUIObject("Body", card.transform);
        SetRect(bodyGO.GetComponent<RectTransform>(), new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.76f));
        TextMeshProUGUI bTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        if (font != null) bTMP.font = font;
        bTMP.fontSize = 13f;
        bTMP.lineSpacing = 16f;
        bTMP.color = new Color(0.88f, 0.92f, 0.96f);

        return bTMP;
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
        tmp.fontSize = 13f;
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
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null)
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
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

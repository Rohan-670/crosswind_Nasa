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
/// Automation tool to build the MissionSelection and MissionPreparation scenes,
/// generate mission preview textures, create ScriptableObjects, and update Build Settings.
/// </summary>
public class MissionSelectionSceneBuilder
{
    private const string MissionsSOPath = "Assets/ScriptableObjects/Missions";
    private const string ScenesPath = "Assets/Scenes";
    private const string TexturesPath = "Assets/Art/NASA_Textures";
    private const string PrefabsUIPath = "Assets/Prefabs/UI";

    [MenuItem("ASTROBASE/Missions/Build MissionSelection Scene")]
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
            if (!File.Exists(ScenesPath + "/MissionSelection.unity"))
            {
                Debug.Log("[ASTROBASE] Initializing MissionSelection scene automatically...");
                ExecuteBuild(true);
            }
        };
    }

    public static void ExecuteBuild(bool isBatch)
    {
        try
        {
            EnsureDirectory(MissionsSOPath);
            EnsureDirectory(ScenesPath);
            EnsureDirectory(TexturesPath);
            EnsureDirectory(PrefabsUIPath);

            TMP_FontAsset font = FindTMPFont();

            // 1. Generate preview sprites
            Sprite lunarSprite = GetOrCreatePreviewSprite("Mission_Preview_Lunar.png", new Color(0.75f, 0.78f, 0.82f), new Color(0.18f, 0.20f, 0.25f));
            Sprite marsSprite = GetOrCreatePreviewSprite("Mission_Preview_Mars.png", new Color(0.95f, 0.45f, 0.22f), new Color(0.35f, 0.12f, 0.06f));
            Sprite solarSprite = GetOrCreatePreviewSprite("Mission_Preview_Sun.png", new Color(1.0f, 0.88f, 0.25f), new Color(0.85f, 0.35f, 0.05f));

            // 2. Create or update ScriptableObjects
            MissionDefinition lunarDef = CreateOrUpdateLunarMission(lunarSprite);
            MissionDefinition marsDef = CreateOrUpdateMarsMission(marsSprite);
            MissionDefinition solarDef = CreateOrUpdateSolarMission(solarSprite);

            MissionDefinition[] missions = new MissionDefinition[] { lunarDef, marsDef, solarDef };

            // 3. Build MissionSelection.unity scene
            BuildMissionSelectionScene(missions, font);

            // 4. Build MissionPreparation.unity scene
            BuildMissionPreparationScene(font);

            // 5. Update EditorBuildSettings
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: Mission Selection Ready!",
                    "MissionSelection scene created successfully at:\n  Assets/Scenes/MissionSelection.unity\n\n" +
                    "  ✓ 3 Responsive Expedition Cards (Moon, Mars, Sun)\n" +
                    "  ✓ ScriptableObject definitions saved in Assets/ScriptableObjects/Missions/\n" +
                    "  ✓ Reusable MissionManager storing player selection\n" +
                    "  ✓ Lunar Expedition proceeds to MissionPreparation scene\n" +
                    "  ✓ Mars & Solar Expeditions display informative Coming Soon telemetry\n" +
                    "  ✓ Build Settings updated for seamless browser transitions\n\n" +
                    "Press Play ▶ in Unity Editor to test Mission Selection!",
                    "OK");
            }
            Debug.Log("[ASTROBASE] MissionSelection scene built successfully.");
        }
        catch (System.Exception ex)
        {
            if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", ex.Message, "OK");
            Debug.LogError("[ASTROBASE] MissionSelection build failed: " + ex);
        }
    }

    // ===================================================================
    // 1. SCRIPTABLE OBJECT DEFINITIONS
    // ===================================================================

    private static MissionDefinition CreateOrUpdateLunarMission(Sprite preview)
    {
        string path = MissionsSOPath + "/Mission_01_Lunar.asset";
        MissionDefinition def = AssetDatabase.LoadAssetAtPath<MissionDefinition>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<MissionDefinition>();
            AssetDatabase.CreateAsset(def, path);
        }

        def.missionId = "lunar_expedition";
        def.missionName = "Lunar Expedition";
        def.targetDestination = "The Moon (South Pole)";
        def.estimatedDuration = "14 Earth Days";
        def.description = "Descend to the lunar surface at Shackleton Crater. Establish an exploratory base camp, drill and collect volatile core ice samples, and return crew safely to Earth.";
        def.difficulty = MissionDifficulty.Moderate;
        def.accentColor = new Color(0.28f, 0.85f, 0.95f);
        def.previewImage = preview;
        def.objectives = new string[]
        {
            "Trans-Lunar injection burn and orbital insertion",
            "Targeted precision descent to Shackleton Crater",
            "Establish surface science habitat & power array",
            "Collect cryogenic lunar regolith and ice samples",
            "Ascent to lunar orbit and safe crew return to Earth"
        };
        def.isPlayableNow = true;
        def.targetSceneName = "MissionPreparation";
        def.comingSoonNotice = "";

        EditorUtility.SetDirty(def);
        return def;
    }

    private static MissionDefinition CreateOrUpdateMarsMission(Sprite preview)
    {
        string path = MissionsSOPath + "/Mission_02_Mars.asset";
        MissionDefinition def = AssetDatabase.LoadAssetAtPath<MissionDefinition>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<MissionDefinition>();
            AssetDatabase.CreateAsset(def, path);
        }

        def.missionId = "mars_expedition";
        def.missionName = "Red Planet Expedition";
        def.targetDestination = "Mars (Jezero Crater)";
        def.estimatedDuration = "2.5 Earth Years";
        def.description = "Embark on humanity's first crewed interplanetary voyage. Execute atmospheric entry into the Martian atmosphere, explore ancient river deltas for biosignatures, and return to Earth.";
        def.difficulty = MissionDifficulty.Challenging;
        def.accentColor = new Color(1.0f, 0.45f, 0.20f);
        def.previewImage = preview;
        def.objectives = new string[]
        {
            "Interplanetary Hohmann transfer trajectory to Mars",
            "Hypersonic aerocapture & retro-propulsive landing",
            "Survey delta sediment layers at Jezero Crater",
            "Extract atmospheric CO2 for return propellant",
            "Launch Mars Ascent Vehicle and transit back to Earth"
        };
        def.isPlayableNow = false;
        def.targetSceneName = "MissionPreparation";
        def.comingSoonNotice = "RED PLANET EXPEDITION is currently in flight hardware qualification and Mars entry simulation.\n\n" +
            "Long-duration life support and surface habitat systems are undergoing testing at NASA Houston.\n\n" +
            "Clearance for Mars departure will unlock in upcoming mission updates. Please proceed with the active Lunar Expedition.";

        EditorUtility.SetDirty(def);
        return def;
    }

    private static MissionDefinition CreateOrUpdateSolarMission(Sprite preview)
    {
        string path = MissionsSOPath + "/Mission_03_Solar.asset";
        MissionDefinition def = AssetDatabase.LoadAssetAtPath<MissionDefinition>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<MissionDefinition>();
            AssetDatabase.CreateAsset(def, path);
        }

        def.missionId = "solar_orbit";
        def.missionName = "Solar Orbit Expedition";
        def.targetDestination = "Sun (Observation Orbit)";
        def.estimatedDuration = "180 Days";
        def.description = "Navigate a specialized heat-shielded spacecraft to a safe perihelion observation orbit. Collect heliophysics data, observe coronal mass ejections, and return safely to Earth. NEVER attempt to land on the Sun.";
        def.difficulty = MissionDifficulty.Extreme;
        def.accentColor = new Color(1.0f, 0.82f, 0.15f);
        def.previewImage = preview;
        def.objectives = new string[]
        {
            "Gravity assist burn into high-inclination solar orbit",
            "Maintain perihelion safe standoff distance (10 Solar Radii)",
            "Strict safety rule: Solar observation only — never land on Sun",
            "Deploy heat-shielded sensors into coronal magnetic loops",
            "Record solar wind telemetry and return crew to Earth"
        };
        def.isPlayableNow = false;
        def.targetSceneName = "MissionPreparation";
        def.comingSoonNotice = "SOLAR ORBIT EXPEDITION is undergoing thermal shield carbon-composite qualification.\n\n" +
            "NOTE: Per NASA mission protocols, this is strictly a safe orbital observation expedition. Landing on the Sun is physically impossible and prohibited by Mission Control.\n\n" +
            "Flight clearance will unlock in future ASTROBASE mission updates.";

        EditorUtility.SetDirty(def);
        return def;
    }

    // ===================================================================
    // 2. PROCEDURAL PREVIEW TEXTURES & SPRITES
    // ===================================================================

    private static Sprite GetOrCreatePreviewSprite(string filename, Color primary, Color secondary)
    {
        string path = TexturesPath + "/" + filename;
        if (!File.Exists(path))
        {
            int width = 512;
            int height = 280;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                float ny = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float nx = (float)x / width;
                    // Stylized planetary surface gradient with noise
                    float noise = Mathf.PerlinNoise(nx * 4f, ny * 4f) * 0.4f + Mathf.PerlinNoise(nx * 12f, ny * 12f) * 0.15f;
                    float t = Mathf.Clamp01(ny + (noise - 0.25f));
                    Color c = Color.Lerp(secondary, primary, t);
                    // Dark vignette edges
                    float edgeDist = Mathf.Min(nx, 1f - nx, ny, 1f - ny) * 4f;
                    c *= Mathf.Clamp01(edgeDist + 0.35f);
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(tex);
            AssetDatabase.Refresh();
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ===================================================================
    // 3. BUILD MISSION SELECTION SCENE
    // ===================================================================

    private static void BuildMissionSelectionScene(MissionDefinition[] missions, TMP_FontAsset font)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string scenePath = ScenesPath + "/MissionSelection.unity";

        // 1. Camera
        GameObject camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        Camera cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.008f, 0.012f, 0.020f, 1f); // Deep space navy
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 1000f;
        camGO.transform.position = new Vector3(0f, 0f, -10f);

        // 2. Directional Light
        GameObject lightGO = new GameObject("Directional Light", typeof(Light));
        Light light = lightGO.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.85f, 0.92f, 1.0f);
        light.intensity = 0.6f;
        lightGO.transform.rotation = Quaternion.Euler(20f, -30f, 0f);

        // 3. Persistent MissionManager
        GameObject managerGO = new GameObject("MissionManager", typeof(MissionManager));

        // 4. UI Canvas
        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject eventSystemGO = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // Background subtle grid image
        GameObject bgGO = CreateUIObject("Background_Vignette", canvasGO.transform);
        StretchFull(bgGO.GetComponent<RectTransform>());
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.01f, 0.02f, 0.04f, 0.95f);

        // Top Header Bar
        GameObject headerGO = CreateUIObject("HeaderPanel", canvasGO.transform);
        RectTransform headerRect = headerGO.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.86f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.offsetMin = new Vector2(40f, 0f);
        headerRect.offsetMax = new Vector2(-40f, -20f);

        // Back button
        GameObject backBtnGO = CreateButton("BackButton", "< FLIGHT DECK", font, headerGO.transform,
            new Vector2(0f, 0.3f), new Vector2(0f, 0.8f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0f), new Vector2(180f, 44f),
            new Color(0.15f, 0.20f, 0.28f, 0.85f));

        // Header Title
        GameObject titleGO = CreateUIObject("TitleText", headerGO.transform);
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.12f, 0.35f);
        titleRect.anchorMax = new Vector2(0.85f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTMP.font = font;
        titleTMP.text = "ASTROBASE // MISSION OPERATIONS";
        titleTMP.fontSize = 32f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = Color.white;
        titleTMP.characterSpacing = 3f;

        // Header Subtitle
        GameObject subTitleGO = CreateUIObject("SubTitleText", headerGO.transform);
        RectTransform subTitleRect = subTitleGO.GetComponent<RectTransform>();
        subTitleRect.anchorMin = new Vector2(0.12f, 0f);
        subTitleRect.anchorMax = new Vector2(0.85f, 0.40f);
        subTitleRect.offsetMin = Vector2.zero;
        subTitleRect.offsetMax = Vector2.zero;
        TextMeshProUGUI subTitleTMP = subTitleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) subTitleTMP.font = font;
        subTitleTMP.text = "SELECT EXPEDITION FOR FLIGHT CLEARANCE // TELEMETRY: ALL SYSTEMS NOMINAL";
        subTitleTMP.fontSize = 15f;
        subTitleTMP.color = new Color(0.4f, 0.85f, 1.0f);
        subTitleTMP.characterSpacing = 2f;

        // Cards Container (Horizontal Row of 3 Cards)
        GameObject cardsContainerGO = CreateUIObject("CardsContainer", canvasGO.transform);
        RectTransform cardsRect = cardsContainerGO.GetComponent<RectTransform>();
        cardsRect.anchorMin = new Vector2(0.04f, 0.08f);
        cardsRect.anchorMax = new Vector2(0.96f, 0.84f);
        cardsRect.offsetMin = Vector2.zero;
        cardsRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup hlg = cardsContainerGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 32f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // Build the 3 Mission Cards
        for (int i = 0; i < missions.Length; i++)
        {
            BuildCard(cardsContainerGO.transform, missions[i], font);
        }

        // Build Coming Soon Modal
        GameObject modalGO = BuildComingSoonModal(canvasGO.transform, font);

        // Wire MissionSelectionUI
        MissionSelectionUI ui = canvasGO.AddComponent<MissionSelectionUI>();
        ui.missions = missions;
        ui.cardsContainer = cardsContainerGO.transform;
        ui.comingSoonModal = modalGO;
        ui.modalTitleText = modalGO.transform.Find("DialogPanel/TitleText").GetComponent<TextMeshProUGUI>();
        ui.modalDestinationText = modalGO.transform.Find("DialogPanel/DestinationText").GetComponent<TextMeshProUGUI>();
        ui.modalBodyText = modalGO.transform.Find("DialogPanel/BodyText").GetComponent<TextMeshProUGUI>();
        ui.modalCloseButton = modalGO.transform.Find("DialogPanel/DismissButton").GetComponent<Button>();
        ui.backButton = backBtnGO.GetComponent<Button>();

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static void BuildCard(Transform parent, MissionDefinition mission, TMP_FontAsset font)
    {
        GameObject cardGO = CreateUIObject("Card_" + mission.missionId, parent);
        Image cardBg = cardGO.AddComponent<Image>();
        cardBg.color = new Color(0.06f, 0.09f, 0.14f, 0.92f); // Dark translucent panel

        // Card Border Outline
        GameObject borderGO = CreateUIObject("CardBorder", cardGO.transform);
        StretchFull(borderGO.GetComponent<RectTransform>());
        Outline outline = borderGO.AddComponent<Outline>();
        outline.effectColor = mission.accentColor * 0.7f;
        outline.effectDistance = new Vector2(2f, 2f);

        // Header Accent Glow
        GameObject headerGlowGO = CreateUIObject("HeaderGlow", cardGO.transform);
        RectTransform hgRect = headerGlowGO.GetComponent<RectTransform>();
        hgRect.anchorMin = new Vector2(0f, 0.94f);
        hgRect.anchorMax = new Vector2(1f, 1f);
        hgRect.offsetMin = Vector2.zero;
        hgRect.offsetMax = Vector2.zero;
        Image hgImg = headerGlowGO.AddComponent<Image>();
        Color glowCol = mission.accentColor;
        glowCol.a = 0.45f;
        hgImg.color = glowCol;

        // Mission Title Text
        GameObject titleGO = CreateUIObject("MissionName", cardGO.transform);
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.06f, 0.86f);
        titleRect.anchorMax = new Vector2(0.94f, 0.94f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTMP.font = font;
        titleTMP.text = mission.missionName.ToUpper();
        titleTMP.fontSize = 24f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = Color.white;
        titleTMP.characterSpacing = 2f;

        // Destination & Duration Text
        GameObject destGO = CreateUIObject("Destination", cardGO.transform);
        RectTransform destRect = destGO.GetComponent<RectTransform>();
        destRect.anchorMin = new Vector2(0.06f, 0.80f);
        destRect.anchorMax = new Vector2(0.94f, 0.86f);
        destRect.offsetMin = Vector2.zero;
        destRect.offsetMax = Vector2.zero;
        TextMeshProUGUI destTMP = destGO.AddComponent<TextMeshProUGUI>();
        if (font != null) destTMP.font = font;
        destTMP.text = $"{mission.targetDestination.ToUpper()} // {mission.estimatedDuration.ToUpper()}";
        destTMP.fontSize = 12f;
        destTMP.color = mission.accentColor;
        destTMP.characterSpacing = 1.5f;

        // Difficulty Badge
        GameObject badgeGO = CreateUIObject("DifficultyBadge", cardGO.transform);
        RectTransform badgeRect = badgeGO.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.06f, 0.74f);
        badgeRect.anchorMax = new Vector2(0.45f, 0.79f);
        badgeRect.offsetMin = Vector2.zero;
        badgeRect.offsetMax = Vector2.zero;
        Image badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.color = new Color(0.12f, 0.16f, 0.24f, 0.9f);

        GameObject badgeTextGO = CreateUIObject("BadgeText", badgeGO.transform);
        StretchFull(badgeTextGO.GetComponent<RectTransform>());
        TextMeshProUGUI badgeTMP = badgeTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) badgeTMP.font = font;
        badgeTMP.text = mission.difficulty.ToString().ToUpper();
        badgeTMP.fontSize = 11f;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        badgeTMP.fontStyle = FontStyles.Bold;
        badgeTMP.color = mission.accentColor;

        // Preview Image Banner
        GameObject previewGO = CreateUIObject("PreviewImage", cardGO.transform);
        RectTransform prevRect = previewGO.GetComponent<RectTransform>();
        prevRect.anchorMin = new Vector2(0.06f, 0.48f);
        prevRect.anchorMax = new Vector2(0.94f, 0.72f);
        prevRect.offsetMin = Vector2.zero;
        prevRect.offsetMax = Vector2.zero;
        Image prevImg = previewGO.AddComponent<Image>();
        if (mission.previewImage != null) prevImg.sprite = mission.previewImage;
        prevImg.color = Color.white;

        // Description Text
        GameObject descGO = CreateUIObject("Description", cardGO.transform);
        RectTransform descRect = descGO.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.06f, 0.33f);
        descRect.anchorMax = new Vector2(0.94f, 0.46f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        TextMeshProUGUI descTMP = descGO.AddComponent<TextMeshProUGUI>();
        if (font != null) descTMP.font = font;
        descTMP.text = mission.description;
        descTMP.fontSize = 13f;
        descTMP.color = new Color(0.85f, 0.88f, 0.92f);
        descTMP.textWrappingMode = TextWrappingModes.Normal;

        // Objectives Header & Text
        GameObject objGO = CreateUIObject("Objectives", cardGO.transform);
        RectTransform objRect = objGO.GetComponent<RectTransform>();
        objRect.anchorMin = new Vector2(0.06f, 0.12f);
        objRect.anchorMax = new Vector2(0.94f, 0.32f);
        objRect.offsetMin = Vector2.zero;
        objRect.offsetMax = Vector2.zero;
        TextMeshProUGUI objTMP = objGO.AddComponent<TextMeshProUGUI>();
        if (font != null) objTMP.font = font;

        string bulleted = "KEY OBJECTIVES:\n";
        foreach (string obj in mission.objectives)
        {
            bulleted += $"- {obj}\n";
        }
        objTMP.text = bulleted.TrimEnd();
        objTMP.fontSize = 12f;
        objTMP.color = new Color(0.70f, 0.75f, 0.82f);
        objTMP.textWrappingMode = TextWrappingModes.Normal;

        // Select Mission Button
        Color btnCol = mission.isPlayableNow
            ? new Color(0.12f, 0.45f, 0.85f, 0.95f)
            : new Color(0.20f, 0.25f, 0.35f, 0.85f);
        string btnLabel = mission.isPlayableNow ? "SELECT MISSION >" : "VIEW STATUS";

        GameObject btnGO = CreateButton("SelectButton", btnLabel, font, cardGO.transform,
            new Vector2(0.06f, 0.025f), new Vector2(0.94f, 0.095f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, btnCol);

        // Bind MissionCardUI component
        MissionCardUI cardUI = cardGO.AddComponent<MissionCardUI>();
        cardUI.missionNameText = titleTMP;
        cardUI.destinationText = destTMP;
        cardUI.difficultyText = badgeTMP;
        cardUI.difficultyBadgeImage = badgeImg;
        cardUI.previewImage = prevImg;
        cardUI.descriptionText = descTMP;
        cardUI.objectivesText = objTMP;
        cardUI.selectButton = btnGO.GetComponent<Button>();
        cardUI.selectButtonText = btnGO.GetComponentInChildren<TextMeshProUGUI>();
        cardUI.selectButtonImage = btnGO.GetComponent<Image>();
        cardUI.cardBorder = borderGO.GetComponent<Image>();
        cardUI.headerGlow = hgImg;

        cardUI.Bind(mission, null); // Will be rebound dynamically at Start by MissionSelectionUI
    }

    private static GameObject BuildComingSoonModal(Transform parent, TMP_FontAsset font)
    {
        GameObject modalGO = CreateUIObject("ComingSoonModal", parent);
        StretchFull(modalGO.GetComponent<RectTransform>());

        // Dark modal backdrop
        Image backdrop = modalGO.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.82f);

        // Dialog Panel
        GameObject panelGO = CreateUIObject("DialogPanel", modalGO.transform);
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.28f, 0.26f);
        panelRect.anchorMax = new Vector2(0.72f, 0.74f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);

        Outline panelOutline = panelGO.AddComponent<Outline>();
        panelOutline.effectColor = new Color(1.0f, 0.65f, 0.15f, 0.8f);
        panelOutline.effectDistance = new Vector2(2f, 2f);

        // Status Header
        GameObject statGO = CreateUIObject("StatusHeader", panelGO.transform);
        RectTransform statRect = statGO.GetComponent<RectTransform>();
        statRect.anchorMin = new Vector2(0.06f, 0.84f);
        statRect.anchorMax = new Vector2(0.94f, 0.94f);
        statRect.offsetMin = Vector2.zero;
        statRect.offsetMax = Vector2.zero;
        TextMeshProUGUI statTMP = statGO.AddComponent<TextMeshProUGUI>();
        if (font != null) statTMP.font = font;
        statTMP.text = "[ MISSION CONTROL // TELEMETRY NOTICE ]";
        statTMP.fontSize = 14f;
        statTMP.color = new Color(1.0f, 0.75f, 0.25f);
        statTMP.characterSpacing = 2f;

        // Modal Title
        GameObject titleGO = CreateUIObject("TitleText", panelGO.transform);
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.06f, 0.72f);
        titleRect.anchorMax = new Vector2(0.94f, 0.84f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTMP.font = font;
        titleTMP.text = "MISSION TITLE";
        titleTMP.fontSize = 26f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = Color.white;

        // Destination Text
        GameObject destGO = CreateUIObject("DestinationText", panelGO.transform);
        RectTransform destRect = destGO.GetComponent<RectTransform>();
        destRect.anchorMin = new Vector2(0.06f, 0.64f);
        destRect.anchorMax = new Vector2(0.94f, 0.72f);
        destRect.offsetMin = Vector2.zero;
        destRect.offsetMax = Vector2.zero;
        TextMeshProUGUI destTMP = destGO.AddComponent<TextMeshProUGUI>();
        if (font != null) destTMP.font = font;
        destTMP.text = "TARGET // STATUS";
        destTMP.fontSize = 13f;
        destTMP.color = new Color(0.4f, 0.85f, 1.0f);

        // Body Text
        GameObject bodyGO = CreateUIObject("BodyText", panelGO.transform);
        RectTransform bodyRect = bodyGO.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.06f, 0.22f);
        bodyRect.anchorMax = new Vector2(0.94f, 0.62f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
        TextMeshProUGUI bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        if (font != null) bodyTMP.font = font;
        bodyTMP.text = "Notice message goes here.";
        bodyTMP.fontSize = 15f;
        bodyTMP.color = new Color(0.85f, 0.88f, 0.92f);
        bodyTMP.textWrappingMode = TextWrappingModes.Normal;

        // Dismiss Button
        CreateButton("DismissButton", "ACKNOWLEDGE & DISMISS", font, panelGO.transform,
            new Vector2(0.25f, 0.05f), new Vector2(0.75f, 0.16f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.18f, 0.24f, 0.35f, 0.95f));

        modalGO.SetActive(false);
        return modalGO;
    }

    // ===================================================================
    // 4. BUILD MISSION PREPARATION SCENE (FOR LUNAR EXPEDITION CONTINUATION)
    // ===================================================================

    private static void BuildMissionPreparationScene(TMP_FontAsset font)
    {
        string scenePath = ScenesPath + "/MissionPreparation.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        GameObject camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        Camera cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.008f, 0.012f, 0.020f, 1f);

        // UI Canvas
        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // Background
        GameObject bgGO = CreateUIObject("Background", canvasGO.transform);
        StretchFull(bgGO.GetComponent<RectTransform>());
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.95f);

        // Main Preparation Panel
        GameObject panelGO = CreateUIObject("PreparationPanel", canvasGO.transform);
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.18f, 0.12f);
        panelRect.anchorMax = new Vector2(0.82f, 0.88f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.06f, 0.09f, 0.15f, 0.94f);
        panelGO.AddComponent<Outline>().effectColor = new Color(0.28f, 0.85f, 0.95f, 0.7f);

        // Title
        GameObject titleGO = CreateUIObject("TitleText", panelGO.transform);
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.06f, 0.84f);
        titleRect.anchorMax = new Vector2(0.94f, 0.96f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTMP.font = font;
        titleTMP.text = "LUNAR EXPEDITION // PREPARATION";
        titleTMP.fontSize = 28f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = Color.white;

        // Destination & Duration
        GameObject destGO = CreateUIObject("DestinationText", panelGO.transform);
        RectTransform destRect = destGO.GetComponent<RectTransform>();
        destRect.anchorMin = new Vector2(0.06f, 0.78f);
        destRect.anchorMax = new Vector2(0.94f, 0.84f);
        destRect.offsetMin = Vector2.zero;
        destRect.offsetMax = Vector2.zero;
        TextMeshProUGUI destTMP = destGO.AddComponent<TextMeshProUGUI>();
        if (font != null) destTMP.font = font;
        destTMP.text = "DESTINATION: THE MOON // SOUTH POLE // 14 EARTH DAYS";
        destTMP.fontSize = 14f;
        destTMP.color = new Color(0.28f, 0.85f, 0.95f);

        // Briefing Text
        GameObject descGO = CreateUIObject("DescriptionText", panelGO.transform);
        RectTransform descRect = descGO.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.06f, 0.60f);
        descRect.anchorMax = new Vector2(0.94f, 0.76f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        TextMeshProUGUI descTMP = descGO.AddComponent<TextMeshProUGUI>();
        if (font != null) descTMP.font = font;
        descTMP.text = "Flight clearance confirmed by Mission Control. Spacecraft payload bay is being loaded with cryogenic sample containers and surface telemetry equipment.";
        descTMP.fontSize = 16f;
        descTMP.color = new Color(0.85f, 0.88f, 0.92f);
        descTMP.textWrappingMode = TextWrappingModes.Normal;

        // Objectives Text
        GameObject objGO = CreateUIObject("ObjectivesText", panelGO.transform);
        RectTransform objRect = objGO.GetComponent<RectTransform>();
        objRect.anchorMin = new Vector2(0.06f, 0.28f);
        objRect.anchorMax = new Vector2(0.94f, 0.58f);
        objRect.offsetMin = Vector2.zero;
        objRect.offsetMax = Vector2.zero;
        TextMeshProUGUI objTMP = objGO.AddComponent<TextMeshProUGUI>();
        if (font != null) objTMP.font = font;
        objTMP.text = "MISSION OBJECTIVES:\n- Trans-Lunar injection burn and orbital insertion\n- Targeted precision descent to Shackleton Crater\n- Establish surface science habitat\n- Collect cryogenic lunar regolith samples\n- Safe return to Earth";
        objTMP.fontSize = 15f;
        objTMP.color = new Color(0.75f, 0.80f, 0.88f);
        objTMP.textWrappingMode = TextWrappingModes.Normal;

        // Status Text
        GameObject statGO = CreateUIObject("LaunchpadStatusText", panelGO.transform);
        RectTransform statRect = statGO.GetComponent<RectTransform>();
        statRect.anchorMin = new Vector2(0.06f, 0.16f);
        statRect.anchorMax = new Vector2(0.94f, 0.25f);
        statRect.offsetMin = Vector2.zero;
        statRect.offsetMax = Vector2.zero;
        TextMeshProUGUI statTMP = statGO.AddComponent<TextMeshProUGUI>();
        if (font != null) statTMP.font = font;
        statTMP.text = "STATUS: READY FOR EQUIPMENT & EXPLORATION STAGING";
        statTMP.fontSize = 14f;
        statTMP.color = new Color(0.4f, 1.0f, 0.6f);

        // Return Button
        GameObject retBtnGO = CreateButton("ReturnButton", "< RETURN TO MISSION SELECT", font, panelGO.transform,
            new Vector2(0.06f, 0.05f), new Vector2(0.48f, 0.14f), new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.18f, 0.24f, 0.35f, 0.95f));

        // Launchpad Button
        GameObject launchBtnGO = CreateButton("LaunchpadButton", "CONFIRM FLIGHT READINESS >", font, panelGO.transform,
            new Vector2(0.52f, 0.05f), new Vector2(0.94f, 0.14f), new Vector2(1f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.12f, 0.50f, 0.85f, 0.95f));

        // Bind MissionPreparationUI
        MissionPreparationUI prepUI = canvasGO.AddComponent<MissionPreparationUI>();
        prepUI.missionTitleText = titleTMP;
        prepUI.destinationText = destTMP;
        prepUI.descriptionText = descTMP;
        prepUI.objectivesText = objTMP;
        prepUI.launchpadStatusText = statTMP;
        prepUI.returnButton = retBtnGO.GetComponent<Button>();
        prepUI.launchpadButton = launchBtnGO.GetComponent<Button>();

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    // ===================================================================
    // 5. UPDATE BUILD SETTINGS
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
    // UI BUILDER HELPERS
    // ===================================================================

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static GameObject CreateButton(string name, string label, TMP_FontAsset font,
        Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 offsetMin, Vector2 offsetMax, Color bgColor)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);

        RectTransform rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image img = btnGO.AddComponent<Image>();
        img.color = bgColor;

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.35f;
        colors.pressedColor = bgColor * 0.75f;
        colors.selectedColor = bgColor;
        btn.colors = colors;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        StretchFull(textGO.AddComponent<RectTransform>());

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 14f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.characterSpacing = 1.5f;

        return btnGO;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TMP_FontAsset FindTMPFont()
    {
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.Contains("LiberationSans") || p.Contains("Default"))
            {
                return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p);
            }
        }
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return null;
    }

    private static void EnsureDirectory(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}

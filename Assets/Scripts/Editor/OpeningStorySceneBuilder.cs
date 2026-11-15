using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>
/// Editor tool for ASTROBASE to build and safely upgrade the OpeningStory scene
/// into a cinematic 3D space experience.
/// </summary>
public class OpeningStorySceneBuilder
{
    private const string MaterialsPath = "Assets/Materials";
    private const string SettingsPath = "Assets/Settings";
    private const string ScenesPath = "Assets/Scenes";
    private const string TexturesPath = "Assets/Art/NASA_Textures";

    // ===================================================================
    // MENU ITEMS
    // ===================================================================

    [MenuItem("ASTROBASE/Upgrade Existing Opening Story Scene (Visual Polish)")]
    public static void UpgradeSceneInteractive()
    {
        if (!EditorUtility.DisplayDialog(
            "ASTROBASE: Upgrade Opening Story Scene",
            "This will apply cinematic visual upgrades to the existing OpeningStory scene:\n\n" +
            "  • Realistic Earth with continents, clouds & night city lights\n" +
            "  • High-tech Spacecraft with solar wings, dish & glowing engines\n" +
            "  • Deep space environment with 4,500 stars and cosmic nebula\n" +
            "  • Smooth cinematic orbital camera motion\n" +
            "  • Futuristic UI with speaker callsigns and larger buttons\n" +
            "  • Official 3-line astronaut dialogue introduction\n\n" +
            "Existing scene objects and button functionality will be preserved.\nContinue?",
            "Upgrade Scene", "Cancel"))
        {
            return;
        }

        ExecuteUpgrade(false);
    }

    [MenuItem("ASTROBASE/Build Opening Story Scene (Full Rebuild)")]
    public static void BuildSceneInteractive()
    {
        if (!EditorUtility.DisplayDialog(
            "ASTROBASE: Rebuild Opening Story Scene",
            "This will recreate the OpeningStory scene from scratch with all visual upgrades.\n\nContinue?",
            "Rebuild Scene", "Cancel"))
        {
            return;
        }

        ExecuteBuild(false);
    }

    public static void UpgradeSceneBatch()
    {
        ExecuteUpgrade(true);
    }

    public static void BuildSceneBatch()
    {
        ExecuteBuild(true);
    }

    // ===================================================================
    // UPGRADE EXISTING SCENE IN-PLACE
    // ===================================================================

    private static void ExecuteUpgrade(bool isBatch)
    {
        try
        {
            ShowProgress("Verifying project assets...", 0.1f, isBatch);
            EnsureDirectory(MaterialsPath);
            EnsureDirectory(SettingsPath);
            EnsureDirectory(ScenesPath);

            string scenePath = ScenesPath + "/OpeningStory.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                string missingMsg = "Scene file not found at " + scenePath + ".\nPlease run 'Build Opening Story Scene' first.";
                if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Scene Missing", missingMsg, "OK");
                Debug.LogError("[ASTROBASE] " + missingMsg);
                ClearProgress(isBatch);
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            TMP_FontAsset tmpFont = FindTMPFont();

            // 1. Upgrade Materials safely (Never deletes existing assets)
            ShowProgress("Upgrading materials & textures...", 0.25f, isBatch);
            UpgradeMaterials();

            // 2. Load Materials with null verification
            Material earthMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Earth_Mat.mat");
            Material cloudsMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Earth_Clouds_Mat.mat");
            Material atmoMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Atmosphere_Mat.mat");
            Material bodyMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Spacecraft_Body_Mat.mat");
            Material solarMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Solar_Panel_Mat.mat");
            Material cockpitMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Spacecraft_Cockpit_Mat.mat");
            Material engineMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Spacecraft_Engine_Mat.mat");
            Material starMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Star_Particle_Mat.mat");
            Material nebulaMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Space_Nebula_Mat.mat");

            if (earthMat == null)
            {
                Debug.LogError("[ASTROBASE] Failed to load Earth_Mat.mat! Aborting upgrade to protect scene.");
                ClearProgress(isBatch);
                return;
            }

            // 3. Upgrade Earth
            ShowProgress("Upgrading Earth, cloud layers & atmosphere...", 0.4f, isBatch);
            GameObject earthGO = GameObject.Find("Earth");
            if (earthGO == null)
            {
                earthGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                earthGO.name = "Earth";
                Object.DestroyImmediate(earthGO.GetComponent<Collider>());
            }
            earthGO.transform.position = new Vector3(25f, -8f, 80f);
            earthGO.transform.localScale = new Vector3(40f, 40f, 40f);

            Renderer earthRenderer = earthGO.GetComponent<Renderer>();
            if (earthRenderer != null)
            {
                earthRenderer.sharedMaterial = earthMat;
            }

            PlanetRotation earthRot = earthGO.GetComponent<PlanetRotation>();
            if (earthRot == null) earthRot = earthGO.AddComponent<PlanetRotation>();
            earthRot.rotationSpeed = 1.8f;

            // Clouds Layer
            Transform cloudsTrans = earthGO.transform.Find("Clouds");
            GameObject cloudsGO;
            if (cloudsTrans == null)
            {
                cloudsGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cloudsGO.name = "Clouds";
                cloudsGO.transform.SetParent(earthGO.transform, false);
                Object.DestroyImmediate(cloudsGO.GetComponent<Collider>());
            }
            else
            {
                cloudsGO = cloudsTrans.gameObject;
            }
            cloudsGO.transform.localPosition = Vector3.zero;
            cloudsGO.transform.localScale = Vector3.one * 1.012f;
            if (cloudsMat != null) cloudsGO.GetComponent<Renderer>().sharedMaterial = cloudsMat;

            PlanetRotation cloudsRot = cloudsGO.GetComponent<PlanetRotation>();
            if (cloudsRot == null) cloudsRot = cloudsGO.AddComponent<PlanetRotation>();
            cloudsRot.rotationSpeed = 2.8f;

            // Atmosphere Layer
            Transform atmoTrans = earthGO.transform.Find("Atmosphere");
            GameObject atmoGO;
            if (atmoTrans == null)
            {
                atmoGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                atmoGO.name = "Atmosphere";
                atmoGO.transform.SetParent(earthGO.transform, false);
                Object.DestroyImmediate(atmoGO.GetComponent<Collider>());
            }
            else
            {
                atmoGO = atmoTrans.gameObject;
            }
            atmoGO.transform.localPosition = Vector3.zero;
            atmoGO.transform.localScale = Vector3.one * 1.035f;
            if (atmoMat != null) atmoGO.GetComponent<Renderer>().sharedMaterial = atmoMat;

            // 4. Upgrade Spacecraft
            ShowProgress("Upgrading spacecraft & solar arrays...", 0.55f, isBatch);
            GameObject shipGO = GameObject.Find("Spacecraft");
            if (shipGO == null)
            {
                shipGO = new GameObject("Spacecraft");
            }
            shipGO.transform.position = new Vector3(3.2f, 0.4f, 13f);
            shipGO.transform.rotation = Quaternion.Euler(4f, -24f, 6f);

            SpacecraftFloating shipFloat = shipGO.GetComponent<SpacecraftFloating>();
            if (shipFloat == null) shipFloat = shipGO.AddComponent<SpacecraftFloating>();
            shipFloat.floatSpeed = 0.5f;
            shipFloat.floatAmount = 0.12f;

            // Assemble / update modular parts safely
            AssembleShipHierarchy(shipGO, bodyMat, solarMat, cockpitMat, engineMat);

            // 5. Upgrade Starfield and Nebula
            ShowProgress("Enhancing deep space & starfield...", 0.68f, isBatch);
            GameObject starfieldGO = GameObject.Find("Starfield");
            if (starfieldGO == null)
            {
                starfieldGO = new GameObject("Starfield");
            }
            ParticleSystem ps = starfieldGO.GetComponent<ParticleSystem>();
            if (ps == null) ps = starfieldGO.AddComponent<ParticleSystem>();
            if (starMat != null) ConfigureStarfield(ps, starMat);

            // Add or update Nebula Backdrop Quad
            GameObject nebulaGO = GameObject.Find("Nebula_Backdrop");
            if (nebulaGO == null)
            {
                nebulaGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                nebulaGO.name = "Nebula_Backdrop";
                Object.DestroyImmediate(nebulaGO.GetComponent<Collider>());
            }
            nebulaGO.transform.position = new Vector3(-160f, 45f, 380f);
            nebulaGO.transform.rotation = Quaternion.Euler(10f, 25f, -15f);
            nebulaGO.transform.localScale = new Vector3(320f, 260f, 1f);
            if (nebulaMat != null) nebulaGO.GetComponent<Renderer>().sharedMaterial = nebulaMat;

            // 6. Upgrade Camera & Sun
            ShowProgress("Configuring cinematic orbital camera...", 0.78f, isBatch);
            GameObject cameraGO = GameObject.FindWithTag("MainCamera");
            if (cameraGO == null) cameraGO = new GameObject("Main Camera", typeof(Camera));
            cameraGO.tag = "MainCamera";
            Camera cam = cameraGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.008f, 0.016f, 1f);
            cam.fieldOfView = 58f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 2000f;

            CinematicCameraController camCtrl = cameraGO.GetComponent<CinematicCameraController>();
            if (camCtrl == null) camCtrl = cameraGO.AddComponent<CinematicCameraController>();
            camCtrl.earthTarget = earthGO.transform;
            camCtrl.spacecraftTarget = shipGO.transform;
            camCtrl.orbitCenter = earthGO.transform.position;
            camCtrl.orbitDistance = 88f;
            camCtrl.orbitSpeed = 0.75f;
            camCtrl.elevation = 6.2f;

            // Directional Light (Sun)
            GameObject lightGO = GameObject.Find("Directional Light (Sun)");
            if (lightGO == null) lightGO = new GameObject("Directional Light (Sun)", typeof(Light));
            Light sunLight = lightGO.GetComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1f, 0.96f, 0.88f);
            sunLight.intensity = 1.35f;
            sunLight.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(22f, -68f, 0f);

            // Volume profile (Bloom + Vignette)
            CreateOrUpdateVolumeProfile();

            // 7. Upgrade UI Canvas
            ShowProgress("Upgrading futuristic UI & dialogue...", 0.88f, isBatch);
            UpgradeUI(tmpFont);

            // 8. Save
            ShowProgress("Saving upgraded scene...", 0.98f, isBatch);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ClearProgress(isBatch);

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: Visual Upgrade Complete!",
                    "OpeningStory scene has been successfully upgraded!\n\n" +
                    "Upgrades applied:\n" +
                    "  ✓ Realistic Earth with continents, clouds & night lights\n" +
                    "  ✓ Spacecraft with solar wings, dish & glowing engines\n" +
                    "  ✓ Deep Space environment with 4,500 stars and cosmic nebula\n" +
                    "  ✓ Smooth cinematic orbital camera pan\n" +
                    "  ✓ Sleek glassmorphic UI with speaker callsigns & larger buttons\n" +
                    "  ✓ Official 3-stage astronaut dialogue sequence\n\n" +
                    "Press Play ▶ in the Unity Editor to view the upgraded cinematic!",
                    "OK");
            }
            Debug.Log("[ASTROBASE] OpeningStory scene visual upgrade completed successfully.");
        }
        catch (System.Exception ex)
        {
            ClearProgress(isBatch);
            if (!isBatch)
            {
                EditorUtility.DisplayDialog("ASTROBASE: Upgrade Error", ex.Message, "OK");
            }
            Debug.LogError("[ASTROBASE] Upgrade failed: " + ex);
        }
    }

    // ===================================================================
    // FULL REBUILD FROM SCRATCH
    // ===================================================================

    private static void ExecuteBuild(bool isBatch)
    {
        try
        {
            ShowProgress("Rebuilding scene...", 0.1f, isBatch);
            EnsureDirectory(MaterialsPath);
            EnsureDirectory(SettingsPath);
            EnsureDirectory(ScenesPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string scenePath = ScenesPath + "/OpeningStory.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            ExecuteUpgrade(isBatch);
        }
        catch (System.Exception ex)
        {
            ClearProgress(isBatch);
            Debug.LogError("[ASTROBASE] Rebuild failed: " + ex);
        }
    }

    // ===================================================================
    // SAFE MATERIAL CREATION & UPGRADE (No DeleteAsset)
    // ===================================================================

    private static void UpgradeMaterials()
    {
        // Load textures from Assets/Art/NASA_Textures/
        Texture2D dayTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath + "/Earth_Daymap.png");
        Texture2D nightTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath + "/Earth_Night_Lights.png");
        Texture2D cloudsTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath + "/Earth_Clouds.png");
        Texture2D solarTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath + "/Solar_Panel.png");
        Texture2D nebulaTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath + "/Space_Nebula.png");

        // 1. Earth Material (Daymap + Night City Lights Emission)
        Material earthMat = GetOrCreateLitMaterial("Earth_Mat", Color.white, 0.05f, 0.65f);
        if (dayTex != null) earthMat.SetTexture("_BaseMap", dayTex);
        if (nightTex != null)
        {
            earthMat.EnableKeyword("_EMISSION");
            earthMat.SetTexture("_EmissionMap", nightTex);
            earthMat.SetColor("_EmissionColor", new Color(1.2f, 1.1f, 0.9f));
            earthMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.AnyEmissive;
        }
        EditorUtility.SetDirty(earthMat);

        // 2. Clouds Material (Transparent Lit)
        Material cloudsMat = GetOrCreateTransparentMaterial("Earth_Clouds_Mat", new Color(1f, 1f, 1f, 0.85f), Color.black);
        if (cloudsTex != null) cloudsMat.SetTexture("_BaseMap", cloudsTex);
        cloudsMat.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(cloudsMat);

        // 3. Atmosphere Material (Cyan Rim Glow)
        Material atmoMat = GetOrCreateTransparentMaterial("Atmosphere_Mat",
            new Color(0.2f, 0.6f, 1f, 0.08f),
            new Color(0.15f, 0.5f, 1f) * 1.5f);
        EditorUtility.SetDirty(atmoMat);

        // 4. Solar Panel Material
        Material solarMat = GetOrCreateLitMaterial("Solar_Panel_Mat", new Color(0.1f, 0.25f, 0.55f), 0.85f, 0.9f);
        if (solarTex != null) solarMat.SetTexture("_BaseMap", solarTex);
        solarMat.EnableKeyword("_EMISSION");
        solarMat.SetColor("_EmissionColor", new Color(0.04f, 0.08f, 0.2f));
        EditorUtility.SetDirty(solarMat);

        // 5. Spacecraft Body Material (Titanium Hull)
        Material bodyMat = GetOrCreateLitMaterial("Spacecraft_Body_Mat", new Color(0.88f, 0.9f, 0.95f), 0.75f, 0.8f);
        EditorUtility.SetDirty(bodyMat);

        // 6. Cockpit Glass Material
        Material cockpitMat = GetOrCreateLitMaterial("Spacecraft_Cockpit_Mat",
            new Color(0.12f, 0.25f, 0.45f), 0.1f, 0.95f,
            new Color(0.2f, 0.8f, 1f) * 0.9f);
        EditorUtility.SetDirty(cockpitMat);

        // 7. Engine Thruster Material (Electric Cyan Plasma)
        Material engineMat = GetOrCreateLitMaterial("Spacecraft_Engine_Mat",
            new Color(0.15f, 0.16f, 0.2f), 0.9f, 0.4f,
            new Color(0.2f, 0.75f, 1.0f) * 3.5f);
        EditorUtility.SetDirty(engineMat);

        // 8. Star Particle Material
        Material starMat = GetOrCreateParticleMaterial("Star_Particle_Mat", null);
        EditorUtility.SetDirty(starMat);

        // 9. Nebula Billboard Material
        Material nebulaMat = GetOrCreateParticleMaterial("Space_Nebula_Mat", nebulaTex);
        nebulaMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.7f));
        EditorUtility.SetDirty(nebulaMat);

        AssetDatabase.SaveAssets();
    }

    private static Material GetOrCreateLitMaterial(string name, Color baseColor, float metallic, float smoothness, Color? emission = null)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("URP/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        string path = MaterialsPath + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = name;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            if (mat.shader != shader && shader != null)
            {
                mat.shader = shader;
            }
        }

        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);

        if (emission.HasValue && emission.Value != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.AnyEmissive;
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateTransparentMaterial(string name, Color baseColor, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("URP/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        string path = MaterialsPath + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = name;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            if (mat.shader != shader && shader != null)
            {
                mat.shader = shader;
            }
        }

        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Surface", 1f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.renderQueue = (int)RenderQueue.Transparent;

        if (emission != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.AnyEmissive;
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateParticleMaterial(string name, Texture2D texture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        string path = MaterialsPath + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = name;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            if (mat.shader != shader && shader != null)
            {
                mat.shader = shader;
            }
        }

        mat.SetColor("_BaseColor", Color.white);
        if (texture != null)
        {
            mat.SetTexture("_BaseMap", texture);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ===================================================================
    // SPACECRAFT MODULAR ASSEMBLY
    // ===================================================================

    private static void AssembleShipHierarchy(GameObject ship, Material bodyMat, Material solarMat,
        Material cockpitMat, Material engineMat)
    {
        // Clean existing primitive children so re-runs remain clean and idempotent
        for (int i = ship.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(ship.transform.GetChild(i).gameObject);
        }

        // Central Cylindrical Fuselage
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Fuselage";
        body.transform.SetParent(ship.transform, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        body.transform.localScale = new Vector3(0.7f, 1.8f, 0.7f);
        if (bodyMat != null) body.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // Aerodynamic Nose Module
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nose.name = "Nose_Module";
        nose.transform.SetParent(ship.transform, false);
        nose.transform.localPosition = new Vector3(1.7f, 0f, 0f);
        nose.transform.localScale = new Vector3(0.65f, 0.55f, 0.55f);
        if (bodyMat != null) nose.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(nose.GetComponent<Collider>());

        // Cockpit Observation Canopy
        GameObject cockpit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cockpit.name = "Cockpit_Canopy";
        cockpit.transform.SetParent(ship.transform, false);
        cockpit.transform.localPosition = new Vector3(1.3f, 0.28f, 0f);
        cockpit.transform.localScale = new Vector3(0.55f, 0.35f, 0.48f);
        if (cockpitMat != null) cockpit.GetComponent<Renderer>().sharedMaterial = cockpitMat;
        Object.DestroyImmediate(cockpit.GetComponent<Collider>());

        // Left Solar Array Wing
        GameObject leftTruss = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftTruss.name = "SolarWing_Left";
        leftTruss.transform.SetParent(ship.transform, false);
        leftTruss.transform.localPosition = new Vector3(-0.1f, 0f, 1.0f);
        leftTruss.transform.localScale = new Vector3(0.08f, 0.08f, 0.9f);
        if (bodyMat != null) leftTruss.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(leftTruss.GetComponent<Collider>());

        GameObject leftPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftPanel.name = "SolarPanel_Left";
        leftPanel.transform.SetParent(leftTruss.transform, false);
        leftPanel.transform.localPosition = new Vector3(0f, 0f, 1.6f);
        leftPanel.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        leftPanel.transform.localScale = new Vector3(16f, 0.4f, 24f);
        if (solarMat != null) leftPanel.GetComponent<Renderer>().sharedMaterial = solarMat;
        Object.DestroyImmediate(leftPanel.GetComponent<Collider>());

        // Right Solar Array Wing
        GameObject rightTruss = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightTruss.name = "SolarWing_Right";
        rightTruss.transform.SetParent(ship.transform, false);
        rightTruss.transform.localPosition = new Vector3(-0.1f, 0f, -1.0f);
        rightTruss.transform.localScale = new Vector3(0.08f, 0.08f, 0.9f);
        if (bodyMat != null) rightTruss.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(rightTruss.GetComponent<Collider>());

        GameObject rightPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightPanel.name = "SolarPanel_Right";
        rightPanel.transform.SetParent(rightTruss.transform, false);
        rightPanel.transform.localPosition = new Vector3(0f, 0f, -1.6f);
        rightPanel.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
        rightPanel.transform.localScale = new Vector3(16f, 0.4f, 24f);
        if (solarMat != null) rightPanel.GetComponent<Renderer>().sharedMaterial = solarMat;
        Object.DestroyImmediate(rightPanel.GetComponent<Collider>());

        // High-Gain Parabolic Communications Dish
        GameObject antennaMount = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        antennaMount.name = "Antenna_Mast";
        antennaMount.transform.SetParent(ship.transform, false);
        antennaMount.transform.localPosition = new Vector3(-0.6f, 0.55f, 0f);
        antennaMount.transform.localScale = new Vector3(0.06f, 0.25f, 0.06f);
        if (bodyMat != null) antennaMount.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(antennaMount.GetComponent<Collider>());

        GameObject dish = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dish.name = "Communication_Dish";
        dish.transform.SetParent(antennaMount.transform, false);
        dish.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        dish.transform.localRotation = Quaternion.Euler(-30f, 45f, 0f);
        dish.transform.localScale = new Vector3(7f, 1.5f, 7f);
        if (bodyMat != null) dish.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(dish.GetComponent<Collider>());

        // Dual Plasma Thruster Engines
        GameObject engineL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        engineL.name = "Engine_Port";
        engineL.transform.SetParent(ship.transform, false);
        engineL.transform.localPosition = new Vector3(-1.85f, 0.1f, 0.32f);
        engineL.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        engineL.transform.localScale = new Vector3(0.36f, 0.45f, 0.36f);
        if (engineMat != null) engineL.GetComponent<Renderer>().sharedMaterial = engineMat;
        Object.DestroyImmediate(engineL.GetComponent<Collider>());

        GameObject engineR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        engineR.name = "Engine_Starboard";
        engineR.transform.SetParent(ship.transform, false);
        engineR.transform.localPosition = new Vector3(-1.85f, 0.1f, -0.32f);
        engineR.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        engineR.transform.localScale = new Vector3(0.36f, 0.45f, 0.36f);
        if (engineMat != null) engineR.GetComponent<Renderer>().sharedMaterial = engineMat;
        Object.DestroyImmediate(engineR.GetComponent<Collider>());

        // Realtime Thruster Point Light
        GameObject plumeLightGO = new GameObject("Engine_Plume_Light");
        plumeLightGO.transform.SetParent(ship.transform, false);
        plumeLightGO.transform.localPosition = new Vector3(-2.2f, 0.1f, 0f);
        Light plumeLight = plumeLightGO.AddComponent<Light>();
        plumeLight.type = LightType.Point;
        plumeLight.color = new Color(0.25f, 0.8f, 1f);
        plumeLight.intensity = 2.4f;
        plumeLight.range = 5.0f;
    }

    // ===================================================================
    // STARFIELD CONFIGURATION
    // ===================================================================

    private static void ConfigureStarfield(ParticleSystem ps, Material starMat)
    {
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 1.6f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.92f, 1f, 0.95f),
            new Color(1f, 0.96f, 0.85f, 0.95f));
        main.startLifetime = 9999f;
        main.maxParticles = 4500;
        main.loop = false;
        main.playOnAwake = true;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 4500)
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 900f;
        shape.radiusThickness = 1f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = starMat;
    }

    // ===================================================================
    // FUTURISTIC UI UPGRADE
    // ===================================================================

    private static void UpgradeUI(TMP_FontAsset font)
    {
        GameObject canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("Canvas");
            canvasGO.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject eventSystemGO = GameObject.Find("EventSystem");
        if (eventSystemGO == null)
        {
            eventSystemGO = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        // 1. Title Panel
        Transform titlePanelTrans = canvasGO.transform.Find("TitlePanel");
        GameObject titlePanel = titlePanelTrans != null ? titlePanelTrans.gameObject : CreateUIObject("TitlePanel", canvasGO.transform);
        RectTransform titleRect = titlePanel.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.68f);
        titleRect.anchorMax = new Vector2(1f, 0.96f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        TitleFadeIn fadeIn = titlePanel.GetComponent<TitleFadeIn>();
        if (fadeIn == null) fadeIn = titlePanel.AddComponent<TitleFadeIn>();
        fadeIn.delay = 0.8f;
        fadeIn.fadeDuration = 2.2f;

        // Title Text
        Transform titleTextTrans = titlePanel.transform.Find("TitleText");
        GameObject titleTextGO = titleTextTrans != null ? titleTextTrans.gameObject : CreateUIObject("TitleText", titlePanel.transform);
        RectTransform ttRect = titleTextGO.GetComponent<RectTransform>();
        ttRect.anchorMin = new Vector2(0f, 0.35f);
        ttRect.anchorMax = Vector2.one;
        ttRect.offsetMin = Vector2.zero;
        ttRect.offsetMax = Vector2.zero;

        TextMeshProUGUI titleText = titleTextGO.GetComponent<TextMeshProUGUI>();
        if (titleText == null) titleText = titleTextGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "ASTROBASE: BEYOND EARTH";
        if (font != null) titleText.font = font;
        titleText.fontSize = 70;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.characterSpacing = 4f;

        // Subtitle Text
        Transform subtitleTrans = titlePanel.transform.Find("SubtitleText");
        GameObject subtitleGO = subtitleTrans != null ? subtitleTrans.gameObject : CreateUIObject("SubtitleText", titlePanel.transform);
        RectTransform stRect = subtitleGO.GetComponent<RectTransform>();
        stRect.anchorMin = Vector2.zero;
        stRect.anchorMax = new Vector2(1f, 0.35f);
        stRect.offsetMin = Vector2.zero;
        stRect.offsetMax = Vector2.zero;

        TextMeshProUGUI subtitleText = subtitleGO.GetComponent<TextMeshProUGUI>();
        if (subtitleText == null) subtitleText = subtitleGO.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "HUMAN EXPEDITION ONE // SYSTEM EXPLORATION PROGRAM";
        if (font != null) subtitleText.font = font;
        subtitleText.fontSize = 24;
        subtitleText.color = new Color(0.45f, 0.85f, 1f, 0.9f);
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.characterSpacing = 2.5f;

        // 2. Dialogue Panel (Glassmorphic)
        Transform dialoguePanelTrans = canvasGO.transform.Find("DialoguePanel");
        GameObject dialoguePanel = dialoguePanelTrans != null ? dialoguePanelTrans.gameObject : CreateUIObject("DialoguePanel", canvasGO.transform);
        RectTransform dpRect = dialoguePanel.GetComponent<RectTransform>();
        dpRect.anchorMin = new Vector2(0.08f, 0.04f);
        dpRect.anchorMax = new Vector2(0.92f, 0.25f);
        dpRect.offsetMin = Vector2.zero;
        dpRect.offsetMax = Vector2.zero;

        Image dpImage = dialoguePanel.GetComponent<Image>();
        if (dpImage == null) dpImage = dialoguePanel.AddComponent<Image>();
        dpImage.color = new Color(0.015f, 0.04f, 0.09f, 0.88f);

        // Speaker Header
        Transform speakerTrans = dialoguePanel.transform.Find("SpeakerText");
        GameObject speakerGO = speakerTrans != null ? speakerTrans.gameObject : CreateUIObject("SpeakerText", dialoguePanel.transform);
        RectTransform spRect = speakerGO.GetComponent<RectTransform>();
        spRect.anchorMin = new Vector2(0.035f, 0.72f);
        spRect.anchorMax = new Vector2(0.75f, 0.95f);
        spRect.offsetMin = Vector2.zero;
        spRect.offsetMax = Vector2.zero;

        TextMeshProUGUI speakerText = speakerGO.GetComponent<TextMeshProUGUI>();
        if (speakerText == null) speakerText = speakerGO.AddComponent<TextMeshProUGUI>();
        speakerText.text = "COMMANDER // HORIZON-1";
        if (font != null) speakerText.font = font;
        speakerText.fontSize = 24;
        speakerText.color = new Color(0.35f, 0.95f, 1f);
        speakerText.fontStyle = FontStyles.Bold;
        speakerText.characterSpacing = 2f;

        // Dialogue Body Text
        Transform dialogueTextTrans = dialoguePanel.transform.Find("DialogueText");
        GameObject dialogueTextGO = dialogueTextTrans != null ? dialogueTextTrans.gameObject : CreateUIObject("DialogueText", dialoguePanel.transform);
        RectTransform dtRect = dialogueTextGO.GetComponent<RectTransform>();
        dtRect.anchorMin = new Vector2(0.035f, 0.20f);
        dtRect.anchorMax = new Vector2(0.78f, 0.70f);
        dtRect.offsetMin = Vector2.zero;
        dtRect.offsetMax = Vector2.zero;

        TextMeshProUGUI dialogueText = dialogueTextGO.GetComponent<TextMeshProUGUI>();
        if (dialogueText == null) dialogueText = dialogueTextGO.AddComponent<TextMeshProUGUI>();
        dialogueText.text = "";
        if (font != null) dialogueText.font = font;
        dialogueText.fontSize = 30;
        dialogueText.color = new Color(0.92f, 0.96f, 1f);
        dialogueText.textWrappingMode = TextWrappingModes.Normal;

        // 3. Continue Button (Enlarged & Prominent)
        Transform continueBtnTrans = dialoguePanel.transform.Find("ContinueButton");
        GameObject continueBtn = continueBtnTrans != null ? continueBtnTrans.gameObject :
            CreateButton("ContinueButton", "CONTINUE >", font, dialoguePanel.transform,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-20f, 16f), new Vector2(210f, 54f),
                new Color(0.12f, 0.42f, 0.82f, 0.95f));

        RectTransform cRect = continueBtn.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(1f, 0f);
        cRect.anchorMax = new Vector2(1f, 0f);
        cRect.pivot = new Vector2(1f, 0f);
        cRect.anchoredPosition = new Vector2(-20f, 16f);
        cRect.sizeDelta = new Vector2(210f, 54f);

        // 4. Skip Button (Enlarged)
        Transform skipBtnTrans = canvasGO.transform.Find("SkipButton");
        GameObject skipBtn = skipBtnTrans != null ? skipBtnTrans.gameObject :
            CreateButton("SkipButton", "SKIP >>", font, canvasGO.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -24f), new Vector2(160f, 48f),
                new Color(0.22f, 0.26f, 0.35f, 0.8f));

        RectTransform sRect = skipBtn.GetComponent<RectTransform>();
        sRect.anchorMin = new Vector2(1f, 1f);
        sRect.anchorMax = new Vector2(1f, 1f);
        sRect.pivot = new Vector2(1f, 1f);
        sRect.anchoredPosition = new Vector2(-24f, -24f);
        sRect.sizeDelta = new Vector2(160f, 48f);

        // 5. Connect DialogueTypewriter
        DialogueTypewriter typewriter = dialoguePanel.GetComponent<DialogueTypewriter>();
        if (typewriter == null) typewriter = dialoguePanel.AddComponent<DialogueTypewriter>();
        typewriter.speakerText = speakerText;
        typewriter.dialogueText = dialogueText;
        typewriter.continueButton = continueBtn.GetComponent<Button>();
        typewriter.skipButton = skipBtn.GetComponent<Button>();
        typewriter.delayBeforeStart = 3.2f;
        typewriter.typingSpeed = 0.032f;
        typewriter.dialogueEntries = new DialogueTypewriter.DialogueEntry[]
        {
            new DialogueTypewriter.DialogueEntry(
                "COMMANDER // HORIZON-1",
                "Welcome aboard, astronaut. Humanity is preparing for its next great journey.",
                new Color(0.35f, 0.95f, 1.0f)
            ),
            new DialogueTypewriter.DialogueEntry(
                "MISSION CONTROL // HOUSTON",
                "Our destination is beyond Earth. Three extraordinary missions await us.",
                new Color(1.0f, 0.82f, 0.25f)
            ),
            new DialogueTypewriter.DialogueEntry(
                "COMMANDER // HORIZON-1",
                "The Moon, the Red Planet, and the mysteries of our Sun. Your decisions will determine the success of every mission.",
                new Color(0.35f, 0.95f, 1.0f)
            )
        };
    }

    // ===================================================================
    // VOLUME PROFILE
    // ===================================================================

    private static VolumeProfile CreateOrUpdateVolumeProfile()
    {
        string path = SettingsPath + "/OpeningStoryVolumeProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        // Bloom
        Bloom bloom;
        if (!profile.TryGet(out bloom))
        {
            bloom = profile.Add<Bloom>(false);
            bloom.name = "Bloom";
            AssetDatabase.AddObjectToAsset(bloom, profile);
        }
        bloom.intensity.Override(1.1f);
        bloom.threshold.Override(0.85f);
        bloom.scatter.Override(0.7f);

        // Vignette
        Vignette vignette;
        if (!profile.TryGet(out vignette))
        {
            vignette = profile.Add<Vignette>(false);
            vignette.name = "Vignette";
            AssetDatabase.AddObjectToAsset(vignette, profile);
        }
        vignette.intensity.Override(0.38f);
        vignette.smoothness.Override(0.35f);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        // Assign to Volume in scene
        GameObject volumeGO = GameObject.Find("Post Processing Volume");
        if (volumeGO == null) volumeGO = new GameObject("Post Processing Volume", typeof(Volume));
        Volume volume = volumeGO.GetComponent<Volume>();
        volume.isGlobal = true;
        volume.profile = profile;
        volume.priority = 1f;

        return profile;
    }

    // ===================================================================
    // UI HELPERS
    // ===================================================================

    static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static GameObject CreateButton(string name, string label, TMP_FontAsset font,
        Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);

        RectTransform rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image btnImage = btnGO.AddComponent<Image>();
        btnImage.color = bgColor;

        Button btn = btnGO.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = bgColor;
        Color highlight = bgColor * 1.35f;
        highlight.a = Mathf.Min(bgColor.a + 0.15f, 1f);
        colors.highlightedColor = highlight;
        Color pressed = bgColor * 0.7f;
        pressed.a = bgColor.a;
        colors.pressedColor = pressed;
        colors.selectedColor = bgColor;
        btn.colors = colors;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 4f);
        textRect.offsetMax = new Vector2(-10f, -4f);

        TextMeshProUGUI btnText = textGO.AddComponent<TextMeshProUGUI>();
        btnText.text = label;
        if (font != null) btnText.font = font;
        btnText.fontSize = 22;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontStyle = FontStyles.Bold;

        return btnGO;
    }

    static void ShowProgress(string message, float progress, bool isBatch)
    {
        if (!isBatch)
        {
            EditorUtility.DisplayProgressBar("ASTROBASE", message, progress);
        }
    }

    static void ClearProgress(bool isBatch)
    {
        if (!isBatch)
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static TMP_FontAsset FindTMPFont()
    {
        if (TMP_Settings.defaultFontAsset != null)
            return TMP_Settings.defaultFontAsset;

        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) return font;
        }

        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    static void EnsureDirectory(string path)
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

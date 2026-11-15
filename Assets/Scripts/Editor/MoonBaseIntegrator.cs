using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Automation and integration tool for ASTROBASE:
/// Integrates the authentic 3D NASA Moon Base model (moon_base.glb/obj)
/// into both LunarLanding.unity and LunarExploration.unity.
/// </summary>
public class MoonBaseIntegrator
{
    private const string ArtPath = "Assets/Art/MoonBase";
    private const string TexturesPath = "Assets/Art/MoonBase/Textures";
    private const string MaterialsPath = "Assets/Materials/MoonBase";
    private const string PrefabsPath = "Assets/Prefabs/Environment";
    private const string ScenesPath = "Assets/Scenes";

    [MenuItem("ASTROBASE/Moon Base/Integrate Moon Base Into Scenes")]
    public static void IntegrateMoonBaseInteractive()
    {
        IntegrateMoonBase(false);
    }

    public static void IntegrateMoonBaseBatch()
    {
        IntegrateMoonBase(true);
    }

    [InitializeOnLoadMethod]
    private static void AutoIntegrateOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            string prefabPath = PrefabsPath + "/MoonBase_Complex.prefab";
            if (!File.Exists(prefabPath))
            {
                Debug.Log("[ASTROBASE] Initializing Moon Base integration...");
                IntegrateMoonBase(true);
            }
        };
    }

    public static void IntegrateMoonBase(bool isBatch)
    {
        try
        {
            EnsureDirectory(MaterialsPath);
            EnsureDirectory(PrefabsPath);

            // 1. Configure texture importers
            ConfigureTextures();

            // 2. Configure model importer
            string objPath = ArtPath + "/moon_base.obj";
            if (!File.Exists(objPath))
            {
                string msg = "moon_base.obj not found at " + objPath;
                if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Missing Model", msg, "OK");
                Debug.LogError("[ASTROBASE] " + msg);
                return;
            }
            ConfigureModelImporter(objPath);

            // 3. Create URP Lit materials
            Dictionary<string, Material> mats = CreateMoonBaseMaterials();

            // 4. Create or update MoonBase_Complex Prefab
            GameObject moonBasePrefab = CreateOrUpdateMoonBasePrefab(objPath, mats);

            // 5. Integrate into LunarLanding.unity
            IntegrateIntoLandingScene(moonBasePrefab);

            // 6. Integrate into LunarExploration.unity
            IntegrateIntoExplorationScene(moonBasePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: Moon Base Integrated!",
                    "Successfully integrated authentic NASA Moon Base into:\n" +
                    "  • Assets/Scenes/LunarLanding.unity\n" +
                    "  • Assets/Scenes/LunarExploration.unity\n\n" +
                    "  ✓ 4 Pressurized biodomes with structural frames & tinted viewing glass\n" +
                    "  ✓ Interconnecting transit corridors & NASA research laboratory module\n" +
                    "  ✓ Cryogenic storage tanks & lunar exploration lander pad\n" +
                    "  ✓ Realistic contoured lunar terrain & perimeter boulder fields\n" +
                    "  ✓ Full physical mesh colliders for exploration on foot\n" +
                    "  ✓ PhotoCapture landmark target recognition for Moon Base\n\n" +
                    "Press Play ▶ in LunarLanding or LunarExploration to inspect!",
                    "OK");
            }
            Debug.Log("[ASTROBASE] Moon Base integrated successfully into Landing and Exploration scenes.");
        }
        catch (System.Exception ex)
        {
            if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", ex.Message, "OK");
            Debug.LogError("[ASTROBASE] Moon Base integration failed: " + ex);
        }
    }

    public static void ConfigureTextures()
    {
        string[] pngFiles = Directory.GetFiles(TexturesPath, "*.png");
        foreach (string file in pngFiles)
        {
            string unityPath = file.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.maxTextureSize = 2048;
                importer.mipmapEnabled = true;

                // Terrain texture needs repeat wrap mode for 12x12 tiling
                if (unityPath.Contains("texture_0"))
                {
                    importer.wrapMode = TextureWrapMode.Repeat;
                }
                else
                {
                    importer.wrapMode = TextureWrapMode.Clamp;
                }

                importer.SaveAndReimport();
            }
        }
    }

    private static void ConfigureModelImporter(string objPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(objPath) as ModelImporter;
        if (importer != null)
        {
            importer.globalScale = 1.0f;
            importer.importNormals = ModelImporterNormals.Import;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.addCollider = false;
            importer.SaveAndReimport();
        }
    }

    public static Dictionary<string, Material> CreateMoonBaseMaterials()
    {
        Dictionary<string, Material> mats = new Dictionary<string, Material>();

        mats["Mat_0_Terrain"] = GetOrCreateMaterial("Mat_0_Terrain", "texture_0.png", Color.white, 0.05f, 0.15f, false);
        mats["Mat_1_DarkFrame"] = GetOrCreateMaterial("Mat_1_DarkFrame", null, new Color(0.044f, 0.051f, 0.065f, 1f), 0.6f, 0.50f, false);
        mats["Mat_2_DomeGlass"] = GetOrCreateMaterial("Mat_2_DomeGlass", null, new Color(0.687f, 0.839f, 1.0f, 0.35f), 0.1f, 0.95f, true);
        mats["Mat_3_MetalConnectors"] = GetOrCreateMaterial("Mat_3_MetalConnectors", null, new Color(0.262f, 0.305f, 0.371f, 1f), 0.75f, 0.65f, false);
        mats["Mat_4_ThermalInsulation"] = GetOrCreateMaterial("Mat_4_ThermalInsulation", null, new Color(0.807f, 0.738f, 0.638f, 1f), 0.10f, 0.30f, false);
        mats["Mat_5_GoldFoil"] = GetOrCreateMaterial("Mat_5_GoldFoil", null, new Color(1.0f, 0.871f, 0.723f, 1f), 0.90f, 0.70f, false);
        mats["Mat_6_AlloyPanels"] = GetOrCreateMaterial("Mat_6_AlloyPanels", null, new Color(0.578f, 0.610f, 0.658f, 1f), 0.40f, 0.50f, false);
        mats["Mat_7_SolarDecal"] = GetOrCreateMaterial("Mat_7_SolarDecal", "texture_1.png", Color.white, 0.70f, 0.80f, false);
        mats["Mat_8_NASALogo"] = GetOrCreateMaterial("Mat_8_NASALogo", "texture_2.png", Color.white, 0.20f, 0.45f, false);
        mats["Mat_9_WhiteHabitat"] = GetOrCreateMaterial("Mat_9_WhiteHabitat", null, new Color(0.776f, 0.807f, 0.855f, 1f), 0.10f, 0.35f, false);
        mats["Mat_10_VisorDark"] = GetOrCreateMaterial("Mat_10_VisorDark", null, new Color(0.01f, 0.01f, 0.01f, 1f), 0.95f, 0.90f, false);
        mats["Mat_11_LunarRocks"] = GetOrCreateMaterial("Mat_11_LunarRocks", null, new Color(0.080f, 0.089f, 0.105f, 1f), 0.05f, 0.10f, false);

        return mats;
    }

    private static Material GetOrCreateMaterial(string matName, string textureFileName, Color baseColor, float metallic, float smoothness, bool transparent)
    {
        string assetPath = MaterialsPath + "/" + matName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, assetPath);
        }

        mat.SetColor("_BaseColor", baseColor);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);

        if (transparent)
        {
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f); // Alpha blend
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        else
        {
            mat.SetFloat("_Surface", 0f); // Opaque
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = 2000;
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        if (!string.IsNullOrEmpty(textureFileName))
        {
            string texPath = TexturesPath + "/" + textureFileName;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
            }
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static GameObject CreateOrUpdateMoonBasePrefab(string objPath, Dictionary<string, Material> mats)
    {
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
        if (modelAsset == null)
        {
            throw new System.Exception("Cannot load model asset at " + objPath);
        }

        GameObject instance = (GameObject)Object.Instantiate(modelAsset);
        instance.name = "MoonBase_Complex";

        // Assign materials and add colliders
        MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer r in renderers)
        {
            string goName = r.gameObject.name;
            Material targetMat = FindMatchingMaterial(goName, mats);
            if (targetMat != null)
            {
                r.sharedMaterial = targetMat;
            }

            // Add physical MeshColliders for terrain, rocks, and structures so the astronaut
            // and lander interact realistically with the authentic 3D lunar base environment
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                if (goName.Contains("Lunar_Terrain") || goName.Contains("Lunar_Rocks") ||
                    goName.Contains("Dome") || goName.Contains("NASA_Module") ||
                    goName.Contains("Corridor") || goName.Contains("Storage_Tanks") ||
                    goName.Contains("Lunar_Lander"))
                {
                    MeshCollider mc = r.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
        }

        string prefabPath = PrefabsPath + "/MoonBase_Complex.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);

        Debug.Log("[ASTROBASE] Saved MoonBase_Complex.prefab at " + prefabPath);
        return prefab;
    }

    private static Material FindMatchingMaterial(string goName, Dictionary<string, Material> mats)
    {
        if (goName.Contains("Lunar_Terrain")) return mats["Mat_0_Terrain"];
        if (goName.Contains("Lunar_Rocks")) return mats["Mat_11_LunarRocks"];

        // Domes
        if (goName.Contains("Dome"))
        {
            if (goName.Contains("node_1") || goName.Contains("node_8") || goName.Contains("node_15") || goName.Contains("node_22"))
                return mats["Mat_1_DarkFrame"];
            if (goName.Contains("node_2") || goName.Contains("node_9") || goName.Contains("node_16") || goName.Contains("node_23"))
                return mats["Mat_2_DomeGlass"];
            if (goName.Contains("node_3") || goName.Contains("node_10") || goName.Contains("node_17") || goName.Contains("node_24"))
                return mats["Mat_3_MetalConnectors"];
            if (goName.Contains("node_4") || goName.Contains("node_11") || goName.Contains("node_18") || goName.Contains("node_25"))
                return mats["Mat_4_ThermalInsulation"];
            if (goName.Contains("node_5") || goName.Contains("node_12") || goName.Contains("node_19") || goName.Contains("node_26"))
                return mats["Mat_5_GoldFoil"];
        }

        // Corridors
        if (goName.Contains("Corridor"))
        {
            if (goName.Contains("node_31") || goName.Contains("node_36")) return mats["Mat_1_DarkFrame"];
            if (goName.Contains("node_37")) return mats["Mat_7_SolarDecal"];
            if (goName.Contains("node_32") || goName.Contains("node_38")) return mats["Mat_5_GoldFoil"];
            return mats["Mat_6_AlloyPanels"];
        }

        // NASA Module
        if (goName.Contains("NASA_Module"))
        {
            if (goName.Contains("node_40")) return mats["Mat_8_NASALogo"];
            if (goName.Contains("node_41")) return mats["Mat_1_DarkFrame"];
            if (goName.Contains("node_42")) return mats["Mat_3_MetalConnectors"];
            return mats["Mat_6_AlloyPanels"];
        }

        // Storage Tanks
        if (goName.Contains("Storage_Tanks"))
        {
            return mats["Mat_6_AlloyPanels"];
        }

        // Lander
        if (goName.Contains("Lunar_Lander"))
        {
            if (goName.Contains("node_54")) return mats["Mat_1_DarkFrame"];
            return mats["Mat_6_AlloyPanels"];
        }

        // Astronauts
        if (goName.Contains("Astronaut"))
        {
            if (goName.Contains("node_57") || goName.Contains("node_60")) return mats["Mat_10_VisorDark"];
            return mats["Mat_9_WhiteHabitat"];
        }

        return mats["Mat_6_AlloyPanels"];
    }

    public static void IntegrateIntoLandingScene(GameObject moonBasePrefab)
    {
        string scenePath = ScenesPath + "/LunarLanding.unity";
        if (!File.Exists(scenePath)) return;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Remove old MoonBase instance if present
        GameObject oldBase = GameObject.Find("MoonBase_Complex");
        if (oldBase != null) Object.DestroyImmediate(oldBase);

        // Instantiate Moon Base Complex at ground origin
        GameObject baseInstance = (GameObject)PrefabUtility.InstantiatePrefab(moonBasePrefab);
        baseInstance.name = "MoonBase_Complex";
        baseInstance.transform.position = Vector3.zero;
        baseInstance.transform.rotation = Quaternion.identity;

        // Reposition Touchdown Pad to designated base landing sector
        GameObject padGO = GameObject.Find("Touchdown_Pad");
        if (padGO != null)
        {
            padGO.transform.position = new Vector3(-14.2f, 0.05f, -7.6f);
        }

        // Adjust lander start position so descent faces the base
        GameObject landerGO = GameObject.Find("Lunar_Lander");
        if (landerGO != null)
        {
            landerGO.transform.position = new Vector3(-14.2f, 280f, -7.6f);
        }

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[ASTROBASE] Integrated MoonBase_Complex into LunarLanding.unity");
    }

    public static void IntegrateIntoExplorationScene(GameObject moonBasePrefab)
    {
        string scenePath = ScenesPath + "/LunarExploration.unity";
        if (!File.Exists(scenePath)) return;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Remove old MoonBase instance if present
        GameObject oldBase = GameObject.Find("MoonBase_Complex");
        if (oldBase != null) Object.DestroyImmediate(oldBase);

        // Instantiate Moon Base Complex at ground origin
        GameObject baseInstance = (GameObject)PrefabUtility.InstantiatePrefab(moonBasePrefab);
        baseInstance.name = "MoonBase_Complex";
        baseInstance.transform.position = Vector3.zero;
        baseInstance.transform.rotation = Quaternion.identity;

        // Position player astronaut outside the NASA Module airlock
        GameObject astronautGO = GameObject.Find("Astronaut_Player");
        if (astronautGO != null)
        {
            CharacterController cc = astronautGO.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Outside NASA module airlock
            astronautGO.transform.position = new Vector3(14.3f, 0.15f, -4.5f);
            astronautGO.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            if (cc != null) cc.enabled = true;
        }

        // Reposition Lander Return Zone to authentic lander pad
        GameObject returnZoneGO = GameObject.Find("Lander_Return_Zone");
        if (returnZoneGO != null)
        {
            returnZoneGO.transform.position = new Vector3(-14.2f, 0.5f, -7.6f);
        }

        // Update Parked Lander position if present
        GameObject parkedLander = GameObject.Find("Lunar_Lander_Base");
        if (parkedLander != null)
        {
            parkedLander.transform.position = new Vector3(-14.2f, 0.02f, -7.6f);
        }

        // Reposition Samples to interesting Moon Base geological sites
        GameObject s1 = GameObject.Find("Sample_01_RegolithCanister");
        if (s1 != null) s1.transform.position = new Vector3(2.5f, 0.4f, 4.0f);

        GameObject s2 = GameObject.Find("Sample_02_VolatileIce");
        if (s2 != null) s2.transform.position = new Vector3(-22.0f, 0.4f, 6.0f);

        GameObject s3 = GameObject.Find("Sample_03_BasaltBreccia");
        if (s3 != null) s3.transform.position = new Vector3(24.0f, 0.4f, -12.0f);

        // Reposition Anomaly near perimeter crater fissure
        GameObject anomalyGO = GameObject.Find("Anomaly_SubsurfaceVolatiles");
        if (anomalyGO != null) anomalyGO.transform.position = new Vector3(0.0f, 1.0f, 18.0f);

        // Connect Moon Base Habitat Target to PhotoCaptureSystem
        if (astronautGO != null)
        {
            PhotoCaptureSystem photoSystem = astronautGO.GetComponent<PhotoCaptureSystem>();
            if (photoSystem != null)
            {
                // Find central habitat dome
                Transform centerDome = baseInstance.transform.Find("Dome_Front_Center_node_8");
                if (centerDome == null) centerDome = baseInstance.transform;
                photoSystem.moonBaseHabitatTarget = centerDome;
            }
        }

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[ASTROBASE] Integrated MoonBase_Complex into LunarExploration.unity");
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}

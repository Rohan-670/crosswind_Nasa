using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Integrator for the Space Center Realistic Launch Facility model:
/// sets up PBR materials, textures, and prefabs for the launch facility grounds,
/// gantry towers, and the launch vehicle on the pad.
/// </summary>
public class SpaceCenterIntegrator
{
    private const string EnvArtPath = "Assets/Art/Environment";
    private const string TexPath = "Assets/Art/Environment/SpaceCenter_Textures";
    private const string MatPath = "Assets/Materials/SpaceCenter";
    private const string PrefabPath = "Assets/Prefabs/Environment";
    private const string SpacecraftPrefabPath = "Assets/Prefabs/Spacecraft";

    [MenuItem("ASTROBASE/Space Center/Integrate Space Center Launch Facility")]
    public static void IntegrateFacilityInteractive()
    {
        ExecuteIntegration(false);
    }

    public static void IntegrateFacilityBatch()
    {
        ExecuteIntegration(true);
    }

    public static void ExecuteIntegration(bool isBatch)
    {
        try
        {
            EnsureDirectory(MatPath);
            EnsureDirectory(PrefabPath);
            EnsureDirectory(SpacecraftPrefabPath);

            // 1. Configure Textures
            ConfigureTextures();

            // 2. Create Materials
            Dictionary<string, Material> facilityMats = CreateFacilityMaterials();

            // 3. Create Prefab for Space Center Facility
            string facilityObjPath = $"{EnvArtPath}/SpaceCenter_Facility.obj";
            if (File.Exists(facilityObjPath))
            {
                CreateFacilityPrefab(facilityObjPath, facilityMats);
            }

            // 4. Create Prefab for Launch Vehicle
            string rocketObjPath = $"{EnvArtPath}/SpaceCenter_Rocket.obj";
            if (File.Exists(rocketObjPath))
            {
                CreateRocketPrefab(rocketObjPath, facilityMats);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: Space Center Integrated",
                    "Space Center Realistic Launch Facility successfully integrated!\n\n" +
                    "  ✓ Ground terrain, launch pad, and roads\n" +
                    "  ✓ Red gantry umbilical service tower and lightning masts\n" +
                    "  ✓ Hangars, control facilities, and flame trenches\n" +
                    "  ✓ Orbital launch vehicle stationed on the pad\n\n" +
                    "Prefabs created at:\n" +
                    $"  • {PrefabPath}/SpaceCenter_LaunchFacility.prefab\n" +
                    $"  • {SpacecraftPrefabPath}/SpaceCenter_LaunchVehicle.prefab",
                    "OK");
            }
            Debug.Log("[ASTROBASE] Space Center Launch Facility integrated successfully.");
        }
        catch (System.Exception ex)
        {
            if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", ex.Message, "OK");
            Debug.LogError("[ASTROBASE] Space Center integration failed: " + ex);
        }
    }

    private static void ConfigureTextures()
    {
        if (!Directory.Exists(TexPath)) return;

        string[] texFiles = Directory.GetFiles(TexPath, "*.*");
        foreach (string file in texFiles)
        {
            if (file.EndsWith(".meta")) continue;

            string assetPath = file.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                if (assetPath.Contains("Tex_8")) // normal map
                {
                    importer.textureType = TextureImporterType.NormalMap;
                }
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
        }
    }

    private static Dictionary<string, Material> CreateFacilityMaterials()
    {
        Dictionary<string, Material> mats = new Dictionary<string, Material>();
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // 1. Material_29: Base ground terrain
        mats["Material_29"] = GetOrCreateMaterial("SpaceCenter_Terrain_Mat", litShader,
            new Color(0.85f, 0.85f, 0.82f), 0.05f, 0.25f, LoadTexture("SpaceCenter_Tex_0.jpg"));

        // 2. Material_22: Concrete roads & launch pad apron
        mats["Material_22"] = GetOrCreateMaterial("SpaceCenter_Roads_Mat", litShader,
            new Color(0.88f, 0.88f, 0.90f), 0.10f, 0.35f, LoadTexture("SpaceCenter_Tex_1.jpg"));

        // 3. Material_68: Gantry umbilical tower, cranes & lightning masts (Iconic NASA Red)
        mats["Material_68"] = GetOrCreateMaterial("SpaceCenter_Gantry_Mat", litShader,
            new Color(0.85f, 0.22f, 0.18f), 0.35f, 0.50f, null);

        // 4. Material_35: Facility buildings & hangars
        mats["Material_35"] = GetOrCreateMaterial("SpaceCenter_Buildings_Mat", litShader,
            new Color(0.90f, 0.90f, 0.92f), 0.15f, 0.40f, LoadTexture("SpaceCenter_Tex_2.jpg"));

        // 5. Rocket Materials
        mats["Nozzle_Ring"] = GetOrCreateMaterial("SpaceCenter_NozzleRing_Mat", litShader,
            new Color(0.35f, 0.35f, 0.38f), 0.70f, 0.60f, LoadTexture("SpaceCenter_Tex_3.png"));

        mats["Nozzle"] = GetOrCreateMaterial("SpaceCenter_Nozzle_Mat", litShader,
            new Color(0.28f, 0.28f, 0.30f), 0.80f, 0.65f, LoadTexture("SpaceCenter_Tex_4.png"));

        mats["Booster"] = GetOrCreateMaterial("SpaceCenter_Booster_Mat", litShader,
            new Color(0.92f, 0.92f, 0.94f), 0.20f, 0.45f, LoadTexture("SpaceCenter_Tex_5.png"));

        mats["Side_Booster"] = GetOrCreateMaterial("SpaceCenter_SideBooster_Mat", litShader,
            new Color(0.92f, 0.92f, 0.94f), 0.20f, 0.45f, LoadTexture("SpaceCenter_Tex_6.png"));

        mats["Main_Stage"] = GetOrCreateMaterial("SpaceCenter_MainStage_Mat", litShader,
            new Color(0.95f, 0.95f, 0.97f), 0.25f, 0.50f, LoadTexture("SpaceCenter_Tex_7.png"));

        mats["2ndStage_Cover"] = GetOrCreateMaterial("SpaceCenter_2ndStage_Mat", litShader,
            new Color(0.90f, 0.90f, 0.92f), 0.30f, 0.50f, null);

        mats["Faring"] = GetOrCreateMaterial("SpaceCenter_Fairing_Mat", litShader,
            new Color(0.95f, 0.95f, 0.97f), 0.20f, 0.45f, LoadTexture("SpaceCenter_Tex_9.png"));

        return mats;
    }

    private static Material GetOrCreateMaterial(string name, Shader shader, Color color, float metallic, float smoothness, Texture2D texture)
    {
        string path = $"{MatPath}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = name;
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

    private static Texture2D LoadTexture(string filename)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexPath}/{filename}");
    }

    private static void CreateFacilityPrefab(string objPath, Dictionary<string, Material> mats)
    {
        string prefabPath = $"{PrefabPath}/SpaceCenter_LaunchFacility.prefab";
        AssetDatabase.ImportAsset(objPath, ImportAssetOptions.ForceUpdate);
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
        if (model == null) return;

        GameObject instance = (GameObject)Object.Instantiate(model);
        instance.name = "SpaceCenter_RealisticLaunchFacility";

        // Assign materials to mesh renderers
        MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mr in renderers)
        {
            string cleanName = mr.gameObject.name;
            string normClean = cleanName.Replace("#", "").Replace("_", "").ToLowerInvariant();
            Material chosen = null;
            foreach (var kvp in mats)
            {
                string normKey = kvp.Key.Replace("#", "").Replace("_", "").ToLowerInvariant();
                if (normClean.Contains(normKey))
                {
                    chosen = kvp.Value;
                    break;
                }
            }
            if (chosen != null)
            {
                mr.sharedMaterial = chosen;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }

    private static void CreateRocketPrefab(string objPath, Dictionary<string, Material> mats)
    {
        string prefabPath = $"{SpacecraftPrefabPath}/SpaceCenter_LaunchVehicle.prefab";
        AssetDatabase.ImportAsset(objPath, ImportAssetOptions.ForceUpdate);
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
        if (model == null) return;

        GameObject instance = (GameObject)Object.Instantiate(model);
        instance.name = "SpaceCenter_LaunchVehicle";

        MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mr in renderers)
        {
            string cleanName = mr.gameObject.name;
            string normClean = cleanName.Replace("#", "").Replace("_", "").ToLowerInvariant();
            Material chosen = null;
            foreach (var kvp in mats)
            {
                string normKey = kvp.Key.Replace("#", "").Replace("_", "").ToLowerInvariant();
                if (normClean.Contains(normKey))
                {
                    chosen = kvp.Value;
                    break;
                }
            }
            if (chosen != null)
            {
                mr.sharedMaterial = chosen;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}

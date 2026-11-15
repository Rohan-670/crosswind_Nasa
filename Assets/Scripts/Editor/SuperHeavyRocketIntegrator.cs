using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Integrator for the NASA Nexus Super Heavy Rocket 3D model into ASTROBASE:
/// Sets up texture import settings, URP Lit PBR materials, and generates
/// the reusable Nexus_SuperHeavyRocket.prefab.
/// </summary>
public class SuperHeavyRocketIntegrator
{
    private const string SpacecraftArtPath = "Assets/Art/Spacecraft";
    private const string TexturesPath = "Assets/Art/Spacecraft/Textures";
    private const string MaterialsPath = "Assets/Materials/Spacecraft";
    private const string PrefabsPath = "Assets/Prefabs/Spacecraft";

    [MenuItem("ASTROBASE/Spacecraft/Integrate Super Heavy Rocket")]
    public static void IntegrateSuperHeavyRocket()
    {
        try
        {
            EnsureDirectory(MaterialsPath);
            EnsureDirectory(PrefabsPath);

            // 1. Configure Textures
            ConfigureTextures();

            // 2. Configure Model
            string objPath = SpacecraftArtPath + "/Nexus_SuperHeavyRocket.obj";
            if (!File.Exists(objPath))
            {
                Debug.LogError("[ASTROBASE] Nexus_SuperHeavyRocket.obj not found at " + objPath);
                return;
            }
            ConfigureModelImporter(objPath);

            // 3. Create PBR Material
            Material rocketMat = CreateOrUpdateMaterial();

            // 4. Create Prefab
            GameObject prefab = CreateOrUpdatePrefab(objPath, rocketMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ASTROBASE] Super Heavy Rocket integrated successfully at " + PrefabsPath + "/Nexus_SuperHeavyRocket.prefab");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ASTROBASE] Failed to integrate Super Heavy Rocket: " + ex);
        }
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }

    private static void ConfigureTextures()
    {
        // Normal Map
        string normalPath = TexturesPath + "/Nexus_Normal.png";
        if (File.Exists(normalPath))
        {
            TextureImporter normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null)
            {
                bool dirty = false;
                if (normalImporter.textureType != TextureImporterType.NormalMap)
                {
                    normalImporter.textureType = TextureImporterType.NormalMap;
                    dirty = true;
                }
                if (normalImporter.sRGBTexture)
                {
                    normalImporter.sRGBTexture = false;
                    dirty = true;
                }
                if (dirty)
                {
                    normalImporter.SaveAndReimport();
                }
            }
        }

        // Metallic / Roughness Map
        string mrPath = TexturesPath + "/Nexus_MetallicRoughness.png";
        if (File.Exists(mrPath))
        {
            TextureImporter mrImporter = AssetImporter.GetAtPath(mrPath) as TextureImporter;
            if (mrImporter != null && mrImporter.sRGBTexture)
            {
                mrImporter.sRGBTexture = false;
                mrImporter.SaveAndReimport();
            }
        }

        // Base Color
        string bcPath = TexturesPath + "/Nexus_BaseColor.jpg";
        if (File.Exists(bcPath))
        {
            TextureImporter bcImporter = AssetImporter.GetAtPath(bcPath) as TextureImporter;
            if (bcImporter != null && !bcImporter.sRGBTexture)
            {
                bcImporter.sRGBTexture = true;
                bcImporter.SaveAndReimport();
            }
        }
    }

    private static void ConfigureModelImporter(string objPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(objPath) as ModelImporter;
        if (importer != null)
        {
            bool dirty = false;
            if (!importer.isReadable) { importer.isReadable = true; dirty = true; }
            if (importer.importNormals != ModelImporterNormals.Import) { importer.importNormals = ModelImporterNormals.Import; dirty = true; }
            if (importer.importTangents != ModelImporterTangents.CalculateMikk) { importer.importTangents = ModelImporterTangents.CalculateMikk; dirty = true; }
            if (Mathf.Abs(importer.globalScale - 1f) > 0.001f) { importer.globalScale = 1f; dirty = true; }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
    }

    private static Material CreateOrUpdateMaterial()
    {
        string matPath = MaterialsPath + "/Nexus_SuperHeavyRocket_Mat.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(urpLit);
            mat.name = "Nexus_SuperHeavyRocket_Mat";
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else if (mat.shader != urpLit && urpLit != null)
        {
            mat.shader = urpLit;
        }

        // Configure PBR parameters
        mat.SetColor("_BaseColor", new Color(0.96f, 0.96f, 0.98f, 1.0f));
        mat.SetFloat("_Metallic", 0.45f);
        mat.SetFloat("_Smoothness", 0.65f);

        Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath + "/Nexus_BaseColor.jpg");
        if (baseTex != null) mat.SetTexture("_BaseMap", baseTex);

        Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath + "/Nexus_Normal.png");
        if (normalTex != null)
        {
            mat.SetTexture("_BumpMap", normalTex);
            mat.EnableKeyword("_NORMALMAP");
        }

        Texture2D mrTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath + "/Nexus_MetallicRoughness.png");
        if (mrTex != null)
        {
            mat.SetTexture("_MetallicGlossMap", mrTex);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject CreateOrUpdatePrefab(string objPath, Material rocketMat)
    {
        string prefabPath = PrefabsPath + "/Nexus_SuperHeavyRocket.prefab";

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
        if (modelAsset == null)
        {
            throw new System.Exception("Failed to load model asset at " + objPath);
        }

        GameObject root = (GameObject)Object.Instantiate(modelAsset);
        root.name = "Nexus_SuperHeavyRocket";

        // Assign materials to all renderers
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in renderers)
        {
            if (mr != null && rocketMat != null)
            {
                mr.sharedMaterial = rocketMat;
            }
        }

        // Add main capsule collider for physical interaction / docking / hazard hits
        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0f, 0f);
        col.radius = 4.8f;
        col.height = 16.0f;
        col.direction = 2; // Z-axis

        // Create engine mounting anchor at the rocket base (Z = -8.0f)
        GameObject engineAnchor = new GameObject("EngineMount_Cluster");
        engineAnchor.transform.SetParent(root.transform, false);
        engineAnchor.transform.localPosition = new Vector3(0f, 0f, -8.1f);

        // Save prefab
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        return prefabAsset;
    }
}

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Integrator for the real 3D NASA Lunar Petavius Crater environment into ASTROBASE:
/// Replaces the placeholder sphere crater rims and flat terrain with the
/// authentic 3D lunar crater terrain model, PBR textures, and physical collision.
/// </summary>
public class MoonEnvironmentIntegrator
{
    private const string CraterArtPath = "Assets/Art/Environment/PetaviusCrater";
    private const string CraterTexturesPath = "Assets/Art/Environment/PetaviusCrater/Textures";
    private const string MaterialsPath = "Assets/Materials";
    private const string PrefabsPath = "Assets/Prefabs/Environment";
    private const string ScenesPath = "Assets/Scenes";

    [MenuItem("ASTROBASE/Environment/Integrate Real Moon Petavius Crater")]
    public static void IntegrateMoonEnvironment()
    {
        try
        {
            EnsureDirectory(MaterialsPath);
            EnsureDirectory(PrefabsPath);

            // 1. Configure Textures
            ConfigureTextures();

            // 2. Configure Model
            string objPath = CraterArtPath + "/Petavius_Crater.obj";
            if (!File.Exists(objPath))
            {
                objPath = PrefabsPath + "/Petavius_Crater.obj";
            }

            if (!File.Exists(objPath))
            {
                Debug.LogError("[ASTROBASE] Petavius_Crater.obj not found at " + objPath);
                return;
            }
            ConfigureModelImporter(objPath);

            // 3. Create or update Material
            Material craterMat = CreateOrUpdateMaterial();

            // 4. Create or update Prefab
            GameObject prefab = CreateOrUpdatePrefab(objPath, craterMat);

            // 5. Update Scenes: LunarLanding.unity and LunarExploration.unity
            UpdateSceneEnvironment(ScenesPath + "/LunarLanding.unity", prefab);
            UpdateSceneEnvironment(ScenesPath + "/LunarExploration.unity", prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ASTROBASE] Real Moon Petavius Crater environment integrated successfully!");
            EditorUtility.DisplayDialog("ASTROBASE: Moon Environment",
                "Successfully integrated the real 3D NASA Petavius Crater environment into LunarLanding and LunarExploration scenes!", "OK");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[ASTROBASE] Failed to integrate Moon Environment: " + ex);
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
        // Normal map
        string normalPath = CraterTexturesPath + "/Petavius_Normal.png";
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
                if (dirty) normalImporter.SaveAndReimport();
            }
        }

        // Metallic/Roughness
        string mrPath = CraterTexturesPath + "/Petavius_MetallicRoughness.png";
        if (File.Exists(mrPath))
        {
            TextureImporter mrImporter = AssetImporter.GetAtPath(mrPath) as TextureImporter;
            if (mrImporter != null && mrImporter.sRGBTexture)
            {
                mrImporter.sRGBTexture = false;
                mrImporter.SaveAndReimport();
            }
        }

        // Base color
        string bcPath = CraterTexturesPath + "/Petavius_BaseColor.jpg";
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
            if (importer.addCollider) { importer.addCollider = false; dirty = true; }

            if (dirty) importer.SaveAndReimport();
        }
    }

    private static Material CreateOrUpdateMaterial()
    {
        string matPath = MaterialsPath + "/Moon_PetaviusCrater_Mat.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(urpLit);
            mat.name = "Moon_PetaviusCrater_Mat";
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else if (mat.shader != urpLit && urpLit != null)
        {
            mat.shader = urpLit;
        }

        // Realistic lunar regolith photometric properties
        mat.SetColor("_BaseColor", new Color(0.85f, 0.86f, 0.88f, 1.0f));
        mat.SetFloat("_Metallic", 0.05f);
        mat.SetFloat("_Smoothness", 0.20f);

        Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(CraterTexturesPath + "/Petavius_BaseColor.jpg");
        if (baseTex != null) mat.SetTexture("_BaseMap", baseTex);

        Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(CraterTexturesPath + "/Petavius_Normal.png");
        if (normalTex != null)
        {
            mat.SetTexture("_BumpMap", normalTex);
            mat.EnableKeyword("_NORMALMAP");
        }

        Texture2D mrTex = AssetDatabase.LoadAssetAtPath<Texture2D>(CraterTexturesPath + "/Petavius_MetallicRoughness.png");
        if (mrTex != null)
        {
            mat.SetTexture("_MetallicGlossMap", mrTex);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject CreateOrUpdatePrefab(string objPath, Material mat)
    {
        string prefabPath = PrefabsPath + "/MoonEnvironment_PetaviusCrater.prefab";

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
        if (modelAsset == null)
        {
            throw new System.Exception("Failed to load crater model asset at " + objPath);
        }

        GameObject root = (GameObject)Object.Instantiate(modelAsset);
        root.name = "MoonEnvironment_PetaviusCrater";

        // Assign materials and add mesh collider
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in renderers)
        {
            if (mr != null && mat != null) mr.sharedMaterial = mat;

            MeshFilter mf = mr.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                MeshCollider mc = mr.gameObject.GetComponent<MeshCollider>();
                if (mc == null) mc = mr.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        return prefabAsset;
    }

    public static void UpdateSceneEnvironment(string scenePath, GameObject craterPrefab)
    {
        if (!File.Exists(scenePath)) return;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find existing Lunar_Surface_Terrain
        GameObject terrainRoot = GameObject.Find("Lunar_Surface_Terrain");
        if (terrainRoot != null)
        {
            // Remove old placeholder CraterRim spheres
            for (int i = terrainRoot.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = terrainRoot.transform.GetChild(i);
                if (child.name.StartsWith("CraterRim_") || child.name.StartsWith("Hill_"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            // Check if Petavius Crater is already attached
            Transform existingCrater = terrainRoot.transform.Find("Petavius_Crater_Model");
            if (existingCrater != null)
            {
                Object.DestroyImmediate(existingCrater.gameObject);
            }

            if (craterPrefab != null)
            {
                GameObject craterInstance = (GameObject)PrefabUtility.InstantiatePrefab(craterPrefab, terrainRoot.transform);
                craterInstance.name = "Petavius_Crater_Model";
                craterInstance.transform.localPosition = Vector3.zero;
                craterInstance.transform.localRotation = Quaternion.identity;
                craterInstance.transform.localScale = Vector3.one;
            }

            // Lower or disable placeholder flat Ground_Plane so real crater topography shines through
            Transform groundPlane = terrainRoot.transform.Find("Ground_Plane");
            if (groundPlane != null)
            {
                groundPlane.localPosition = new Vector3(0f, -34.0f, 0f);
            }
        }

        EditorSceneManager.SaveScene(scene, scenePath);
    }
}

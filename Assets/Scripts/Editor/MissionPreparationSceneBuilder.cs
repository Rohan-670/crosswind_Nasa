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
/// Automation tool to build the MissionPreparation scene,
/// generate the 3D astronaut EMU model, create equipment ScriptableObjects,
/// and wire all capacity and life-support validation telemetry.
/// </summary>
public class MissionPreparationSceneBuilder
{
    private const string EquipmentSOPath = "Assets/ScriptableObjects/Equipment";
    private const string MaterialsPath = "Assets/Materials";
    private const string PrefabsPath = "Assets/Prefabs/Astronaut";
    private const string ScenesPath = "Assets/Scenes";

    [MenuItem("ASTROBASE/Missions/Build MissionPreparation Scene")]
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
            // If Equipment ScriptableObjects do not exist yet, initialize them
            if (!File.Exists(EquipmentSOPath + "/Item_01_Spacesuit.asset"))
            {
                Debug.Log("[ASTROBASE] Initializing Equipment ScriptableObjects and Preparation scene...");
                ExecuteBuild(true);
            }
        };
    }

    public static void ExecuteBuild(bool isBatch)
    {
        try
        {
            EnsureDirectory(EquipmentSOPath);
            EnsureDirectory(MaterialsPath);
            EnsureDirectory(PrefabsPath);
            EnsureDirectory(ScenesPath);

            TMP_FontAsset font = FindTMPFont();

            // 1. Create equipment ScriptableObjects
            EquipmentItem[] equipmentItems = CreateOrUpdateEquipmentItems();

            // 2. Build 3D Astronaut Prefab
            GameObject astronautPrefab = CreateOrUpdateAstronautPrefab();

            // 3. Build MissionPreparation.unity scene
            BuildPreparationScene(equipmentItems, astronautPrefab, font);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: Mission Preparation Ready!",
                    "MissionPreparation scene created successfully at:\n  Assets/Scenes/MissionPreparation.unity\n\n" +
                    "  ✓ 3D Astronaut in staging airlock with idle breathing sway\n" +
                    "  ✓ 7 ScriptableObject equipment items (3 required, 4 optional)\n" +
                    "  ✓ Real-time cargo capacity meter (100 kg max payload)\n" +
                    "  ✓ Automatic life support validation (prevents boarding without suit/O2/helmet)\n" +
                    "  ✓ Persistent EquipmentManager and Boarding confirmation modal\n\n" +
                    "Press Play ▶ in Unity Editor to test equipment outfitting!",
                    "OK");
            }
            Debug.Log("[ASTROBASE] MissionPreparation scene built successfully.");
        }
        catch (System.Exception ex)
        {
            if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", ex.Message, "OK");
            Debug.LogError("[ASTROBASE] MissionPreparation build failed: " + ex);
        }
    }

    // ===================================================================
    // 1. SCRIPTABLE OBJECT EQUIPMENT DEFINITIONS
    // ===================================================================

    private static EquipmentItem[] CreateOrUpdateEquipmentItems()
    {
        List<EquipmentItem> items = new List<EquipmentItem>();

        // 1. Spacesuit (Required)
        items.Add(CreateOrUpdateItem("Item_01_Spacesuit", "spacesuit", "EMU Lunar Spacesuit",
            EquipmentCategory.LifeSupport,
            "Pressurized Extravehicular Mobility Unit. Provides multi-layer thermal shielding, micrometeorite protection, and joint articulation for lunar surface operations.",
            34.0f, true, new Color(0.25f, 0.85f, 1.0f)));

        // 2. Oxygen Supply (Required)
        items.Add(CreateOrUpdateItem("Item_02_OxygenSupply", "oxygen_supply", "Primary Life Support (PLSS)",
            EquipmentCategory.LifeSupport,
            "Dual high-pressure oxygen reservoirs with lithium hydroxide CO2 scrubbers, liquid cooling loop, and 8-hour breathable atmosphere cycle.",
            22.0f, true, new Color(0.25f, 0.85f, 1.0f)));

        // 3. Helmet (Required)
        items.Add(CreateOrUpdateItem("Item_03_EVAHelmet", "helmet", "EVA Helmet & Gold Visor",
            EquipmentCategory.LifeSupport,
            "Impact-resistant polycarbonate pressure helmet with vapor-deposited gold thermal reflective visor, wireless comms, and heads-up telemetry HUD.",
            6.0f, true, new Color(0.25f, 0.85f, 1.0f)));

        // 4. Sample Containers (Optional)
        items.Add(CreateOrUpdateItem("Item_04_SampleContainers", "sample_containers", "Cryogenic Sample Canisters",
            EquipmentCategory.Scientific,
            "Hermetically sealed vacuum-insulated canisters designed to preserve volatile lunar south-pole core ice and pristine regolith samples.",
            12.0f, false, new Color(0.95f, 0.75f, 0.25f)));

        // 5. Scientific Camera (Optional)
        items.Add(CreateOrUpdateItem("Item_05_ScientificCamera", "camera", "High-Res Multispectral Camera",
            EquipmentCategory.Scientific,
            "Radiation-hardened survey camera with calibrated macro and telephoto optical assemblies for documenting crater wall stratigraphy.",
            4.0f, false, new Color(0.95f, 0.75f, 0.25f)));

        // 6. Spectral Scanner (Optional)
        items.Add(CreateOrUpdateItem("Item_06_SpectralScanner", "scanner", "XRF Mineralogical Scanner",
            EquipmentCategory.Tools,
            "Handheld X-ray fluorescence spectrometer for real-time elemental detection of titanium, iron, and subsurface water-ice signatures.",
            8.0f, false, new Color(0.40f, 0.90f, 0.50f)));

        // 7. Emergency Kit (Optional)
        items.Add(CreateOrUpdateItem("Item_07_EmergencyKit", "emergency_kit", "Field Repair & Medical Kit",
            EquipmentCategory.Safety,
            "Rapid-seal suit puncture patches, emergency joint clamps, portable medical vitals monitor, and first-response trauma supplies.",
            10.0f, false, new Color(0.95f, 0.35f, 0.35f)));

        return items.ToArray();
    }

    private static EquipmentItem CreateOrUpdateItem(string file, string id, string name,
        EquipmentCategory category, string desc, float weight, bool required, Color accent)
    {
        string path = EquipmentSOPath + "/" + file + ".asset";
        EquipmentItem item = AssetDatabase.LoadAssetAtPath<EquipmentItem>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<EquipmentItem>();
            AssetDatabase.CreateAsset(item, path);
        }

        item.itemId = id;
        item.itemName = name;
        item.category = category;
        item.description = desc;
        item.weightKg = weight;
        item.isRequired = required;
        item.accentColor = accent;

        EditorUtility.SetDirty(item);
        return item;
    }

    // ===================================================================
    // 2. 3D ASTRONAUT MODEL PREFAB
    // ===================================================================

    [MenuItem("ASTROBASE/Astronaut/Rebuild Upright Astronaut Prefab")]
    public static void RebuildAstronautPrefabMenu()
    {
        CreateOrUpdateAstronautPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ASTROBASE] Upright Astronaut EMU prefab rebuilt successfully.");
    }

    public static GameObject CreateOrUpdateAstronautPrefab()
    {
        string articulatedTorso = "Assets/Art/Astronaut/Articulated/Astronaut_Torso.obj";
        if (File.Exists(articulatedTorso))
        {
            return AstronautRigBuilder.BuildAstronautPrefab();
        }

        string prefabPath = PrefabsPath + "/Astronaut_EMU_Rig.prefab";
        string objPath = "Assets/Art/Astronaut/Extravehicular_Mobility_Unit.obj";

        // Check if real NASA EMU 3D model exists
        if (File.Exists(objPath))
        {
            return BuildRealEMUAstronautPrefab(objPath, prefabPath);
        }
        else
        {
            return BuildPrimitiveAstronautPrefab(prefabPath);
        }
    }

    private static GameObject BuildRealEMUAstronautPrefab(string objPath, string prefabPath)
    {
        // 1. Configure texture importers
        string texDir = "Assets/Art/Astronaut/Textures";
        if (Directory.Exists(texDir))
        {
            string[] texFiles = Directory.GetFiles(texDir, "*.png");
            foreach (string tf in texFiles)
            {
                string uPath = tf.Replace('\\', '/');
                TextureImporter ti = AssetImporter.GetAtPath(uPath) as TextureImporter;
                if (ti != null)
                {
                    ti.textureType = TextureImporterType.Default;
                    ti.sRGBTexture = true;
                    ti.wrapMode = TextureWrapMode.Repeat;
                    ti.SaveAndReimport();
                }
            }
        }

        // 2. Configure model importer
        ModelImporter mi = AssetImporter.GetAtPath(objPath) as ModelImporter;
        if (mi != null)
        {
            mi.globalScale = 1.0f;
            mi.importNormals = ModelImporterNormals.Import;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.SaveAndReimport();
        }

        // 3. Create materials
        Dictionary<string, Material> mats = new Dictionary<string, Material>();
        mats["EMU_Suit_Mat"] = GetOrCreateLitMaterial("EMU_Suit_Mat", new Color(0.92f, 0.92f, 0.94f), 0.05f, 0.35f, null);
        mats["EMU_Visor_Mat"] = GetOrCreateLitMaterial("EMU_Visor_Mat", new Color(1.0f, 0.82f, 0.25f), 0.95f, 0.92f, null);
        mats["EMU_Joints_Mat"] = GetOrCreateLitMaterial("EMU_Joints_Mat", new Color(0.60f, 0.60f, 0.65f), 0.75f, 0.60f, null);
        mats["EMU_DarkDetails_Mat"] = GetOrCreateLitMaterial("EMU_DarkDetails_Mat", new Color(0.20f, 0.20f, 0.24f), 0.15f, 0.30f, null);
        mats["EMU_RedStripe_Mat"] = GetOrCreateLitMaterial("EMU_RedStripe_Mat", new Color(0.94f, 0.14f, 0.16f), 0.0f, 0.35f, null);

        mats["EMU_FlagPatch_Mat"] = GetOrCreateLitMaterial("EMU_FlagPatch_Mat", Color.white, 0.0f, 0.30f, "emusuitFLAGpatch.png");
        mats["EMU_ChestBox_Mat"] = GetOrCreateLitMaterial("EMU_ChestBox_Mat", Color.white, 0.15f, 0.40f, "emusuitbox4.png");
        mats["EMU_VitruvianPatch_Mat"] = GetOrCreateLitMaterial("EMU_VitruvianPatch_Mat", Color.white, 0.0f, 0.30f, "emusuitvitruvianpatch.png");

        // 4. Instantiate model asset
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
        if (modelAsset == null)
        {
            return BuildPrimitiveAstronautPrefab(prefabPath);
        }

        GameObject root = (GameObject)Object.Instantiate(modelAsset);
        root.name = "Astronaut_EMU_Rig";

        // Assign materials to primitives
        MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 1 && renderers[0].sharedMaterials.Length == 25)
        {
            Material[] assignedMats = new Material[25];
            for (int i = 0; i < 25; i++)
            {
                assignedMats[i] = GetEMUMaterialForPrimitive(i, mats);
            }
            renderers[0].sharedMaterials = assignedMats;
        }
        else
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer mr = renderers[i];
                string rName = mr.gameObject.name;
                int pIdx = -1;
                if (rName.StartsWith("Primitive_"))
                {
                    string[] parts = rName.Split('_');
                    if (parts.Length > 1) int.TryParse(parts[1], out pIdx);
                }
                if (pIdx < 0) pIdx = i;

                mr.sharedMaterial = GetEMUMaterialForPrimitive(pIdx, mats);
            }
        }

        // Attach idle motion
        AstronautIdling idling = root.GetComponent<AstronautIdling>();
        if (idling == null) idling = root.AddComponent<AstronautIdling>();
        idling.breathSpeed = 1.1f;
        idling.breathAmount = 0.008f;
        idling.swaySpeed = 0.5f;
        idling.swayAmount = 0.5f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[ASTROBASE] Successfully built authentic 3D NASA EMU Spacesuit prefab at " + prefabPath);
        return prefab;
    }

    private static Material GetEMUMaterialForPrimitive(int pIdx, Dictionary<string, Material> mats)
    {
        switch (pIdx)
        {
            case 1:  // blinn2SG
            case 7:  // emusuit5_blinn2SG
            case 15: // blinn12SG
            case 17: // blinn1SG
            case 23: // emusuit5_blinn3SG
            case 24: // emusuit5_blinn1SG
                return mats["EMU_Joints_Mat"];

            case 13: // justbody_blinn3SG (visor)
            case 16: // blinn3SG (visor)
                return mats["EMU_Visor_Mat"];

            case 6:  // emusuit5_jetpack2SG (flag)
            case 21: // lambert9SG (flag)
                return mats["EMU_FlagPatch_Mat"];

            case 11: // lambert7SG (chest telemetry box)
                return mats["EMU_ChestBox_Mat"];

            case 20: // lambert8SG (Vitruvian NASA patch)
                return mats["EMU_VitruvianPatch_Mat"];

            case 9:  // lambert4SG (red commander stripe)
                return mats["EMU_RedStripe_Mat"];

            case 8:  // emusuit5_lambert5SG
            case 12: // lambert11SG
            case 18: // lambert3SG
            case 22: // emusuit5_lambert3SG
                return mats["EMU_DarkDetails_Mat"];

            case 0:  // emusuit5_lambert2SG
            case 2:  // lambert2SG
            case 3:  // jetpack2SG (PLSS backpack)
            case 4:  // initialShadingGroup
            case 5:  // lambert5SG
            case 10: // justbody_lambert2SG
            case 14: // lambert10SG
            case 19: // body_lambert2SG
            default:
                return mats["EMU_Suit_Mat"];
        }
    }

    private static GameObject BuildPrimitiveAstronautPrefab(string prefabPath)
    {
        Material suitMat = GetOrCreateLitMaterial("Astronaut_Suit_Mat", new Color(0.92f, 0.92f, 0.94f), 0.05f, 0.35f, null);
        Material visorMat = GetOrCreateLitMaterial("Astronaut_Visor_Mat", new Color(1.0f, 0.82f, 0.25f), 0.95f, 0.92f, null);
        Material darkMat = GetOrCreateLitMaterial("Astronaut_DarkDetails_Mat", new Color(0.18f, 0.20f, 0.25f), 0.20f, 0.40f, null);
        Material tankMat = GetOrCreateLitMaterial("Astronaut_Tanks_Mat", new Color(0.85f, 0.88f, 0.92f), 0.85f, 0.75f, null);

        GameObject root = new GameObject("Astronaut_EMU_Rig");
        root.AddComponent<AstronautIdling>();

        // Torso / Chest Unit
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = "Torso";
        torso.transform.SetParent(root.transform, false);
        torso.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        torso.transform.localScale = new Vector3(0.55f, 0.50f, 0.38f);
        torso.GetComponent<Renderer>().sharedMaterial = suitMat;
        Object.DestroyImmediate(torso.GetComponent<Collider>());

        // Chest Control Pack
        GameObject chestPack = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chestPack.name = "Chest_Control_Module";
        chestPack.transform.SetParent(torso.transform, false);
        chestPack.transform.localPosition = new Vector3(0f, 0.05f, 0.45f);
        chestPack.transform.localScale = new Vector3(0.65f, 0.50f, 0.25f);
        chestPack.GetComponent<Renderer>().sharedMaterial = darkMat;
        Object.DestroyImmediate(chestPack.GetComponent<Collider>());

        // Helmet
        GameObject helmet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        helmet.name = "Helmet";
        helmet.transform.SetParent(root.transform, false);
        helmet.transform.localPosition = new Vector3(0f, 1.62f, 0.02f);
        helmet.transform.localScale = new Vector3(0.44f, 0.46f, 0.44f);
        helmet.GetComponent<Renderer>().sharedMaterial = suitMat;
        Object.DestroyImmediate(helmet.GetComponent<Collider>());

        // Gold Reflective Visor
        GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visor.name = "Gold_Visor";
        visor.transform.SetParent(helmet.transform, false);
        visor.transform.localPosition = new Vector3(0f, 0f, 0.22f);
        visor.transform.localScale = new Vector3(0.78f, 0.65f, 0.72f);
        visor.GetComponent<Renderer>().sharedMaterial = visorMat;
        Object.DestroyImmediate(visor.GetComponent<Collider>());

        // PLSS Backpack (Life Support)
        GameObject plss = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plss.name = "PLSS_Backpack";
        plss.transform.SetParent(root.transform, false);
        plss.transform.localPosition = new Vector3(0f, 1.15f, -0.26f);
        plss.transform.localScale = new Vector3(0.52f, 0.72f, 0.28f);
        plss.GetComponent<Renderer>().sharedMaterial = suitMat;
        Object.DestroyImmediate(plss.GetComponent<Collider>());

        // Oxygen Tanks
        GameObject tankL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tankL.name = "Oxygen_Tank_Port";
        tankL.transform.SetParent(plss.transform, false);
        tankL.transform.localPosition = new Vector3(-0.25f, 0.02f, -0.45f);
        tankL.transform.localScale = new Vector3(0.24f, 0.40f, 0.24f);
        tankL.GetComponent<Renderer>().sharedMaterial = tankMat;
        Object.DestroyImmediate(tankL.GetComponent<Collider>());

        GameObject tankR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tankR.name = "Oxygen_Tank_Starboard";
        tankR.transform.SetParent(plss.transform, false);
        tankR.transform.localPosition = new Vector3(0.25f, 0.02f, -0.45f);
        tankR.transform.localScale = new Vector3(0.24f, 0.40f, 0.24f);
        tankR.GetComponent<Renderer>().sharedMaterial = tankMat;
        Object.DestroyImmediate(tankR.GetComponent<Collider>());

        // Limbs
        CreateLimb(root.transform, "Arm_Port", new Vector3(-0.36f, 1.25f, 0f), new Vector3(0.18f, 0.38f, 0.18f), suitMat);
        CreateLimb(root.transform, "Arm_Starboard", new Vector3(0.36f, 1.25f, 0f), new Vector3(0.18f, 0.38f, 0.18f), suitMat);
        CreateLimb(root.transform, "Leg_Port", new Vector3(-0.16f, 0.45f, 0f), new Vector3(0.20f, 0.48f, 0.20f), suitMat);
        CreateLimb(root.transform, "Leg_Starboard", new Vector3(0.16f, 0.45f, 0f), new Vector3(0.20f, 0.48f, 0.20f), suitMat);
        CreateLimb(root.transform, "Boot_Port", new Vector3(-0.16f, 0.06f, 0.06f), new Vector3(0.22f, 0.12f, 0.32f), darkMat);
        CreateLimb(root.transform, "Boot_Starboard", new Vector3(0.16f, 0.06f, 0.06f), new Vector3(0.22f, 0.12f, 0.32f), darkMat);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void CreateLimb(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        limb.name = name;
        limb.transform.SetParent(parent, false);
        limb.transform.localPosition = pos;
        limb.transform.localScale = scale;
        limb.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(limb.GetComponent<Collider>());
    }

    private static Material GetOrCreateLitMaterial(string name, Color color, float metallic, float smoothness, string textureFilename)
    {
        string dir = MaterialsPath + "/Astronaut";
        EnsureDirectory(dir);
        string path = dir + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = name;
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
        }

        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);

        if (!string.IsNullOrEmpty(textureFilename))
        {
            string texPath = "Assets/Art/Astronaut/Textures/" + textureFilename;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
            }
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ===================================================================
    // 3. BUILD PREPARATION SCENE
    // ===================================================================

    private static void BuildPreparationScene(EquipmentItem[] items, GameObject astronautPrefab, TMP_FontAsset font)
    {
        string scenePath = ScenesPath + "/MissionPreparation.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Camera (Positioned to frame 3D astronaut on left, leaving right clear for UI)
        GameObject camGO = new GameObject("Main Camera", typeof(Camera));
        camGO.tag = "MainCamera";
        Camera cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.006f, 0.009f, 0.016f, 1f); // Deep space staging navy
        cam.fieldOfView = 48f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 500f;
        camGO.transform.position = new Vector3(-0.4f, 1.15f, -3.2f);
        camGO.transform.rotation = Quaternion.Euler(4f, 12f, 0f);

        // 2. Lighting (Key light + Staging Rim Light)
        GameObject keyLightGO = new GameObject("Key Light", typeof(Light));
        Light keyLight = keyLightGO.GetComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color(0.95f, 0.98f, 1.0f);
        keyLight.intensity = 1.1f;
        keyLightGO.transform.rotation = Quaternion.Euler(22f, -35f, 0f);

        GameObject rimLightGO = new GameObject("Airlock Rim Light", typeof(Light));
        Light rimLight = rimLightGO.GetComponent<Light>();
        rimLight.type = LightType.Point;
        rimLight.color = new Color(0.25f, 0.75f, 1.0f);
        rimLight.intensity = 2.2f;
        rimLight.range = 6.0f;
        rimLightGO.transform.position = new Vector3(-0.9f, 1.8f, 1.2f);

        // 3. 3D Airlock Staging Platform
        GameObject platformGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platformGO.name = "Airlock_Platform";
        platformGO.transform.position = new Vector3(-0.9f, -0.05f, 0f);
        platformGO.transform.localScale = new Vector3(2.0f, 0.1f, 2.0f);
        Material platMat = GetOrCreateLitMaterial("Airlock_Platform_Mat", new Color(0.12f, 0.15f, 0.20f), 0.6f, 0.4f,null);
        platformGO.GetComponent<Renderer>().sharedMaterial = platMat;

        // 4. Instantiate 3D Astronaut
        if (astronautPrefab != null)
        {
            GameObject astroInstance = (GameObject)PrefabUtility.InstantiatePrefab(astronautPrefab);
            astroInstance.name = "Astronaut_EMU";
            astroInstance.transform.position = new Vector3(-0.9f, 0f, 0f);
            astroInstance.transform.rotation = Quaternion.Euler(0f, 160f, 0f); // Angled nicely toward camera

            // 360 Turntable & Visual Modular Equipment Controller
            AstronautVisualEquipmentController visualCtrl = astroInstance.AddComponent<AstronautVisualEquipmentController>();
            visualCtrl.astronautRoot = astroInstance.transform;
            visualCtrl.defaultYRotation = 160f;
            visualCtrl.BuildOrFindAttachmentProps();
        }

        // 5. Persistent Managers
        new GameObject("MissionManager", typeof(MissionManager));
        GameObject eqMgrGO = new GameObject("EquipmentManager", typeof(EquipmentManager));
        EquipmentManager eqMgr = eqMgrGO.GetComponent<EquipmentManager>();
        eqMgr.maxCargoCapacityKg = 100f;

        // Automatically pre-equip required items initially so player starts ready
        eqMgr.Equip(items[0]); // Spacesuit
        eqMgr.Equip(items[1]); // Oxygen
        eqMgr.Equip(items[2]); // Helmet

        // 6. UI Canvas
        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // Top Header
        GameObject headerGO = CreateUIObject("HeaderPanel", canvasGO.transform);
        RectTransform headerRect = headerGO.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.88f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.offsetMin = new Vector2(30f, 0f);
        headerRect.offsetMax = new Vector2(-30f, -15f);

        GameObject retBtnGO = CreateButton("ReturnButton", "< MISSION SELECT", font, headerGO.transform,
            new Vector2(0f, 0.25f), new Vector2(0f, 0.85f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(190f, 44f), new Color(0.18f, 0.22f, 0.32f, 0.9f));

        GameObject titleGO = CreateUIObject("TitleText", headerGO.transform);
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.12f, 0.40f);
        titleRect.anchorMax = new Vector2(0.85f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTMP.font = font;
        titleTMP.text = "LUNAR EXPEDITION // CREW OUTFITTING";
        titleTMP.fontSize = 28f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = Color.white;
        titleTMP.characterSpacing = 2.5f;

        GameObject destGO = CreateUIObject("DestinationText", headerGO.transform);
        RectTransform destRect = destGO.GetComponent<RectTransform>();
        destRect.anchorMin = new Vector2(0.12f, 0f);
        destRect.anchorMax = new Vector2(0.85f, 0.40f);
        destRect.offsetMin = Vector2.zero;
        destRect.offsetMax = Vector2.zero;
        TextMeshProUGUI destTMP = destGO.AddComponent<TextMeshProUGUI>();
        if (font != null) destTMP.font = font;
        destTMP.text = "TARGET: THE MOON (SOUTH POLE) // DURATION: 14 EARTH DAYS";
        destTMP.fontSize = 14f;
        destTMP.color = new Color(0.35f, 0.85f, 1.0f);

        // Left Viewport Tag (Overlay on the 3D Astronaut side)
        GameObject tagGO = CreateUIObject("AstronautTagPanel", canvasGO.transform);
        RectTransform tagRect = tagGO.GetComponent<RectTransform>();
        tagRect.anchorMin = new Vector2(0.04f, 0.12f);
        tagRect.anchorMax = new Vector2(0.36f, 0.22f);
        tagRect.offsetMin = Vector2.zero;
        tagRect.offsetMax = Vector2.zero;
        Image tagBg = tagGO.AddComponent<Image>();
        tagBg.color = new Color(0.05f, 0.08f, 0.14f, 0.85f);
        tagGO.AddComponent<Outline>().effectColor = new Color(0.25f, 0.85f, 1f, 0.4f);

        GameObject tagTextGO = CreateUIObject("TagText", tagGO.transform);
        StretchFull(tagTextGO.GetComponent<RectTransform>());
        TextMeshProUGUI tagTMP = tagTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) tagTMP.font = font;
        tagTMP.text = "CREW COMMANDER: READY\nSUIT: EXTRAVEHICULAR MOBILITY UNIT (EMU)\nBIOMETRICS: NOMINAL // O2 PRESSURE: 4.3 PSI";
        tagTMP.fontSize = 12f;
        tagTMP.alignment = TextAlignmentOptions.Center;
        tagTMP.color = new Color(0.75f, 0.90f, 1.0f);

        // 360 Turntable Navigation Bar (Under Astronaut Tag Panel)
        GameObject barGO = CreateUIObject("TurntableBar", canvasGO.transform);
        RectTransform barRect = barGO.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.04f, 0.035f);
        barRect.anchorMax = new Vector2(0.36f, 0.105f);
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = Vector2.zero;
        Image barBg = barGO.AddComponent<Image>();
        barBg.color = new Color(0.04f, 0.07f, 0.12f, 0.90f);
        barGO.AddComponent<Outline>().effectColor = new Color(0.25f, 0.75f, 1.0f, 0.6f);

        GameObject hintTextGO = CreateUIObject("HintText", barGO.transform);
        RectTransform htRect = hintTextGO.GetComponent<RectTransform>();
        htRect.anchorMin = new Vector2(0.02f, 0.52f);
        htRect.anchorMax = new Vector2(0.98f, 0.96f);
        htRect.offsetMin = Vector2.zero;
        htRect.offsetMax = Vector2.zero;
        TextMeshProUGUI htTMP = hintTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) htTMP.font = font;
        htTMP.text = "360° VIEW // DRAG TO ROTATE | [A/D] SPIN";
        htTMP.fontSize = 11f;
        htTMP.fontStyle = FontStyles.Bold;
        htTMP.alignment = TextAlignmentOptions.Center;
        htTMP.color = new Color(0.35f, 0.85f, 1.0f);

        // 4 Action Buttons inside bar
        GameObject bRotL = CreateButton("Btn_RotL", "< ROTATE", font, barGO.transform,
            new Vector2(0.02f, 0.08f), new Vector2(0.24f, 0.50f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.15f, 0.22f, 0.32f, 0.95f));

        GameObject bAuto = CreateButton("Btn_Auto", "AUTO 360", font, barGO.transform,
            new Vector2(0.26f, 0.08f), new Vector2(0.50f, 0.50f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.12f, 0.40f, 0.55f, 0.95f));

        GameObject bRotR = CreateButton("Btn_RotR", "ROTATE >", font, barGO.transform,
            new Vector2(0.52f, 0.08f), new Vector2(0.74f, 0.50f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.15f, 0.22f, 0.32f, 0.95f));

        GameObject bReset = CreateButton("Btn_Reset", "RESET", font, barGO.transform,
            new Vector2(0.76f, 0.08f), new Vector2(0.98f, 0.50f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.25f, 0.28f, 0.35f, 0.95f));

        // Right Outfitting Panel
        GameObject outfittingGO = CreateUIObject("OutfittingPanel", canvasGO.transform);
        RectTransform ofRect = outfittingGO.GetComponent<RectTransform>();
        ofRect.anchorMin = new Vector2(0.40f, 0.06f);
        ofRect.anchorMax = new Vector2(0.96f, 0.86f);
        ofRect.offsetMin = Vector2.zero;
        ofRect.offsetMax = Vector2.zero;
        Image ofBg = outfittingGO.AddComponent<Image>();
        ofBg.color = new Color(0.06f, 0.09f, 0.15f, 0.95f);
        outfittingGO.AddComponent<Outline>().effectColor = new Color(0.20f, 0.45f, 0.75f, 0.6f);

        // Capacity Meter Container
        GameObject meterGO = CreateUIObject("CapacityMeter", outfittingGO.transform);
        RectTransform meterRect = meterGO.GetComponent<RectTransform>();
        meterRect.anchorMin = new Vector2(0.03f, 0.90f);
        meterRect.anchorMax = new Vector2(0.97f, 0.98f);
        meterRect.offsetMin = Vector2.zero;
        meterRect.offsetMax = Vector2.zero;

        GameObject weightTextGO = CreateUIObject("TotalWeightText", meterGO.transform);
        RectTransform wtRect = weightTextGO.GetComponent<RectTransform>();
        wtRect.anchorMin = new Vector2(0f, 0.5f);
        wtRect.anchorMax = new Vector2(0.6f, 1f);
        wtRect.offsetMin = Vector2.zero;
        wtRect.offsetMax = Vector2.zero;
        TextMeshProUGUI wtTMP = weightTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) wtTMP.font = font;
        wtTMP.text = "PAYLOAD WEIGHT: 62.0 / 100.0 KG";
        wtTMP.fontSize = 15f;
        wtTMP.fontStyle = FontStyles.Bold;
        wtTMP.color = Color.white;

        GameObject remTextGO = CreateUIObject("RemainingText", meterGO.transform);
        RectTransform rtRect = remTextGO.GetComponent<RectTransform>();
        rtRect.anchorMin = new Vector2(0.6f, 0.5f);
        rtRect.anchorMax = new Vector2(1f, 1f);
        rtRect.offsetMin = Vector2.zero;
        rtRect.offsetMax = Vector2.zero;
        TextMeshProUGUI remTMP = remTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) remTMP.font = font;
        remTMP.text = "FREE CAPACITY: 38.0 KG";
        remTMP.fontSize = 14f;
        remTMP.alignment = TextAlignmentOptions.Right;
        remTMP.color = new Color(0.35f, 0.85f, 1.0f);

        // Progress Bar Background
        GameObject barBgGO = CreateUIObject("BarBackground", meterGO.transform);
        RectTransform barBgRect = barBgGO.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0f, 0.1f);
        barBgRect.anchorMax = new Vector2(1f, 0.42f);
        barBgRect.offsetMin = Vector2.zero;
        barBgRect.offsetMax = Vector2.zero;
        Image barBgImg = barBgGO.AddComponent<Image>();
        barBgImg.color = new Color(0.12f, 0.16f, 0.24f, 0.9f);

        // Progress Bar Fill
        GameObject barFillGO = CreateUIObject("BarFill", barBgGO.transform);
        StretchFull(barFillGO.GetComponent<RectTransform>());
        Image barFillImg = barFillGO.AddComponent<Image>();
        barFillImg.color = new Color(0.25f, 0.85f, 0.45f);
        barFillImg.type = Image.Type.Filled;
        barFillImg.fillMethod = Image.FillMethod.Horizontal;
        barFillImg.fillAmount = 0.62f;

        // Validation Status Banner
        GameObject valGO = CreateUIObject("ValidationBanner", outfittingGO.transform);
        RectTransform valRect = valGO.GetComponent<RectTransform>();
        valRect.anchorMin = new Vector2(0.03f, 0.82f);
        valRect.anchorMax = new Vector2(0.97f, 0.88f);
        valRect.offsetMin = Vector2.zero;
        valRect.offsetMax = Vector2.zero;
        Image valBg = valGO.AddComponent<Image>();
        valBg.color = new Color(0.08f, 0.25f, 0.15f, 0.85f);

        GameObject valTextGO = CreateUIObject("ValidationText", valGO.transform);
        StretchFull(valTextGO.GetComponent<RectTransform>());
        TextMeshProUGUI valTMP = valTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) valTMP.font = font;
        valTMP.text = "[OK] ALL REQUIRED LIFE SUPPORT GEAR VERIFIED // READY FOR BOARDING";
        valTMP.fontSize = 13f;
        valTMP.fontStyle = FontStyles.Bold;
        valTMP.alignment = TextAlignmentOptions.Center;
        valTMP.color = new Color(0.35f, 1.0f, 0.55f);

        // Equipment Rows Container (Vertical List)
        GameObject listGO = CreateUIObject("EquipmentListContainer", outfittingGO.transform);
        RectTransform listRect = listGO.GetComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0.03f, 0.12f);
        listRect.anchorMax = new Vector2(0.97f, 0.80f);
        listRect.offsetMin = Vector2.zero;
        listRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = listGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Build individual rows for the 7 items
        foreach (EquipmentItem item in items)
        {
            BuildEquipmentRow(listGO.transform, item, font);
        }

        // Continue to Boarding Button
        GameObject boardingBtnGO = CreateButton("ContinueToBoardingButton", "CONTINUE TO BOARDING >", font, outfittingGO.transform,
            new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.09f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.12f, 0.52f, 0.90f, 0.95f));

        // Boarding Confirmation Modal
        GameObject boardingModalGO = BuildBoardingModal(canvasGO.transform, font);

        // Wire UI Controller
        MissionPreparationUI ui = canvasGO.AddComponent<MissionPreparationUI>();
        ui.availableEquipment = items;
        ui.equipmentListContainer = listGO.transform;
        ui.missionTitleText = titleTMP;
        ui.destinationText = destTMP;
        ui.totalWeightText = wtTMP;
        ui.remainingCapacityText = remTMP;
        ui.capacityProgressBar = barFillImg;
        ui.validationStatusText = valTMP;
        ui.validationStatusBg = valBg;
        ui.returnButton = retBtnGO.GetComponent<Button>();
        ui.continueToBoardingButton = boardingBtnGO.GetComponent<Button>();
        ui.continueButtonText = boardingBtnGO.GetComponentInChildren<TextMeshProUGUI>();
        ui.continueButtonImage = boardingBtnGO.GetComponent<Image>();
        ui.boardingModal = boardingModalGO;
        ui.boardingModalText = boardingModalGO.transform.Find("DialogPanel/BodyText").GetComponent<TextMeshProUGUI>();
        ui.boardingModalCloseButton = boardingModalGO.transform.Find("DialogPanel/DismissButton").GetComponent<Button>();
        ui.proceedToLaunchButton = boardingModalGO.transform.Find("DialogPanel/ProceedToLaunchButton").GetComponent<Button>();
        ui.rotateLeftButton = bRotL.GetComponent<Button>();
        ui.autoSpinButton = bAuto.GetComponent<Button>();
        ui.rotateRightButton = bRotR.GetComponent<Button>();
        ui.resetViewButton = bReset.GetComponent<Button>();

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static void BuildEquipmentRow(Transform parent, EquipmentItem item, TMP_FontAsset font)
    {
        GameObject rowGO = CreateUIObject("Row_" + item.itemId, parent);
        LayoutElement le = rowGO.AddComponent<LayoutElement>();
        le.minHeight = 56f;
        le.preferredHeight = 58f;

        Image rowBg = rowGO.AddComponent<Image>();
        rowBg.color = new Color(0.09f, 0.13f, 0.20f, 0.90f);

        Outline borderImg = rowGO.AddComponent<Outline>();
        borderImg.effectColor = item.isRequired ? new Color(0.9f, 0.45f, .1f, 0.8f) : new Color(0.22f, 0.5f, 0.32f, 0.4f);

        // Item Name
        GameObject nameGO = CreateUIObject("ItemName", rowGO.transform);
        RectTransform nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.02f, 0.52f);
        nameRect.anchorMax = new Vector2(0.55f, 0.95f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        TextMeshProUGUI nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        if (font != null) nameTMP.font = font;
        nameTMP.text = item.itemName.ToUpper();
        nameTMP.fontSize = 14f;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = Color.white;

        // Description
        GameObject descGO = CreateUIObject("Description", rowGO.transform);
        RectTransform descRect = descGO.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.02f, 0.08f);
        descRect.anchorMax = new Vector2(0.55f, 0.50f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        TextMeshProUGUI descTMP = descGO.AddComponent<TextMeshProUGUI>();
        if (font != null) descTMP.font = font;
        descTMP.text = item.description;
        descTMP.fontSize = 11f;
        descTMP.color = new Color(0.70f, 0.75f, 0.82f);
        descTMP.textWrappingMode = TextWrappingModes.NoWrap;
        descTMP.overflowMode = TextOverflowModes.Ellipsis;

        // Status Badge (Mandatory vs Optional)
        GameObject badgeGO = CreateUIObject("StatusBadge", rowGO.transform);
        RectTransform badgeRect = badgeGO.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.56f, 0.25f);
        badgeRect.anchorMax = new Vector2(0.70f, 0.75f);
        badgeRect.offsetMin = Vector2.zero;
        badgeRect.offsetMax = Vector2.zero;
        Image badgeImg = badgeGO.AddComponent<Image>();
        badgeImg.color = item.isRequired ? new Color(0.95f, 0.35f, 0.20f, 0.9f) : new Color(0.22f, 0.28f, 0.38f, 0.8f);

        GameObject badgeTextGO = CreateUIObject("BadgeText", badgeGO.transform);
        StretchFull(badgeTextGO.GetComponent<RectTransform>());
        TextMeshProUGUI badgeTMP = badgeTextGO.AddComponent<TextMeshProUGUI>();
        if (font != null) badgeTMP.font = font;
        badgeTMP.text = item.isRequired ? "MANDATORY" : "OPTIONAL";
        badgeTMP.fontSize = 10f;
        badgeTMP.fontStyle = FontStyles.Bold;
        badgeTMP.alignment = TextAlignmentOptions.Center;
        badgeTMP.color = Color.white;

        // Weight Text
        GameObject wtGO = CreateUIObject("WeightText", rowGO.transform);
        RectTransform wtRect = wtGO.GetComponent<RectTransform>();
        wtRect.anchorMin = new Vector2(0.71f, 0.25f);
        wtRect.anchorMax = new Vector2(0.81f, 0.75f);
        wtRect.offsetMin = Vector2.zero;
        wtRect.offsetMax = Vector2.zero;
        TextMeshProUGUI wtTMP = wtGO.AddComponent<TextMeshProUGUI>();
        if (font != null) wtTMP.font = font;
        wtTMP.text = $"{item.weightKg:F1} KG";
        wtTMP.fontSize = 13f;
        wtTMP.fontStyle = FontStyles.Bold;
        wtTMP.alignment = TextAlignmentOptions.Center;
        wtTMP.color = new Color(0.4f, 0.85f, 1.0f);

        // Equip / Toggle Button
        GameObject btnGO = CreateButton("EquipButton", item.isRequired ? "[OK] EQUIPPED" : "+ EQUIP", font, rowGO.transform,
            new Vector2(0.82f, 0.18f), new Vector2(0.98f, 0.82f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            item.isRequired ? new Color(0.15f, 0.55f, 0.85f, 0.95f) : new Color(0.18f, 0.22f, 0.30f, 0.85f));

        EquipmentItemUI rowUI = rowGO.AddComponent<EquipmentItemUI>();
        rowUI.itemNameText = nameTMP;
        rowUI.descriptionText = descTMP;
        rowUI.weightText = wtTMP;
        rowUI.statusBadgeText = badgeTMP;
        rowUI.statusBadgeImage = badgeImg;
        rowUI.equipToggleButton = btnGO.GetComponent<Button>();
        rowUI.toggleButtonText = btnGO.GetComponentInChildren<TextMeshProUGUI>();
        rowUI.toggleButtonImage = btnGO.GetComponent<Image>();
        rowUI.itemBorder = rowBg;
    }

    private static GameObject BuildBoardingModal(Transform parent, TMP_FontAsset font)
    {
        GameObject modalGO = CreateUIObject("BoardingModal", parent);
        StretchFull(modalGO.GetComponent<RectTransform>());

        Image backdrop = modalGO.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject panelGO = CreateUIObject("DialogPanel", modalGO.transform);
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.28f, 0.25f);
        panelRect.anchorMax = new Vector2(0.72f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);
        panelGO.AddComponent<Outline>().effectColor = new Color(0.25f, 0.85f, 1.0f, 0.9f);

        GameObject headGO = CreateUIObject("Header", panelGO.transform);
        RectTransform headRect = headGO.GetComponent<RectTransform>();
        headRect.anchorMin = new Vector2(0.06f, 0.82f);
        headRect.anchorMax = new Vector2(0.94f, 0.94f);
        headRect.offsetMin = Vector2.zero;
        headRect.offsetMax = Vector2.zero;
        TextMeshProUGUI headTMP = headGO.AddComponent<TextMeshProUGUI>();
        if (font != null) headTMP.font = font;
        headTMP.text = "[ AIRLOCK CLEARANCE // BOARDING APPROVED ]";
        headTMP.fontSize = 16f;
        headTMP.fontStyle = FontStyles.Bold;
        headTMP.color = new Color(0.35f, 1.0f, 0.55f);
        headTMP.characterSpacing = 2f;

        GameObject bodyGO = CreateUIObject("BodyText", panelGO.transform);
        RectTransform bodyRect = bodyGO.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.06f, 0.22f);
        bodyRect.anchorMax = new Vector2(0.94f, 0.80f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
        TextMeshProUGUI bodyTMP = bodyGO.AddComponent<TextMeshProUGUI>();
        if (font != null) bodyTMP.font = font;
        bodyTMP.text = "Flight clearance confirmed.";
        bodyTMP.fontSize = 15f;
        bodyTMP.color = new Color(0.85f, 0.88f, 0.92f);
        bodyTMP.textWrappingMode = TextWrappingModes.Normal;

        CreateButton("DismissButton", "MODIFY LOADOUT", font, panelGO.transform,
            new Vector2(0.06f, 0.05f), new Vector2(0.46f, 0.16f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.20f, 0.24f, 0.32f, 0.95f));

        CreateButton("ProceedToLaunchButton", "PROCEED TO LAUNCH PAD >", font, panelGO.transform,
            new Vector2(0.52f, 0.05f), new Vector2(0.94f, 0.16f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero, new Color(0.12f, 0.58f, 0.92f, 0.98f));

        modalGO.SetActive(false);
        return modalGO;
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
        tmp.fontSize = 13f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.characterSpacing = 1.2f;

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

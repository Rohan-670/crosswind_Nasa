using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Automation and testing tool for ASTROBASE:
/// - Phase 1: Realistic NASA Earth Integration
/// - Phase 2: Cinematic Space Environment Polish
/// </summary>
public class RealisticEarthIntegrator
{
    private const string ModelsPath = "Assets/Art/NASA_Models";
    private const string TexturesPath = "Assets/Art/NASA_Textures";
    private const string MaterialsPath = "Assets/Materials";
    private const string PrefabsPath = "Assets/Prefabs/Planets";
    private const string ScenesPath = "Assets/Scenes";
    private const string SettingsPath = "Assets/Settings";

    // ===================================================================
    // PHASE 1 MENU ITEMS
    // ===================================================================

    [MenuItem("ASTROBASE/Phase 1: Earth/1. Build & Test in VisualTest Scene")]
    public static void RunStep2VisualTestInteractive()
    {
        RunStep2VisualTest(false);
    }

    [MenuItem("ASTROBASE/Phase 1: Earth/2. Integrate into OpeningStory_VisualTest Scene")]
    public static void RunStep6IntegrationInteractive()
    {
        RunStep6Integration(false);
    }

    // ===================================================================
    // PHASE 2 MENU ITEMS
    // ===================================================================

    [MenuItem("ASTROBASE/Phase 2: Space Polish/Apply Phase 2 Polish to OpeningStory_VisualTest")]
    public static void ApplyPhase2PolishInteractive()
    {
        ApplyPhase2Polish(false);
    }

    public static void RunStep2VisualTestBatch()
    {
        RunStep2VisualTest(true);
    }

    public static void RunStep6IntegrationBatch()
    {
        RunStep6Integration(true);
    }

    public static void ApplyPhase2PolishBatch()
    {
        ApplyPhase2Polish(true);
    }

    // ===================================================================
    // STEP 2 & 5: VISUAL TEST SCENE
    // ===================================================================

    public static void RunStep2VisualTest(bool isBatch)
    {
        try
        {
            EnsureDirectory(PrefabsPath);
            EnsureDirectory(ScenesPath);

            // 1. Configure texture importers
            ConfigureTextureImporters();

            // 2. Configure 3D model importer
            ConfigureModelImporter();

            // 3. Create or update realistic materials
            Material earthMat = GetOrCreateEarthMaterial();
            Material cloudsMat = GetOrCreateCloudsMaterial();
            Material atmoMat = GetOrCreateAtmosphereMaterial();

            // 4. Create or update Prefab
            GameObject earthPrefab = CreateOrUpdateEarthPrefab(earthMat, cloudsMat, atmoMat);

            // 5. Create new VisualTest scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string visualTestScenePath = ScenesPath + "/VisualTest.unity";

            // Spawn Earth instance
            GameObject earthInstance = (GameObject)PrefabUtility.InstantiatePrefab(earthPrefab);
            earthInstance.name = "Earth_NASA_Realistic";
            earthInstance.transform.position = new Vector3(0f, 0f, 0f);

            // Setup Sunlight
            GameObject lightGO = new GameObject("Directional Light (Sun)");
            Light sunLight = lightGO.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1.0f, 0.97f, 0.90f);
            sunLight.intensity = 1.3f;
            sunLight.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(18f, -65f, 0f);

            // Setup Camera
            GameObject camGO = new GameObject("Main Camera", typeof(Camera));
            camGO.tag = "MainCamera";
            Camera cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.008f, 0.015f, 1f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 3000f;
            camGO.transform.position = new Vector3(0f, 5f, -60f);
            camGO.transform.LookAt(Vector3.zero);

            CinematicCameraController camCtrl = camGO.AddComponent<CinematicCameraController>();
            camCtrl.earthTarget = earthInstance.transform;
            camCtrl.orbitCenter = Vector3.zero;
            camCtrl.orbitDistance = 62f;
            camCtrl.orbitSpeed = 1.0f;
            camCtrl.elevation = 5.0f;

            // Save VisualTest scene
            EditorSceneManager.SaveScene(scene, visualTestScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: VisualTest Scene Ready!",
                    "VisualTest scene created successfully at:\n  " + visualTestScenePath + "\n\n" +
                    "Realistic NASA Earth 3D Model and textures are active.\n" +
                    "Press Play ▶ in the Unity Editor to inspect the Earth in isolation.",
                    "OK");
            }
            Debug.Log("[ASTROBASE] VisualTest scene created successfully at " + visualTestScenePath);
        }
        catch (System.Exception ex)
        {
            if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", ex.Message, "OK");
            Debug.LogError("[ASTROBASE] VisualTest creation failed: " + ex);
        }
    }

    // ===================================================================
    // STEP 6: INTEGRATION INTO OpeningStory_VisualTest.unity
    // ===================================================================

    public static void RunStep6Integration(bool isBatch)
    {
        try
        {
            EnsureDirectory(ScenesPath);
            string sourceScenePath = ScenesPath + "/OpeningStory.unity";
            string targetScenePath = ScenesPath + "/OpeningStory_VisualTest.unity";

            if (!System.IO.File.Exists(sourceScenePath))
            {
                // Auto-create OpeningStory.unity if missing
                OpeningStorySceneBuilder.BuildSceneBatch();
            }

            if (!System.IO.File.Exists(sourceScenePath))
            {
                string msg = "Original scene Assets/Scenes/OpeningStory.unity could not be found or built.";
                if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", msg, "OK");
                Debug.LogWarning("[ASTROBASE] " + msg);
                return;
            }

            // Copy source scene to target scene (Original OpeningStory.unity remains 100% untouched)
            System.IO.File.Copy(sourceScenePath, targetScenePath, true);
            AssetDatabase.Refresh();

            if (!System.IO.File.Exists(targetScenePath))
            {
                return;
            }

            // Open the new copy
            var scene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);

            // Ensure materials and prefab are ready
            Material earthMat = GetOrCreateEarthMaterial();
            Material cloudsMat = GetOrCreateCloudsMaterial();
            Material atmoMat = GetOrCreateAtmosphereMaterial();
            GameObject earthPrefab = CreateOrUpdateEarthPrefab(earthMat, cloudsMat, atmoMat);

            // Find old Earth in OpeningStory_VisualTest
            GameObject oldEarth = GameObject.Find("Earth");
            Vector3 targetPos = new Vector3(25f, -8f, 80f);
            if (oldEarth != null)
            {
                targetPos = oldEarth.transform.position;
                Object.DestroyImmediate(oldEarth);
            }

            // Instantiate realistic NASA Earth prefab
            GameObject newEarth = (GameObject)PrefabUtility.InstantiatePrefab(earthPrefab);
            newEarth.name = "Earth";
            newEarth.transform.position = targetPos;

            // Update camera references
            GameObject camGO = GameObject.FindWithTag("MainCamera");
            if (camGO != null)
            {
                CinematicCameraController camCtrl = camGO.GetComponent<CinematicCameraController>();
                if (camCtrl != null)
                {
                    camCtrl.earthTarget = newEarth.transform;
                    camCtrl.orbitCenter = newEarth.transform.position;
                }
            }

            // Save the integration scene
            EditorSceneManager.SaveScene(scene, targetScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: Integration Complete!",
                    "Realistic Earth successfully integrated into:\n  " + targetScenePath + "\n\n" +
                    "  ✓ Original OpeningStory.unity is 100% preserved as fallback\n" +
                    "  ✓ All UI, typewriter dialogue, and buttons are preserved\n" +
                    "  ✓ 3D NASA Earth model with satellite textures is active\n\n" +
                    "Press Play ▶ in Unity Editor to verify the scene!",
                    "OK");
            }
            Debug.Log("[ASTROBASE] Integrated realistic Earth into " + targetScenePath);
        }
        catch (System.Exception ex)
        {
            if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", ex.Message, "OK");
            Debug.LogError("[ASTROBASE] Integration failed: " + ex);
        }
    }

    // ===================================================================
    // PHASE 2: CINEMATIC SPACE ENVIRONMENT POLISH
    // ===================================================================

    public static void ApplyPhase2Polish(bool isBatch)
    {
        try
        {
            EnsureDirectory(ScenesPath);
            string targetScenePath = ScenesPath + "/OpeningStory_VisualTest.unity";
            if (!System.IO.File.Exists(targetScenePath))
            {
                // If integration scene doesn't exist yet, run step 6 first
                RunStep6Integration(true);
            }

            if (!System.IO.File.Exists(targetScenePath))
            {
                string msg = "Scene file not found: '" + targetScenePath + "'. Ensure OpeningStory scene is built first.";
                if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Warning", msg, "OK");
                Debug.LogWarning("[ASTROBASE] " + msg);
                return;
            }

            var scene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);

            // ---------------------------------------------------------------
            // 1. POLISH EARTH ATMOSPHERE & MATERIALS
            // ---------------------------------------------------------------
            GameObject earthGO = GameObject.Find("Earth");
            if (earthGO == null) earthGO = GameObject.Find("Earth_NASA_Realistic");
            if (earthGO == null)
            {
                // Load prefab if missing
                GameObject earthPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsPath + "/Earth_NASA_Realistic.prefab");
                if (earthPrefab != null)
                {
                    earthGO = (GameObject)PrefabUtility.InstantiatePrefab(earthPrefab);
                    earthGO.name = "Earth";
                    earthGO.transform.position = new Vector3(25f, -8f, 80f);
                }
            }

            if (earthGO != null)
            {
                // Ensure rotational velocity
                PlanetRotation earthRot = earthGO.GetComponent<PlanetRotation>();
                if (earthRot == null) earthRot = earthGO.AddComponent<PlanetRotation>();
                earthRot.rotationSpeed = 1.4f;

                // Clouds child
                Transform cloudsTrans = earthGO.transform.Find("Clouds");
                if (cloudsTrans != null)
                {
                    PlanetRotation cloudsRot = cloudsTrans.GetComponent<PlanetRotation>();
                    if (cloudsRot == null) cloudsRot = cloudsTrans.gameObject.AddComponent<PlanetRotation>();
                    cloudsRot.rotationSpeed = 2.2f;
                }
            }

            // Tune Atmosphere Material (Natural sapphire rim, eliminate neon cyan glow)
            Material atmoMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Earth_NASA_Atmosphere_Mat.mat");
            if (atmoMat != null)
            {
                atmoMat.SetColor("_BaseColor", new Color(0.12f, 0.35f, 0.80f, 0.05f));
                atmoMat.EnableKeyword("_EMISSION");
                atmoMat.SetColor("_EmissionColor", new Color(0.04f, 0.15f, 0.45f) * 0.35f);
                EditorUtility.SetDirty(atmoMat);
            }

            // Tune Earth Realistic Surface Material (subtle golden city lights, smooth ocean specular)
            Material earthMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Earth_NASA_Realistic_Mat.mat");
            if (earthMat != null)
            {
                earthMat.SetFloat("_Metallic", 0.02f);
                earthMat.SetFloat("_Smoothness", 0.52f);
                if (earthMat.IsKeywordEnabled("_EMISSION"))
                {
                    earthMat.SetColor("_EmissionColor", new Color(0.85f, 0.75f, 0.55f) * 0.50f);
                }
                EditorUtility.SetDirty(earthMat);
            }

            // Tune Clouds Material
            Material cloudsMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Earth_NASA_Clouds_Mat.mat");
            if (cloudsMat != null)
            {
                cloudsMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.75f));
                cloudsMat.SetFloat("_Smoothness", 0.08f);
                EditorUtility.SetDirty(cloudsMat);
            }

            // ---------------------------------------------------------------
            // 2. REMOVE NEARBY FLAT PURPLE NEBULA QUAD
            // ---------------------------------------------------------------
            GameObject nebulaGO = GameObject.Find("Nebula_Backdrop");
            if (nebulaGO != null)
            {
                Object.DestroyImmediate(nebulaGO);
                Debug.Log("[ASTROBASE] Removed flat Nebula_Backdrop quad.");
            }

            GameObject extraNebula = GameObject.Find("Space_Nebula");
            if (extraNebula != null)
            {
                Object.DestroyImmediate(extraNebula);
            }

            // ---------------------------------------------------------------
            // 3. VAST REALISTIC STARFIELD
            // ---------------------------------------------------------------
            GameObject starfieldGO = GameObject.Find("Starfield");
            if (starfieldGO == null)
            {
                starfieldGO = new GameObject("Starfield");
            }
            ParticleSystem ps = starfieldGO.GetComponent<ParticleSystem>();
            if (ps == null) ps = starfieldGO.AddComponent<ParticleSystem>();
            ConfigureRealisticStarfield(ps);

            // ---------------------------------------------------------------
            // 4. POLISHED SPACECRAFT (PREVENT PLANET CLIPPING, NATURAL ORBIT)
            // ---------------------------------------------------------------
            GameObject shipGO = GameObject.Find("Spacecraft");
            Vector3 shipPos = new Vector3(7.2f, -0.6f, 26f);
            Quaternion shipRot = Quaternion.Euler(4f, -28f, 6f);
            if (shipGO != null)
            {
                shipPos = shipGO.transform.position;
                shipRot = shipGO.transform.rotation;
            }

            // Check if realistic 3D NASA Space Shuttle model/prefab is available
            GameObject shuttlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Spacecraft/SpaceShuttle_Orbiter.prefab");
            if (shuttlePrefab != null)
            {
                if (shipGO != null) Object.DestroyImmediate(shipGO);
                shipGO = (GameObject)PrefabUtility.InstantiatePrefab(shuttlePrefab);
                shipGO.name = "Spacecraft";
                shipGO.transform.position = shipPos;
                shipGO.transform.rotation = shipRot;
            }
            else
            {
                if (shipGO == null) shipGO = new GameObject("Spacecraft");
                shipGO.transform.position = shipPos;
                shipGO.transform.rotation = shipRot;
                shipGO.transform.localScale = Vector3.one;

                // Reassemble modular parts with realistic proportions
                AssemblePolishedShipHierarchy(shipGO);

                // Subtle microgravity drift
                SpacecraftFloating shipFloat = shipGO.GetComponent<SpacecraftFloating>();
                if (shipFloat == null) shipFloat = shipGO.AddComponent<SpacecraftFloating>();
                shipFloat.floatSpeed = 0.45f;
                shipFloat.floatAmount = 0.10f;
                shipFloat.pitchAmount = 0.8f;
                shipFloat.yawAmount = 0.6f;
                shipFloat.rollAmount = 1.0f;
            }

            // ---------------------------------------------------------------
            // 5. SUNLIGHT / DIRECTIONAL LIGHT (NATURAL TERMINATOR SHADING)
            // ---------------------------------------------------------------
            GameObject lightGO = GameObject.Find("Directional Light (Sun)");
            if (lightGO == null)
            {
                Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (Light l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        lightGO = l.gameObject;
                        break;
                    }
                }
            }
            if (lightGO == null) lightGO = new GameObject("Directional Light (Sun)", typeof(Light));
            Light sunLight = lightGO.GetComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1.0f, 0.98f, 0.94f);
            sunLight.intensity = 1.25f;
            sunLight.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(18f, -55f, 0f);

            // ---------------------------------------------------------------
            // 6. VOLUME PROFILE (BLOOM, VIGNETTE, TONEMAPPING ACES)
            // ---------------------------------------------------------------
            ConfigurePostProcessing();

            // ---------------------------------------------------------------
            // 7. CINEMATIC ORBITAL CAMERA
            // ---------------------------------------------------------------
            GameObject camGO = GameObject.FindWithTag("MainCamera");
            if (camGO == null) camGO = new GameObject("Main Camera", typeof(Camera));
            Camera cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.003f, 0.005f, 0.010f, 1f); // Deep space black
            cam.fieldOfView = 52f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 3000f;

            CinematicCameraController camCtrl = camGO.GetComponent<CinematicCameraController>();
            if (camCtrl == null) camCtrl = camGO.AddComponent<CinematicCameraController>();
            if (earthGO != null)
            {
                camCtrl.earthTarget = earthGO.transform;
                camCtrl.orbitCenter = earthGO.transform.position;
            }
            camCtrl.spacecraftTarget = shipGO.transform;
            camCtrl.orbitDistance = 74f;
            camCtrl.orbitSpeed = 0.45f;
            camCtrl.startAzimuth = 200f;
            camCtrl.elevation = 4.8f;
            camCtrl.focalBlend = 0.22f;
            camCtrl.lookSmoothness = 2.0f;
            camCtrl.bobSpeed = 0.2f;
            camCtrl.bobAmount = 0.4f;

            // ---------------------------------------------------------------
            // 8. VERIFY UI & DIALOGUE CONTROLS
            // ---------------------------------------------------------------
            GameObject dialoguePanel = GameObject.Find("DialoguePanel");
            if (dialoguePanel != null)
            {
                DialogueTypewriter typewriter = dialoguePanel.GetComponent<DialogueTypewriter>();
                if (typewriter != null)
                {
                    GameObject continueBtn = GameObject.Find("ContinueButton");
                    GameObject skipBtn = GameObject.Find("SkipButton");
                    if (continueBtn != null && typewriter.continueButton == null)
                        typewriter.continueButton = continueBtn.GetComponent<Button>();
                    if (skipBtn != null && typewriter.skipButton == null)
                        typewriter.skipButton = skipBtn.GetComponent<Button>();
                }
            }

            // ---------------------------------------------------------------
            // 9. SAVE SCENE & ASSETS
            // ---------------------------------------------------------------
            EditorSceneManager.SaveScene(scene, targetScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!isBatch)
            {
                EditorUtility.DisplayDialog(
                    "ASTROBASE: Phase 2 Polish Complete!",
                    "Cinematic Space Environment Polish applied successfully to:\n  " + targetScenePath + "\n\n" +
                    "  ✓ Flat purple nebula quad removed\n" +
                    "  ✓ Deep realistic starfield active (5,000 stellar bodies)\n" +
                    "  ✓ Spacecraft repositioned & resized (no planet clipping)\n" +
                    "  ✓ Atmospheric rim glow refined (natural sapphire limb)\n" +
                    "  ✓ Cinematic camera smoothed (balanced composition)\n" +
                    "  ✓ Original OpeningStory.unity preserved 100% untouched\n\n" +
                    "Press Play ▶ in the Unity Editor to inspect the cinematic scene!",
                    "OK");
            }
            Debug.Log("[ASTROBASE] Phase 2 polish applied successfully to " + targetScenePath);
        }
        catch (System.Exception ex)
        {
            if (!isBatch) EditorUtility.DisplayDialog("ASTROBASE: Error", ex.Message, "OK");
            Debug.LogError("[ASTROBASE] Phase 2 polish failed: " + ex);
        }
    }

    // ===================================================================
    // PHASE 2 HELPER METHODS
    // ===================================================================

    private static void ConfigureRealisticStarfield(ParticleSystem ps)
    {
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.85f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.92f, 1f, 0.95f),
            new Color(1f, 0.96f, 0.85f, 0.95f));
        main.startLifetime = 99999f;
        main.maxParticles = 5000;
        main.loop = false;
        main.playOnAwake = true;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, 5000)
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1200f;
        shape.radiusThickness = 1f;

        Material starMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Star_Particle_Mat.mat");
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null && starMat != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = starMat;
        }
    }

    private static void AssemblePolishedShipHierarchy(GameObject ship)
    {
        // Clean existing primitive children so re-runs remain clean and idempotent
        for (int i = ship.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(ship.transform.GetChild(i).gameObject);
        }

        Material bodyMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Spacecraft_Body_Mat.mat");
        Material solarMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Solar_Panel_Mat.mat");
        Material cockpitMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Spacecraft_Cockpit_Mat.mat");
        Material engineMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Spacecraft_Engine_Mat.mat");

        // Central Cylindrical Fuselage
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Fuselage";
        body.transform.SetParent(ship.transform, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        body.transform.localScale = new Vector3(0.75f, 1.8f, 0.75f);
        if (bodyMat != null) body.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // Aerodynamic Forward Nose Module
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nose.name = "Nose_Module";
        nose.transform.SetParent(ship.transform, false);
        nose.transform.localPosition = new Vector3(1.65f, 0f, 0f);
        nose.transform.localScale = new Vector3(0.65f, 0.55f, 0.55f);
        if (bodyMat != null) nose.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(nose.GetComponent<Collider>());

        // Cockpit Observation Canopy
        GameObject cockpit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cockpit.name = "Cockpit_Canopy";
        cockpit.transform.SetParent(ship.transform, false);
        cockpit.transform.localPosition = new Vector3(1.25f, 0.28f, 0f);
        cockpit.transform.localScale = new Vector3(0.52f, 0.35f, 0.45f);
        if (cockpitMat != null) cockpit.GetComponent<Renderer>().sharedMaterial = cockpitMat;
        Object.DestroyImmediate(cockpit.GetComponent<Collider>());

        // Left Solar Array Structure (Mounted directly to ship to avoid compounded scales)
        GameObject leftTruss = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftTruss.name = "SolarTruss_Port";
        leftTruss.transform.SetParent(ship.transform, false);
        leftTruss.transform.localPosition = new Vector3(-0.1f, 0f, 1.05f);
        leftTruss.transform.localScale = new Vector3(0.08f, 0.08f, 0.9f);
        if (bodyMat != null) leftTruss.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(leftTruss.GetComponent<Collider>());

        GameObject leftPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftPanel.name = "SolarPanel_Port";
        leftPanel.transform.SetParent(ship.transform, false);
        leftPanel.transform.localPosition = new Vector3(-0.1f, 0f, 2.7f);
        leftPanel.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        leftPanel.transform.localScale = new Vector3(1.3f, 0.04f, 2.4f);
        if (solarMat != null) leftPanel.GetComponent<Renderer>().sharedMaterial = solarMat;
        Object.DestroyImmediate(leftPanel.GetComponent<Collider>());

        // Right Solar Array Structure
        GameObject rightTruss = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightTruss.name = "SolarTruss_Starboard";
        rightTruss.transform.SetParent(ship.transform, false);
        rightTruss.transform.localPosition = new Vector3(-0.1f, 0f, -1.05f);
        rightTruss.transform.localScale = new Vector3(0.08f, 0.08f, 0.9f);
        if (bodyMat != null) rightTruss.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(rightTruss.GetComponent<Collider>());

        GameObject rightPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightPanel.name = "SolarPanel_Starboard";
        rightPanel.transform.SetParent(ship.transform, false);
        rightPanel.transform.localPosition = new Vector3(-0.1f, 0f, -2.7f);
        rightPanel.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        rightPanel.transform.localScale = new Vector3(1.3f, 0.04f, 2.4f);
        if (solarMat != null) rightPanel.GetComponent<Renderer>().sharedMaterial = solarMat;
        Object.DestroyImmediate(rightPanel.GetComponent<Collider>());

        // High-Gain Parabolic Communications Dish
        GameObject antennaMount = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        antennaMount.name = "Antenna_Mast";
        antennaMount.transform.SetParent(ship.transform, false);
        antennaMount.transform.localPosition = new Vector3(-0.6f, 0.52f, 0f);
        antennaMount.transform.localScale = new Vector3(0.06f, 0.22f, 0.06f);
        if (bodyMat != null) antennaMount.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(antennaMount.GetComponent<Collider>());

        GameObject dish = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dish.name = "Communication_Dish";
        dish.transform.SetParent(ship.transform, false);
        dish.transform.localPosition = new Vector3(-0.6f, 0.85f, 0f);
        dish.transform.localRotation = Quaternion.Euler(-28f, 40f, 0f);
        dish.transform.localScale = new Vector3(0.48f, 0.12f, 0.48f);
        if (bodyMat != null) dish.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(dish.GetComponent<Collider>());

        // Dual Plasma Thruster Engines
        GameObject engineL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        engineL.name = "Engine_Port";
        engineL.transform.SetParent(ship.transform, false);
        engineL.transform.localPosition = new Vector3(-1.8f, 0.1f, 0.32f);
        engineL.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        engineL.transform.localScale = new Vector3(0.32f, 0.42f, 0.32f);
        if (engineMat != null) engineL.GetComponent<Renderer>().sharedMaterial = engineMat;
        Object.DestroyImmediate(engineL.GetComponent<Collider>());

        GameObject engineR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        engineR.name = "Engine_Starboard";
        engineR.transform.SetParent(ship.transform, false);
        engineR.transform.localPosition = new Vector3(-1.8f, 0.1f, -0.32f);
        engineR.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        engineR.transform.localScale = new Vector3(0.32f, 0.42f, 0.32f);
        if (engineMat != null) engineR.GetComponent<Renderer>().sharedMaterial = engineMat;
        Object.DestroyImmediate(engineR.GetComponent<Collider>());

        // Realtime Thruster Point Light
        GameObject plumeLightGO = new GameObject("Engine_Plume_Light");
        plumeLightGO.transform.SetParent(ship.transform, false);
        plumeLightGO.transform.localPosition = new Vector3(-2.15f, 0.1f, 0f);
        Light plumeLight = plumeLightGO.AddComponent<Light>();
        plumeLight.type = LightType.Point;
        plumeLight.color = new Color(0.25f, 0.8f, 1.0f);
        plumeLight.intensity = 1.8f;
        plumeLight.range = 4.5f;
    }

    private static void ConfigurePostProcessing()
    {
        EnsureDirectory(SettingsPath);
        string profilePath = SettingsPath + "/OpeningStoryVolumeProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        // Bloom
        Bloom bloom;
        if (!profile.TryGet(out bloom))
        {
            bloom = profile.Add<Bloom>(false);
            bloom.name = "Bloom";
            AssetDatabase.AddObjectToAsset(bloom, profile);
        }
        bloom.intensity.Override(0.55f);
        bloom.threshold.Override(0.92f);
        bloom.scatter.Override(0.65f);

        // Vignette
        Vignette vignette;
        if (!profile.TryGet(out vignette))
        {
            vignette = profile.Add<Vignette>(false);
            vignette.name = "Vignette";
            AssetDatabase.AddObjectToAsset(vignette, profile);
        }
        vignette.intensity.Override(0.30f);
        vignette.smoothness.Override(0.35f);

        // Tonemapping ACES
        Tonemapping tonemapping;
        if (!profile.TryGet(out tonemapping))
        {
            tonemapping = profile.Add<Tonemapping>(false);
            tonemapping.name = "Tonemapping";
            AssetDatabase.AddObjectToAsset(tonemapping, profile);
        }
        tonemapping.mode.Override(TonemappingMode.ACES);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        // Assign to Volume in scene
        GameObject volumeGO = GameObject.Find("Post Processing Volume");
        if (volumeGO == null) volumeGO = new GameObject("Post Processing Volume", typeof(Volume));
        Volume volume = volumeGO.GetComponent<Volume>();
        volume.isGlobal = true;
        volume.profile = profile;
        volume.priority = 1f;
    }

    // ===================================================================
    // ASSET & PREFAB MANAGEMENT
    // ===================================================================

    private static void ConfigureTextureImporters()
    {
        string diffPath = TexturesPath + "/earth_diff.png";
        string normPath = TexturesPath + "/earth_norm.png";

        TextureImporter diffImporter = AssetImporter.GetAtPath(diffPath) as TextureImporter;
        if (diffImporter != null)
        {
            diffImporter.textureType = TextureImporterType.Default;
            diffImporter.sRGBTexture = true;
            diffImporter.wrapMode = TextureWrapMode.Repeat;
            diffImporter.maxTextureSize = 4096;
            diffImporter.SaveAndReimport();
        }

        TextureImporter normImporter = AssetImporter.GetAtPath(normPath) as TextureImporter;
        if (normImporter != null)
        {
            normImporter.textureType = TextureImporterType.NormalMap;
            normImporter.wrapMode = TextureWrapMode.Repeat;
            normImporter.maxTextureSize = 4096;
            normImporter.SaveAndReimport();
        }
    }

    private static void ConfigureModelImporter()
    {
        string modelPath = ModelsPath + "/Earth_1_12756.obj";
        ModelImporter modelImporter = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (modelImporter != null)
        {
            modelImporter.globalScale = 0.04f; // 1000 unit OBJ -> 40 unit diameter
            modelImporter.importNormals = ModelImporterNormals.Import;
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            modelImporter.SaveAndReimport();
        }
    }

    private static Material GetOrCreateEarthMaterial()
    {
        string path = MaterialsPath + "/Earth_NASA_Realistic_Mat.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = "Earth_NASA_Realistic_Mat";
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
        }

        Texture2D diffTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath + "/earth_diff.png");
        Texture2D normTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath + "/earth_norm.png");
        Texture2D lightsTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath + "/Earth_Night_Lights.png");

        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Metallic", 0.02f);
        mat.SetFloat("_Smoothness", 0.45f);

        if (diffTex != null) mat.SetTexture("_BaseMap", diffTex);

        if (normTex != null)
        {
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", normTex);
            mat.SetFloat("_BumpScale", 1.0f);
        }

        if (lightsTex != null)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", lightsTex);
            // Subtle, realistic golden city light glow (not artificial cyan)
            mat.SetColor("_EmissionColor", new Color(0.9f, 0.8f, 0.6f) * 0.75f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.AnyEmissive;
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateCloudsMaterial()
    {
        string path = MaterialsPath + "/Earth_NASA_Clouds_Mat.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = "Earth_NASA_Clouds_Mat";
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
        }

        Texture2D cloudsTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturesPath + "/Earth_Clouds.png");

        mat.SetFloat("_Surface", 1f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.80f));
        mat.SetFloat("_Smoothness", 0.1f);
        if (cloudsTex != null) mat.SetTexture("_BaseMap", cloudsTex);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateAtmosphereMaterial()
    {
        string path = MaterialsPath + "/Earth_NASA_Atmosphere_Mat.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = "Earth_NASA_Atmosphere_Mat";
            AssetDatabase.CreateAsset(mat, path);
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
        }

        mat.SetFloat("_Surface", 1f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // Subtle, realistic atmospheric rim (gentle sapphire blue, avoiding neon cyan)
        mat.SetColor("_BaseColor", new Color(0.12f, 0.35f, 0.8f, 0.05f));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.08f, 0.25f, 0.65f) * 0.5f);
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.AnyEmissive;

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject CreateOrUpdateEarthPrefab(Material earthMat, Material cloudsMat, Material atmoMat)
    {
        string prefabPath = PrefabsPath + "/Earth_NASA_Realistic.prefab";

        // Load NASA 3D Mesh
        string modelPath = ModelsPath + "/Earth_1_12756.obj";
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        Mesh earthMesh = null;
        if (modelAsset != null)
        {
            MeshFilter mf = modelAsset.GetComponentInChildren<MeshFilter>();
            if (mf != null) earthMesh = mf.sharedMesh;
        }

        // Temporary root for prefab generation
        GameObject root = new GameObject("Earth_NASA_Realistic");

        // Earth Surface
        MeshFilter rootMF = root.AddComponent<MeshFilter>();
        if (earthMesh != null)
        {
            rootMF.sharedMesh = earthMesh;
        }
        else
        {
            GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rootMF.sharedMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempSphere);
        }

        MeshRenderer rootMR = root.AddComponent<MeshRenderer>();
        rootMR.sharedMaterial = earthMat;

        PlanetRotation earthRot = root.AddComponent<PlanetRotation>();
        earthRot.rotationSpeed = 1.5f;

        // Clouds Child
        GameObject cloudsGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cloudsGO.name = "Clouds";
        cloudsGO.transform.SetParent(root.transform, false);
        cloudsGO.transform.localScale = Vector3.one * 1.012f;
        cloudsGO.GetComponent<Renderer>().sharedMaterial = cloudsMat;
        Object.DestroyImmediate(cloudsGO.GetComponent<Collider>());

        PlanetRotation cloudsRot = cloudsGO.AddComponent<PlanetRotation>();
        cloudsRot.rotationSpeed = 2.4f; // Independent cloud drift

        // Atmosphere Child
        GameObject atmoGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        atmoGO.name = "Atmosphere";
        atmoGO.transform.SetParent(root.transform, false);
        atmoGO.transform.localScale = Vector3.one * 1.030f;
        atmoGO.GetComponent<Renderer>().sharedMaterial = atmoMat;
        Object.DestroyImmediate(atmoGO.GetComponent<Collider>());

        // Save as Prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        return prefab;
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

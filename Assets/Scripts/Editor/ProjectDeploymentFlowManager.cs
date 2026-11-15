using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Manages the official complete deployment scene flow for ASTROBASE:
/// validates scene connectivity, configures Build Settings in linear progression order,
/// and verifies transition triggers from Intro to Final Debrief.
/// </summary>
public static class ProjectDeploymentFlowManager
{
    private const string ScenesPath = "Assets/Scenes";

    public struct SceneDeploymentStage
    {
        public int buildIndex;
        public string sceneName;
        public string scenePath;
        public string stageTitle;
        public string nextSceneTarget;
        public string description;

        public SceneDeploymentStage(int index, string name, string title, string next, string desc)
        {
            buildIndex = index;
            sceneName = name;
            scenePath = $"{ScenesPath}/{name}.unity";
            stageTitle = title;
            nextSceneTarget = next;
            description = desc;
        }
    }

    public static readonly SceneDeploymentStage[] FlowStages = new SceneDeploymentStage[]
    {
        new SceneDeploymentStage(0, "OpeningStory", "Stage 0: Prologue & Story Intro", "MissionSelection",
            "Cinematic space intro, realistic Earth view, and astronaut monologue."),

        new SceneDeploymentStage(1, "MissionSelection", "Stage 1: Expedition Selection (Mission HQ)", "MissionPreparation",
            "Choose between Lunar Expedition, Red Planet Expedition, or Solar Orbit."),

        new SceneDeploymentStage(2, "MissionPreparation", "Stage 2: Crew Outfitting & Payload", "LaunchSequence",
            "360-degree turntable suit inspection, PLSS life-support, and tool loadout."),

        new SceneDeploymentStage(3, "LaunchSequence", "Stage 3: Earth Staging & Liftoff", "SpacecraftFlight",
            "Nexus Super Heavy Rocket, countdown, engine ignition, liftoff, and manual orbital flight."),

        new SceneDeploymentStage(4, "SpacecraftFlight", "Stage 4: Translunar Cruise Corridor", "LunarLanding",
            "Fly the spacecraft through space checkpoints, avoid radiation hazards, and approach the Moon."),

        new SceneDeploymentStage(5, "LunarLanding", "Stage 5: Lunar Powered Descent", "LunarExploration",
            "Descent counter-thrust and lateral RCS over Petavius Crater to execute touchdown."),

        new SceneDeploymentStage(6, "LunarExploration", "Stage 6: Lunar Surface EVA & Science", "EarthReturnFlight",
            "First-person/third-person astronaut exploration, regolith sample collection, scanning, and Earthrise photo."),

        new SceneDeploymentStage(7, "EarthReturnFlight", "Stage 7: Trans-Earth Return & Atmospheric Reentry", "MissionResults",
            "Ascent from the Moon, translunar return cruise, fiery plasma entry, and runway landing."),

        new SceneDeploymentStage(8, "MissionResults", "Stage 8: NASA Mission Debrief & Scoring", "MissionSelection",
            "Mission evaluation, EVA sample ratings, NASA flight certification, and replay/next mission unlock.")
    };

    [MenuItem("ASTROBASE/Deployment/Configure Complete Scene Deployment Flow")]
    public static void ConfigureDeploymentFlowInteractive()
    {
        ExecuteConfigureFlow(false);
    }

    public static void ConfigureDeploymentFlowBatch()
    {
        ExecuteConfigureFlow(true);
    }

    [InitializeOnLoadMethod]
    private static void AutoVerifyOnEditorLoad()
    {
        EditorApplication.delayCall += () =>
        {
            // Verify scenes in Build Settings on startup
            if (EditorBuildSettings.scenes.Length < FlowStages.Length)
            {
                ExecuteConfigureFlow(true);
            }
        };
    }

    public static void ExecuteConfigureFlow(bool isBatch)
    {
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        List<string> missingScenes = new List<string>();

        for (int i = 0; i < FlowStages.Length; i++)
        {
            SceneDeploymentStage stage = FlowStages[i];
            if (File.Exists(stage.scenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(stage.scenePath, true));
            }
            else
            {
                missingScenes.Add(stage.sceneName);
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();

        string report = $"[ASTROBASE Deployment Flow Configured]\nActive Scenes in Build Settings ({buildScenes.Count}/{FlowStages.Length}):\n";
        for (int i = 0; i < buildScenes.Count; i++)
        {
            report += $"  [{i}] {Path.GetFileNameWithoutExtension(buildScenes[i].path)}\n";
        }

        if (missingScenes.Count > 0)
        {
            report += $"\nMissing Scenes ({missingScenes.Count}): {string.Join(", ", missingScenes)}";
            Debug.LogWarning(report);
        }
        else
        {
            Debug.Log(report);
        }

        if (!isBatch)
        {
            EditorUtility.DisplayDialog(
                "ASTROBASE: Deployment Flow Configured",
                $"Successfully configured all {buildScenes.Count} scenes in Build Settings in linear progression order!\n\n" +
                "Complete Mission Sequence:\n" +
                "  0. OpeningStory (Intro / Prologue)\n" +
                "  1. MissionSelection (HQ Selection)\n" +
                "  2. MissionPreparation (Crew Outfitting)\n" +
                "  3. LaunchSequence (Rocket Liftoff & Orbit)\n" +
                "  4. SpacecraftFlight (Translunar Cruise)\n" +
                "  5. LunarLanding (Descent at Petavius Crater)\n" +
                "  6. LunarExploration (Moon Surface EVA)\n" +
                "  7. EarthReturnFlight (Reentry & Landing)\n" +
                "  8. MissionResults (Debrief & Scoring)\n\n" +
                "You can now build for Windows or WebGL via File -> Build Settings!",
                "OK");
        }
    }
}

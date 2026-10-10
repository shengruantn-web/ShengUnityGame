using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VillageClash.Editor
{
    /// <summary>Headless project setup: creates the single empty Main scene, registers it in build settings, builds the macOS player.</summary>
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Village Clash/Create Main Scene")]
        public static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.36f, 0.62f, 0.23f);
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0, 0, -10);
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Village Clash";
            PlayerSettings.companyName = "VillageClash";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Main scene created at " + ScenePath);
        }

        [MenuItem("Village Clash/Build macOS")]
        public static void BuildMac()
        {
            if (!File.Exists(ScenePath)) CreateMainScene();
            var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
            Directory.CreateDirectory(outDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(outDir, "VillageClash.app"),
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[ProjectSetup] Build result: " + report.summary.result + " size=" + report.summary.totalSize + " errors=" + report.summary.totalErrors);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}

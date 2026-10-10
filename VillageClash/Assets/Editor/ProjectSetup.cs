using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VillageClash.Editor
{
    /// <summary>Headless project setup: creates the single empty Main scene, registers it in build settings, applies release settings, builds the macOS player.</summary>
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string IconPath = "Assets/Icon/AppIcon.png";
        public const string Version = "1.0.0";
        public const string BundleId = "com.shengruantn.villageclash";

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

        /// <summary>Release identity: bundle id, version, build number, app icon. Idempotent; runs before every build.</summary>
        [MenuItem("Village Clash/Configure Release Settings")]
        public static void ConfigureRelease()
        {
            var standalone = NamedBuildTarget.Standalone;
            PlayerSettings.productName = "Village Clash";
            PlayerSettings.companyName = "VillageClash";
            PlayerSettings.SetApplicationIdentifier(standalone, BundleId);
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.macOS.buildNumber = "1";
            PlayerSettings.macOS.applicationCategoryType = "public.app-category.games";

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
            {
                Debug.LogError("[ProjectSetup] App icon missing at " + IconPath + " (regenerate with scripts/make_icon.py)");
            }
            else
            {
                var sizes = PlayerSettings.GetIconSizes(standalone, IconKind.Any);
                var icons = new Texture2D[sizes.Length];
                for (int i = 0; i < icons.Length; i++) icons[i] = icon;
                PlayerSettings.SetIcons(standalone, icons, IconKind.Any);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Release settings applied: " + BundleId + " v" + Version + " icon=" + (icon != null));
        }

        [MenuItem("Village Clash/Build macOS")]
        public static void BuildMac()
        {
            if (!File.Exists(ScenePath)) CreateMainScene();
            ConfigureRelease();
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
            {
                EditorApplication.Exit(1);
                return;
            }
            // Burst debug symbols are not part of the shipped package.
            foreach (var dir in Directory.GetDirectories(outDir, "*_BurstDebugInformation_DoNotShip"))
            {
                Directory.Delete(dir, true);
                Debug.Log("[ProjectSetup] Removed " + Path.GetFileName(dir));
            }
        }
    }
}

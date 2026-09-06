using System;
using System.IO;
using System.Linq;
using FunGame.Content;
using FunGame.Demo;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Editor
{
    /// <summary>只组合人工维护的模块；不会调用灰盒或 Prefab 生成器。</summary>
    public static class ModularContentBuilder
    {
        public const string ContentFolder = "Assets/Game/Content/Modules";
        public const string EnvironmentFolder = "Assets/Game/Scenes/Environment";
        public static string[] BuildScenes => new[] { SinglePlayerDemoBootstrap.ScenePath }
            .Concat(Definitions().Select(d => d.ScenePath)).ToArray();

        public static EnvironmentSceneDefinition[] Definitions() => AssetDatabase
            .FindAssets("t:EnvironmentSceneDefinition", new[] { ContentFolder })
            .Select(guid => AssetDatabase.LoadAssetAtPath<EnvironmentSceneDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
            .OrderBy(d => d.Chapter).ThenBy(d => d.ScenePath, StringComparer.Ordinal).ToArray();

        [MenuItem("FunGame/Modules/Generate Environment Scenes")]
        public static void GenerateEnvironmentScenes()
        {
            Scene active = SceneManager.GetActiveScene();
            try
            {
                foreach (EnvironmentSceneDefinition definition in Definitions()) GenerateEnvironmentScene(definition);
            }
            finally { if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
        }

        public static void GenerateEnvironmentScene(EnvironmentSceneDefinition definition)
        {
            if (definition.EnvironmentPrefab == null || !definition.ScenePath.StartsWith(EnvironmentFolder + "/", StringComparison.Ordinal))
                throw new InvalidDataException("Environment output must stay within " + EnvironmentFolder);
            if (definition.EnvironmentPrefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                throw new InvalidDataException("Environment prefabs must contain static presentation only.");
            if (File.Exists(definition.ScenePath))
            {
                Scene existing = SceneManager.GetSceneByPath(definition.ScenePath);
                bool wasOpen = existing.IsValid() && existing.isLoaded;
                if (!wasOpen) existing = EditorSceneManager.OpenScene(definition.ScenePath, OpenSceneMode.Additive);
                try
                {
                    GameObject[] roots = existing.GetRootGameObjects();
                    if (roots.Length != 1 || PrefabUtility.GetCorrespondingObjectFromSource(roots[0]) != definition.EnvironmentPrefab
                        || PrefabUtility.HasPrefabInstanceAnyOverrides(roots[0], false))
                        throw new InvalidDataException("Environment scene was hand edited. Move changes into its source prefab: " + definition.ScenePath);
                    // Prefab instances automatically reflect source changes. Never reserialize a clean scene.
                    return;
                }
                finally { if (!wasOpen) EditorSceneManager.CloseScene(existing, true); }
            }
            Directory.CreateDirectory(EnvironmentFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            PrefabUtility.InstantiatePrefab(definition.EnvironmentPrefab, scene);
            EditorSceneManager.SaveScene(scene, definition.ScenePath);
            EditorSceneManager.CloseScene(scene, true);
        }

        public static void RequireLegacyWorkspace()
        {
            if (Directory.Exists(ContentFolder))
                throw new InvalidOperationException("This workspace uses authored modules. Edit module prefabs/configuration; legacy full-scene generators are disabled.");
        }

        public static void BuildWindows(string executable)
        {
            if (!Directory.Exists(ContentFolder)) throw new InvalidDataException("Run the explicit collaboration migration first.");
            foreach (string path in BuildScenes)
                if (!File.Exists(path)) throw new InvalidDataException("Missing composed scene: " + path);
            Directory.CreateDirectory(Path.GetDirectoryName(executable));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = BuildScenes,
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Modular build failed: " + report.summary.result);
            WindowsFirewallHelperBuilder.Build(executable);
            Debug.Log("[Modules] Windows build succeeded: " + report.summary.totalSize + " bytes.");
        }
    }
}

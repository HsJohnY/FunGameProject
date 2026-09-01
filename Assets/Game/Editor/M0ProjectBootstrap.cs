using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FunGame.Editor
{
    public static class M0ProjectBootstrap
    {
        private const string SettingsFolder = "Assets/Game/Settings";
        private const string ScenesFolder = "Assets/Game/Scenes";
        private const string RendererPath = SettingsFolder + "/FunGameUniversalRenderer.asset";
        private const string PipelinePath = SettingsFolder + "/FunGameUniversalRenderPipeline.asset";
        private const string ScenePath = ScenesFolder + "/M0_Bootstrap.unity";

        public static void Configure()
        {
            EnsureFolder(SettingsFolder);
            EnsureFolder(ScenesFolder);

            UniversalRenderPipelineAsset pipelineAsset = CreateOrLoadPipeline();
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            QualitySettings.renderPipeline = pipelineAsset;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            SetActiveInputHandlerToInputSystem();
            MoveGeneratedAssetIfNeeded(
                "Assets/UniversalRenderPipelineGlobalSettings.asset",
                SettingsFolder + "/UniversalRenderPipelineGlobalSettings.asset");
            MoveGeneratedAssetIfNeeded(
                "Assets/DefaultVolumeProfile.asset",
                SettingsFolder + "/DefaultVolumeProfile.asset");

            CreateValidationScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M0] Technical baseline configured successfully.");
        }

        public static void BuildWindowsDevelopment()
        {
            Configure();
            Directory.CreateDirectory("Builds/M0-Windows");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/M0-Windows/FunGame-M0.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new BuildFailedException($"M0 build failed: {report.summary.result}");
            }

            Debug.Log($"[M0] Windows development build succeeded: {report.summary.totalSize} bytes.");
        }

        private static UniversalRenderPipelineAsset CreateOrLoadPipeline()
        {
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (existing != null)
            {
                return existing;
            }

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, PipelinePath);
            return pipeline;
        }

        private static void SetActiveInputHandlerToInputSystem()
        {
            Object[] settingsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settingsAssets.Length == 0)
            {
                throw new InvalidDataException("Could not load ProjectSettings.asset.");
            }

            var serializedSettings = new SerializedObject(settingsAssets[0]);
            SerializedProperty activeInputHandler = serializedSettings.FindProperty("activeInputHandler");
            if (activeInputHandler == null)
            {
                throw new InvalidDataException("Could not find activeInputHandler in ProjectSettings.asset.");
            }

            activeInputHandler.intValue = 1;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateValidationScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "M0_Bootstrap";

            var root = new GameObject("M0 Technical Baseline");
            root.AddComponent<ProjectBootstrapMarker>();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 3f, -6f), Quaternion.Euler(15f, 0f, 0f));

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Validation Floor";

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Validation Cube";
            cube.transform.position = new Vector3(0f, 0.5f, 0f);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"Could not save validation scene at {ScenePath}.");
            }
        }

        private static void EnsureFolder(string assetPath)
        {
            string current = "Assets";
            string[] parts = assetPath.Split('/');
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static void MoveGeneratedAssetIfNeeded(string sourcePath, string destinationPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(sourcePath) == null ||
                AssetDatabase.LoadMainAssetAtPath(destinationPath) != null)
            {
                return;
            }

            string error = AssetDatabase.MoveAsset(sourcePath, destinationPath);
            if (!string.IsNullOrEmpty(error))
            {
                throw new IOException($"Could not move generated asset to {destinationPath}: {error}");
            }
        }
    }
}

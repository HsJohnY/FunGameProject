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
    /// <summary>
    /// 创建 M0 门禁所需的确定性项目设置、验证场景和构建。
    /// 将初始化过程保存在代码中，可让全新克隆复现相同的 Unity 资产。
    /// </summary>
    public static class M0ProjectBootstrap
    {
        private const string SettingsFolder = "Assets/Game/Settings";
        private const string ScenesFolder = "Assets/Game/Scenes";
        private const string RendererPath = SettingsFolder + "/FunGameUniversalRenderer.asset";
        private const string PipelinePath = SettingsFolder + "/FunGameUniversalRenderPipeline.asset";
        private const string ScenePath = ScenesFolder + "/M0_Bootstrap.unity";

        /// <summary>
        /// 应用 M0 项目基线并重新生成验证场景。
        /// 此方法是 Initialize-M0.ps1 使用的命令行入口。
        /// </summary>
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

        /// <summary>
        /// 生成供 M0 运行时冒烟检查使用的 Windows 开发版本。
        /// </summary>
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
            // Unity 6 没有通过稳定的公共 PlayerSettings 属性开放这个项目级选项，
            // 因此需要使用 Unity 自身的序列化键修改该设置。
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
            // URP 首次启用时会在 Assets 根目录生成这些资产。通过 AssetDatabase 移动
            // 可以保留 GUID 引用，同时维持项目目录整洁。
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

using System.IO;
using FunGame.Diagnostics;
using FunGame.Interaction;
using FunGame.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FunGame.Editor
{
    /// <summary>
    /// 生成当前 M1 灰盒检查点所需的冷却舱场景。
    /// </summary>
    public static class M1GrayboxBootstrap
    {
        private const string ScenePath = "Assets/Game/Scenes/M1_CoolingBay.unity";
        private const string MaterialFolder = "Assets/Game/Content/Graybox";

        [MenuItem("FunGame/M1/生成当前冷却舱场景")]
        public static void ConfigureCurrent()
        {
            EnsureFolder(MaterialFolder);

            Material structureMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/M1_Structure.mat", new Color(0.22f, 0.26f, 0.3f));
            Material floorMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/M1_Floor.mat", new Color(0.1f, 0.12f, 0.14f));
            Material machineryMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/M1_Machinery.mat", new Color(0.12f, 0.42f, 0.48f));
            Material warningMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/M1_Warning.mat", new Color(0.85f, 0.38f, 0.08f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "M1_CoolingBay";

            CreateCheckpoint();
            CreateLighting();
            CreateCoolingBay(structureMaterial, floorMaterial, machineryMaterial, warningMaterial);
            CreatePlayer();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"无法保存 M1-1 场景：{ScenePath}");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M1-2] 冷却舱交互灰盒场景生成成功。");
        }

        private static void CreateCheckpoint()
        {
            var checkpointObject = new GameObject("M1-2 Development Checkpoint");
            checkpointObject.AddComponent<DevelopmentCheckpoint>()
                .Configure("m1-2-context-interaction", "--m1-2-smoke");
        }

        private static void CreateLighting()
        {
            var directionalObject = new GameObject("Directional Light");
            var directional = directionalObject.AddComponent<Light>();
            directionalObject.AddComponent<UniversalAdditionalLightData>();
            directional.type = LightType.Directional;
            directional.intensity = 0.45f;
            directional.color = new Color(0.58f, 0.7f, 0.85f);
            directionalObject.transform.rotation = Quaternion.Euler(55f, -25f, 0f);

            CreatePointLight("Cold Work Light A", new Vector3(-4f, 4f, -5f));
            CreatePointLight("Cold Work Light B", new Vector3(4f, 4f, 4f));
        }

        private static void CreatePointLight(string name, Vector3 position)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.position = position;
            var light = lightObject.AddComponent<Light>();
            lightObject.AddComponent<UniversalAdditionalLightData>();
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = 5f;
            light.color = new Color(0.62f, 0.82f, 1f);
        }

        private static void CreateCoolingBay(
            Material structureMaterial,
            Material floorMaterial,
            Material machineryMaterial,
            Material warningMaterial)
        {
            var environment = new GameObject("Cooling Bay Graybox").transform;

            CreateBlock(environment, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(14f, 0.5f, 20f), floorMaterial);
            CreateBlock(environment, "Ceiling", new Vector3(0f, 5f, 0f), new Vector3(14f, 0.5f, 20f), structureMaterial);
            CreateBlock(environment, "Left Wall", new Vector3(-7f, 2.5f, 0f), new Vector3(0.5f, 5f, 20f), structureMaterial);
            CreateBlock(environment, "Right Wall", new Vector3(7f, 2.5f, 0f), new Vector3(0.5f, 5f, 20f), structureMaterial);
            CreateBlock(environment, "Rear Wall", new Vector3(0f, 2.5f, 10f), new Vector3(14f, 5f, 0.5f), structureMaterial);
            CreateBlock(environment, "Entry Wall", new Vector3(0f, 2.5f, -10f), new Vector3(14f, 5f, 0.5f), structureMaterial);

            // 这些色块只承担空间识别功能，后续维修阶段会逐步替换。
            CreateBlock(environment, "Cooling Pump Placeholder", new Vector3(0f, 1f, 5.8f), new Vector3(4f, 2f, 2.5f), machineryMaterial);
            GameObject console = CreateBlock(environment, "Interactive Control Console", new Vector3(-4.8f, 1f, 2.2f), new Vector3(1.8f, 2f, 2f), warningMaterial);
            console.AddComponent<ToggleConsoleInteractable>();
            CreateBlock(environment, "Tool Rack Placeholder", new Vector3(5.5f, 1.1f, -2.5f), new Vector3(1.2f, 2.2f, 4f), machineryMaterial);
            CreateBlock(environment, "Pipe Rack Placeholder", new Vector3(-5.5f, 1.1f, -4.5f), new Vector3(1.2f, 2.2f, 4f), machineryMaterial);

            GameObject carryable = CreateBlock(environment, "Carryable Test Pipe", new Vector3(1.8f, 0.4f, -3.8f), new Vector3(0.7f, 0.7f, 0.7f), warningMaterial);
            var itemBody = carryable.AddComponent<Rigidbody>();
            itemBody.mass = 3f;
            carryable.AddComponent<CarryableInteractable>();

            CreateBlock(environment, "Walkway A", new Vector3(-3f, 0.15f, 0f), new Vector3(0.18f, 0.3f, 16f), warningMaterial);
            CreateBlock(environment, "Walkway B", new Vector3(3f, 0.15f, 0f), new Vector3(0.18f, 0.3f, 16f), warningMaterial);
        }

        private static void CreatePlayer()
        {
            var player = new GameObject("Local First Person Player");
            player.transform.position = new Vector3(0f, 0.05f, -7f);

            var characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.3f;

            var cameraObject = new GameObject("First Person Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraObject.AddComponent<AudioListener>();

            player.AddComponent<FirstPersonController>();
            player.AddComponent<ContextInteractor>();
            player.AddComponent<ContextPromptOverlay>();
        }

        private static GameObject CreateBlock(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.SetPositionAndRotation(position, Quaternion.identity);
            block.transform.localScale = scale;
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            return block;
        }

        private static Material CreateOrLoadMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidDataException("无法找到 URP Lit Shader。");
            }

            var material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
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
    }
}

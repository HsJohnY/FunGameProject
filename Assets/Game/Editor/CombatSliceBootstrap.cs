using System.IO;
using FunGame.Combat;
using FunGame.Diagnostics;
using FunGame.Interaction;
using FunGame.Player;
using FunGame.Tools;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FunGame.Editor
{
    /// <summary>
    /// 生成与 M1 冷却事故隔离的最小战斗切片，验证冲击扳手防卫和设备干扰闭环。
    /// </summary>
    public static class CombatSliceBootstrap
    {
        private const string ScenePath = "Assets/Game/Scenes/Combat_DefenseSandbox.unity";
        private const string MaterialFolder = "Assets/Game/Content/Graybox";

        [MenuItem("FunGame/Combat/生成基础防卫场景")]
        public static void ConfigureCurrent()
        {
            EnsureFolder(MaterialFolder);

            Material structureMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Combat_Structure.mat", new Color(0.16f, 0.19f, 0.23f));
            Material floorMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Combat_Floor.mat", new Color(0.08f, 0.1f, 0.12f));
            Material systemMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Combat_System.mat", new Color(0.1f, 0.8f, 0.55f));
            Material warningMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Combat_Warning.mat", new Color(0.9f, 0.42f, 0.08f));
            Material enemyMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Combat_Enemy.mat", new Color(0.7f, 0.1f, 0.85f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Combat_DefenseSandbox";

            CreateCheckpoint();
            CreateLighting();
            CreateRoom(structureMaterial, floorMaterial, warningMaterial);

            var encounterObject = new GameObject("Defense Encounter");
            var encounter = encounterObject.AddComponent<CombatEncounterController>();

            GameObject systemObject = CreateBlock(
                null,
                "Defendable Cooling Control Unit",
                new Vector3(0f, 1f, 4.7f),
                new Vector3(3.2f, 2f, 1.6f),
                systemMaterial);
            var defenseTarget = systemObject.AddComponent<DefendableSystemTarget>();
            defenseTarget.Configure(60);

            GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = "Line Interference Creature";
            enemyObject.transform.position = new Vector3(0f, 1f, 0.6f);
            enemyObject.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            enemyObject.GetComponent<Renderer>().sharedMaterial = enemyMaterial;
            var enemy = enemyObject.AddComponent<InterferenceEnemy>();

            encounter.Configure(defenseTarget, enemy);
            enemy.Configure(defenseTarget, encounter);

            GameObject wrenchRack = CreateBlock(
                null,
                "Impact Wrench Rack",
                new Vector3(-2.2f, 1f, -4.2f),
                new Vector3(0.7f, 1.8f, 0.8f),
                warningMaterial);
            wrenchRack.AddComponent<ToolRackInteractable>().Configure("combat-impact-wrench-rack", ToolKind.ImpactWrench);

            GameObject resetConsole = CreateBlock(
                null,
                "Combat Training Console",
                new Vector3(3.8f, 1f, -2.8f),
                new Vector3(1.2f, 2f, 1.2f),
                warningMaterial);
            resetConsole.AddComponent<CombatResetConsoleInteractable>().Configure(encounter);

            CreatePlayer(warningMaterial, systemMaterial, encounter);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"无法保存基础战斗场景：{ScenePath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Combat] 基础防卫灰盒场景生成成功。");
        }

        /// <summary>
        /// 仅在战斗切片需要阶段性交付时生成独立 Windows 开发构建。
        /// </summary>
        [MenuItem("FunGame/Combat/构建 Windows 开发版本")]
        public static void BuildWindowsDevelopment()
        {
            // 构建已有评审场景，避免每次构建重建全部 GameObject fileID 并制造协作冲突。
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                ConfigureCurrent();
            }

            Directory.CreateDirectory("Builds/Combat-Windows");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Combat-Windows/FunGame-Combat.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"基础战斗构建失败：{report.summary.result}");
            }

            Debug.Log($"[Combat] Windows 开发构建成功：{report.summary.totalSize} bytes。");
        }

        private static void CreateCheckpoint()
        {
            var checkpointObject = new GameObject("Combat Development Checkpoint");
            checkpointObject.AddComponent<DevelopmentCheckpoint>()
                .Configure("combat-defense-slice-candidate", "--combat-smoke");
        }

        private static void CreateLighting()
        {
            var directionalObject = new GameObject("Directional Light");
            var directional = directionalObject.AddComponent<Light>();
            directionalObject.AddComponent<UniversalAdditionalLightData>();
            directional.type = LightType.Directional;
            directional.intensity = 0.5f;
            directional.color = new Color(0.62f, 0.72f, 0.9f);
            directionalObject.transform.rotation = Quaternion.Euler(55f, -25f, 0f);

            var workLightObject = new GameObject("Defense Work Light");
            workLightObject.transform.position = new Vector3(0f, 4f, 0f);
            var workLight = workLightObject.AddComponent<Light>();
            workLightObject.AddComponent<UniversalAdditionalLightData>();
            workLight.type = LightType.Point;
            workLight.range = 14f;
            workLight.intensity = 7f;
            workLight.color = new Color(0.65f, 0.82f, 1f);
        }

        private static void CreateRoom(Material structureMaterial, Material floorMaterial, Material warningMaterial)
        {
            var room = new GameObject("Defense Sandbox Graybox").transform;
            CreateBlock(room, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(12f, 0.5f, 14f), floorMaterial);
            CreateBlock(room, "Ceiling", new Vector3(0f, 4.5f, 0f), new Vector3(12f, 0.5f, 14f), structureMaterial);
            CreateBlock(room, "Left Wall", new Vector3(-6f, 2.25f, 0f), new Vector3(0.5f, 4.5f, 14f), structureMaterial);
            CreateBlock(room, "Right Wall", new Vector3(6f, 2.25f, 0f), new Vector3(0.5f, 4.5f, 14f), structureMaterial);
            CreateBlock(room, "Rear Wall", new Vector3(0f, 2.25f, 7f), new Vector3(12f, 4.5f, 0.5f), structureMaterial);
            CreateBlock(room, "Entry Wall", new Vector3(0f, 2.25f, -7f), new Vector3(12f, 4.5f, 0.5f), structureMaterial);
            CreateBlock(room, "Danger Lane Left", new Vector3(-1.4f, 0.03f, 0.5f), new Vector3(0.12f, 0.06f, 9f), warningMaterial);
            CreateBlock(room, "Danger Lane Right", new Vector3(1.4f, 0.03f, 0.5f), new Vector3(0.12f, 0.06f, 9f), warningMaterial);
        }

        private static void CreatePlayer(
            Material warningMaterial,
            Material systemMaterial,
            CombatEncounterController encounter)
        {
            var player = new GameObject("Local First Person Player");
            player.transform.position = new Vector3(0f, 0.05f, -5.4f);

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

            var toolAnchorObject = new GameObject("Main Tool Visual Anchor");
            toolAnchorObject.transform.SetParent(cameraObject.transform, false);
            toolAnchorObject.transform.localPosition = new Vector3(-0.38f, -0.3f, 0.78f);

            GameObject wrenchVisual = CreateVisualCube(
                toolAnchorObject.transform,
                "Impact Wrench Visual",
                new Vector3(0f, 0f, 0.15f),
                new Vector3(0.12f, 0.12f, 0.65f),
                warningMaterial);
            GameObject sealantVisual = CreateVisualCube(
                toolAnchorObject.transform,
                "Sealant Gun Visual",
                new Vector3(0f, -0.03f, 0.12f),
                new Vector3(0.22f, 0.18f, 0.5f),
                systemMaterial);

            var toolbelt = player.AddComponent<PlayerToolbelt>();
            toolbelt.ConfigureVisuals(wrenchVisual, sealantVisual);
            player.AddComponent<FirstPersonController>();
            player.AddComponent<ContextInteractor>();
            player.AddComponent<ToolController>();
            player.AddComponent<ContextPromptOverlay>();
            player.AddComponent<CombatStatusOverlay>().Configure(encounter);
        }

        private static GameObject CreateVisualCube(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = localScale;
            visual.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            return visual;
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
            if (parent != null)
            {
                block.transform.SetParent(parent);
            }

            block.transform.SetPositionAndRotation(position, Quaternion.identity);
            block.transform.localScale = scale;
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            return block;
        }

        private static Material CreateOrLoadMaterial(string path, Color color)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
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

using System.IO;
using FunGame.Audio;
using FunGame.Diagnostics;
using FunGame.Interaction;
using FunGame.Player;
using FunGame.Tools;
using FunGame.Incident;
using FunGame.UI;
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
    /// 生成当前 M1 灰盒检查点所需的冷却舱场景。
    /// </summary>
    public static class M1GrayboxBootstrap
    {
        private const string ScenePath = "Assets/Game/Scenes/M1_CoolingBay.unity";
        private const string MaterialFolder = "Assets/Game/Content/Graybox";
        private const string MenuBgmPath = "Assets/ThirdParty/OpenGameArt/WackyWobblings/wackywobblings.ogg";
        private const string GameplayBgmPath = "Assets/ThirdParty/OpenGameArt/FuturePower/futurepower_loop.ogg";

        [MenuItem("FunGame/M1/生成当前冷却舱场景")]
        public static void ConfigureCurrent()
        {
            EnsureFolder(MaterialFolder);
            AudioClip[] bgmClips = ProceduralBgmAssetBuilder.GenerateOrRefresh();
            AudioClip menuBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuBgmPath);
            AudioClip gameplayBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(GameplayBgmPath);
            if (menuBgm == null || gameplayBgm == null)
            {
                throw new InvalidDataException("缺少已核准的 OpenGameArt BGM 资源，请检查 Assets/ThirdParty。");
            }

            Material structureMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/M1_Structure.mat", new Color(0.22f, 0.26f, 0.3f));
            Material floorMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/M1_Floor.mat", new Color(0.1f, 0.12f, 0.14f));
            Material machineryMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/M1_Machinery.mat", new Color(0.12f, 0.42f, 0.48f));
            Material warningMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/M1_Warning.mat", new Color(0.85f, 0.38f, 0.08f));
            Material trimMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/M1_Trim.mat", new Color(0.34f, 0.19f, 0.12f));
            Material glowMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/M1_Glow.mat", new Color(0.3f, 0.9f, 0.82f));
            Material circuitMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/M1_Circuit.mat", new Color(0.58f, 0.18f, 0.78f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "M1_CoolingBay";

            CreateCheckpoint();
            CreateLighting();
            var incidentObject = new GameObject("Cooling Incident");
            var incident = incidentObject.AddComponent<CoolingIncidentController>();
            incident.ConfigureExtendedIncident(true);
            CoolingIncidentLayoutController layout = CreateCoolingBay(
                structureMaterial, floorMaterial, machineryMaterial, warningMaterial, circuitMaterial, incident);
            CoolingBayArtBuilder.BuildEnvironment(
                structureMaterial, machineryMaterial, warningMaterial, trimMaterial, glowMaterial, circuitMaterial);
            FirstPersonController player = CreatePlayer(warningMaterial, machineryMaterial, circuitMaterial, incident);
            layout.ConfigurePlayer(player.GetComponent<ContextInteractor>());
            CoolingBayArtBuilder.EnhanceFirstPersonTools(machineryMaterial, warningMaterial, trimMaterial, circuitMaterial);
            var bgm = new GameObject("Adaptive Cooling Bay BGM").AddComponent<CoolingBayBgmController>();
            bgm.Configure(incident);
            bgm.ConfigureMusicAssets(menuBgm, gameplayBgm, bgmClips[1]);
            new GameObject("Main and Pause Menu").AddComponent<GameMenuController>().Configure(player);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"无法保存 M1-1 场景：{ScenePath}");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M1-6] 冷却舱完整灰盒候选场景生成成功。");
        }

        /// <summary>
        /// 生成 M1 完整灰盒的 Windows x64 开发构建。
        /// </summary>
        public static void BuildWindowsDevelopment()
        {
            ConfigureCurrent();
            Directory.CreateDirectory("Builds/M1-Windows");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/M1-Windows/FunGame-M1.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"M1 构建失败：{report.summary.result}");
            }

            Debug.Log($"[M1-6] Windows 开发构建成功：{report.summary.totalSize} bytes。");
        }

        private static void CreateCheckpoint()
        {
            var checkpointObject = new GameObject("M1-6 Development Checkpoint");
            checkpointObject.AddComponent<DevelopmentCheckpoint>()
                .Configure("m1-6-graybox-candidate", "--m1-6-smoke");
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

        private static CoolingIncidentLayoutController CreateCoolingBay(
            Material structureMaterial,
            Material floorMaterial,
            Material machineryMaterial,
            Material warningMaterial,
            Material circuitMaterial,
            CoolingIncidentController incident)
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
            console.AddComponent<ToggleConsoleInteractable>().Configure(incident);
            GameObject pressureGauge = CreateBlock(
                environment,
                "Diagnostic Pressure Gauge",
                new Vector3(-4.15f, 1.65f, 1.15f),
                new Vector3(0.55f, 0.55f, 0.25f),
                machineryMaterial);
            pressureGauge.AddComponent<CoolingDiagnosticInteractable>().Configure(
                incident,
                CoolingDiagnosticInteractable.DiagnosticKind.PressureGauge);
            ConfigureBoxCollider(pressureGauge, Vector3.zero, new Vector3(1.25f, 1.25f, 2.2f));
            GameObject pumpInspection = CreateBlock(
                environment,
                "Cooling Pump Inspection Panel",
                new Vector3(1.55f, 1.25f, 4.45f),
                new Vector3(1.05f, 0.75f, 0.2f),
                warningMaterial);
            pumpInspection.AddComponent<CoolingDiagnosticInteractable>().Configure(
                incident,
                CoolingDiagnosticInteractable.DiagnosticKind.PumpHousing);
            ConfigureBoxCollider(pumpInspection, new Vector3(0f, 0f, -0.3f), new Vector3(1f, 1f, 1.75f));
            CreateBlock(environment, "Tool Rack Base", new Vector3(5.7f, 1.1f, -2.5f), new Vector3(0.8f, 2.2f, 4f), structureMaterial);
            CreateBlock(environment, "Pipe Rack Placeholder", new Vector3(-5.5f, 1.1f, -4.5f), new Vector3(1.2f, 2.2f, 4f), machineryMaterial);

            GameObject wrenchRack = CreateBlock(environment, "Impact Wrench Rack", new Vector3(5.15f, 1f, -3.65f), new Vector3(0.35f, 0.9f, 0.8f), warningMaterial);
            wrenchRack.AddComponent<ToolRackInteractable>().Configure("impact-wrench-rack", ToolKind.ImpactWrench);
            ConfigureBoxCollider(wrenchRack, new Vector3(-0.5f, 0f, 0f), new Vector3(2.4f, 1.2f, 1.25f));
            GameObject bridgerRack = CreateBlock(environment, "Circuit Bridger Rack", new Vector3(5.15f, 1f, -2.5f), new Vector3(0.35f, 0.9f, 0.8f), circuitMaterial);
            bridgerRack.AddComponent<ToolRackInteractable>().Configure("circuit-bridger-rack", ToolKind.CircuitBridger);
            ConfigureBoxCollider(bridgerRack, new Vector3(-0.5f, 0f, 0f), new Vector3(2.4f, 1.2f, 1.25f));
            GameObject sealantRack = CreateBlock(environment, "Sealant Gun Rack", new Vector3(5.15f, 1f, -1.35f), new Vector3(0.35f, 0.9f, 0.8f), machineryMaterial);
            sealantRack.AddComponent<ToolRackInteractable>().Configure("sealant-gun-rack", ToolKind.SealantGun);
            ConfigureBoxCollider(sealantRack, new Vector3(-0.5f, 0f, 0f), new Vector3(2.4f, 1.2f, 1.25f));

            GameObject circuitNode = CreateBlock(
                environment,
                "Cooling Control Circuit Interlock",
                new Vector3(-5.85f, 1.25f, -1.6f),
                new Vector3(0.34f, 1.5f, 1.45f),
                circuitMaterial);
            circuitNode.AddComponent<CircuitBridgeTarget>().Configure(incident);
            ConfigureBoxCollider(circuitNode, new Vector3(0.19f, 0f, 0f), new Vector3(1.8f, 1f, 1f));

            var recoveryPointObject = new GameObject("Replacement Pipe Recovery Point");
            recoveryPointObject.transform.SetParent(environment);
            recoveryPointObject.transform.position = new Vector3(-4.8f, 0.75f, -4.5f);
            GameObject fastener = CreateBlock(environment, "Mechanical Fastener Demo", new Vector3(-3.6f, 1.1f, 6.7f), new Vector3(0.8f, 0.8f, 0.25f), machineryMaterial);
            fastener.AddComponent<MechanicalFastenerTarget>().Configure(incident);
            ConfigureBoxCollider(fastener, new Vector3(0f, 0f, -0.36f), new Vector3(1.1f, 1.1f, 2.6f));
            var pipeAnchor = new GameObject("Replacement Pipe Install Anchor");
            pipeAnchor.transform.SetParent(fastener.transform, false);
            pipeAnchor.transform.localPosition = new Vector3(0f, 0f, -1.2f);
            // 安装座同时保留恢复点引用，重置事故时可以把已安装的任务物送回架上。
            fastener.AddComponent<PipeInstallSocket>().Configure(incident, pipeAnchor.transform, recoveryPointObject.transform);
            GameObject leak = CreateBlock(environment, "Sealant Leak Demo", new Vector3(5.85f, 1.2f, 3f), new Vector3(0.65f, 0.5f, 1.5f), machineryMaterial);
            ConfigureBoxCollider(leak, Vector3.zero, new Vector3(1.7f, 2f, 1f));
            leak.AddComponent<SealantTarget>().Configure(incident);

            GameObject carryable = CreateBlock(environment, "Replacement Pipe", recoveryPointObject.transform.position, new Vector3(0.6f, 0.6f, 1.5f), warningMaterial);
            var itemBody = carryable.AddComponent<Rigidbody>();
            itemBody.mass = 3f;
            var carryableItem = carryable.AddComponent<CarryableInteractable>();
            carryableItem.ConfigureIdentity("replacement-pipe", "替换管件");
            ConfigureBoxCollider(carryable, Vector3.zero, new Vector3(1f, 1f, 1.2f));
            carryable.AddComponent<TaskItemRecovery>().Configure(recoveryPointObject.transform, -3f);

            var layout = new GameObject("Controlled Incident Layouts").AddComponent<CoolingIncidentLayoutController>();
            layout.Configure(
                incident,
                leak.transform,
                fastener.transform,
                recoveryPointObject.transform,
                carryableItem,
                new[]
                {
                    new Vector3(5.85f, 1.2f, 3f),
                    new Vector3(-5.85f, 1.2f, 0.8f),
                    new Vector3(5.85f, 1.2f, -0.4f)
                },
                new[]
                {
                    new Vector3(-3.6f, 1.1f, 6.7f),
                    new Vector3(-3.6f, 1.1f, 4.6f),
                    new Vector3(3.2f, 1.1f, 3.4f)
                },
                new[]
                {
                    new Vector3(-4.8f, 0.75f, -4.5f),
                    new Vector3(4.2f, 0.75f, -5.2f),
                    new Vector3(-4.2f, 0.75f, -5.2f)
                });

            CreateDecorationBlock(environment, "Walkway A", new Vector3(-3f, 0.15f, 0f), new Vector3(0.18f, 0.3f, 16f), warningMaterial);
            CreateDecorationBlock(environment, "Walkway B", new Vector3(3f, 0.15f, 0f), new Vector3(0.18f, 0.3f, 16f), warningMaterial);
            return layout;
        }

        private static FirstPersonController CreatePlayer(
            Material warningMaterial,
            Material machineryMaterial,
            Material circuitMaterial,
            CoolingIncidentController incident)
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
                machineryMaterial);
            GameObject bridgerVisual = CreateVisualCube(
                toolAnchorObject.transform,
                "Circuit Bridger Visual",
                new Vector3(0f, -0.02f, 0.12f),
                new Vector3(0.2f, 0.16f, 0.52f),
                circuitMaterial);

            var toolbelt = player.AddComponent<PlayerToolbelt>();
            toolbelt.ConfigureVisuals(wrenchVisual, sealantVisual, bridgerVisual);
            var playerController = player.AddComponent<FirstPersonController>();
            var interactor = player.AddComponent<ContextInteractor>();
            var toolController = player.AddComponent<ToolController>();
            player.AddComponent<ContextPromptOverlay>().Configure(incident);
            player.AddComponent<CoolingIncidentMetricsTracker>().Configure(incident, interactor, toolController);
            player.AddComponent<ToolbeltStatusOverlay>();
            return playerController;
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
            block.transform.SetParent(parent);
            block.transform.SetPositionAndRotation(position, Quaternion.identity);
            block.transform.localScale = scale;
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            return block;
        }

        private static GameObject CreateDecorationBlock(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject block = CreateBlock(parent, name, position, scale, material);
            Object.DestroyImmediate(block.GetComponent<Collider>());
            return block;
        }

        private static void ConfigureBoxCollider(GameObject target, Vector3 center, Vector3 size)
        {
            BoxCollider collider = target.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = target.AddComponent<BoxCollider>();
            }

            collider.center = center;
            collider.size = size;
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

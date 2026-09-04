using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FunGame.Combat;
using FunGame.Demo;
using FunGame.Diagnostics;
using FunGame.Incident;
using FunGame.Interaction;
using FunGame.Tools;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Editor
{
    /// <summary>
    /// 生成三章单人演示场景：扩展冷却事故、继电器事件和风暴核心五波校准防卫。
    /// </summary>
    public static class SinglePlayerDemoBootstrap
    {
        public const string ScenePath = "Assets/Game/Scenes/SinglePlayer_ThreeChapterDemo.unity";
        private const string MaterialFolder = "Assets/Game/Content/Graybox";
        private static readonly Vector3 RelayCompartmentOffset = new Vector3(0f, 0f, 20f);
        private static readonly Vector3 StormChamberOffset = new Vector3(0f, 0f, 40f);

        [MenuItem("FunGame/Demo/生成三章单人演示场景")]
        public static void ConfigureCurrent()
        {
            M1GrayboxBootstrap.ConfigureCurrent();
            CombatRepairIntegrationBootstrap.ConfigureCurrent();

            Scene scene = EditorSceneManager.OpenScene(CombatRepairIntegrationBootstrap.ScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, ScenePath, false))
            {
                throw new IOException($"无法复制三章单人演示场景：{ScenePath}");
            }

            CoolingIncidentController incident = UnityEngine.Object.FindFirstObjectByType<CoolingIncidentController>();
            CoolingCombatIntegrationController chapterOneCombat = UnityEngine.Object.FindFirstObjectByType<CoolingCombatIntegrationController>();
            if (incident == null || chapterOneCombat == null)
            {
                throw new InvalidDataException("三章演示源场景缺少冷却事故或维修防卫组件。");
            }

            Material relayMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Demo_Relay.mat", new Color(0.48f, 0.16f, 0.78f));
            Material stormMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Demo_Storm.mat", new Color(0.12f, 0.62f, 0.9f));
            Material warningMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Demo_Warning.mat", new Color(0.95f, 0.42f, 0.08f));
            Material darkMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Demo_Dark.mat", new Color(0.08f, 0.12f, 0.18f));

            var root = new GameObject("Single Player Three Chapter Demo").transform;
            RemoveLegacyVisualClutter();
            CreateExtendedShipCompartments(
                root,
                darkMaterial,
                warningMaterial,
                relayMaterial,
                stormMaterial,
                out GameObject relayAccessDoor,
                out GameObject stormAccessDoor);
            var relayCompartment = new GameObject("Chapter 2 - Power Relay Compartment").transform;
            relayCompartment.SetParent(root);
            var stormChamber = new GameObject("Chapter 3 - Storm Core Chamber").transform;
            stormChamber.SetParent(root);
            CreateCompartmentToolRack(
                relayCompartment, "Relay Bridger Station", new Vector3(5.65f, 1f, -7f),
                ToolKind.CircuitBridger, relayMaterial, warningMaterial);
            CreateCompartmentToolRack(
                relayCompartment, "Relay Wrench Station", new Vector3(5.65f, 1f, -5.6f),
                ToolKind.ImpactWrench, warningMaterial, darkMaterial);
            CreateCompartmentToolRack(
                stormChamber, "Storm Wrench Station", new Vector3(5.65f, 1f, -6.4f),
                ToolKind.ImpactWrench, warningMaterial, stormMaterial);
            DemoChapterPresentation presentation = CreateChapterArt(
                root, relayCompartment, stormChamber, relayMaterial, stormMaterial, warningMaterial, darkMaterial);

            DemoRelayTarget[] relays = CreateRelayTargets(relayCompartment, relayMaterial, warningMaterial);
            CombatEncounterController relayDefense = CreateEncounter(
                relayCompartment,
                "Chapter 2 Relay Surge",
                new Vector3(-4.4f, 1f, 5.8f),
                100,
                new[]
                {
                    EnemySpec.Strider("relay-strider", new Vector3(-2.8f, 1f, -6.5f)),
                    EnemySpec.Skitter("relay-skitter-left", new Vector3(3.8f, 1f, -5.6f)),
                    EnemySpec.Skitter("relay-skitter-right", new Vector3(-4.2f, 1f, -3.4f)),
                    EnemySpec.Pulser("relay-pulser", new Vector3(2.4f, 1f, 0.5f)),
                    EnemySpec.Bulwark("relay-bulwark", new Vector3(0f, 1f, 8f))
                },
                darkMaterial,
                warningMaterial);

            DefendableSystemTarget stormCoreTarget = CreateDefenseTarget(
                stormChamber,
                "Shared Storm Calibration Core",
                new Vector3(4.2f, 1f, 6.4f),
                130,
                stormMaterial);
            CombatEncounterController[] stormWaves =
            {
                CreateEncounter(
                    stormChamber,
                    "Chapter 3 Wave 1 - Skitter Rush",
                    new Vector3(4.2f, 1f, 6.4f),
                    90,
                    new[]
                    {
                        EnemySpec.Skitter("wave1-skitter-a", new Vector3(-5f, 1f, 5.5f)),
                        EnemySpec.Skitter("wave1-skitter-b", new Vector3(5f, 1f, 5.5f)),
                        EnemySpec.Strider("wave1-strider", new Vector3(0f, 1f, 7.8f))
                    },
                    darkMaterial,
                    warningMaterial,
                    stormCoreTarget),
                CreateEncounter(
                    stormChamber,
                    "Chapter 3 Wave 2 - Pulse Crossfire",
                    new Vector3(4.2f, 1f, 6.4f),
                    110,
                    new[]
                    {
                        EnemySpec.Pulser("wave2-pulser-a", new Vector3(-5.2f, 1f, 4.8f)),
                        EnemySpec.Pulser("wave2-pulser-b", new Vector3(5.2f, 1f, 4.8f)),
                        EnemySpec.Strider("wave2-strider", new Vector3(0f, 1f, 7.8f)),
                        EnemySpec.Skitter("wave2-skitter", new Vector3(4.6f, 1f, 0.8f))
                    },
                    darkMaterial,
                    warningMaterial,
                    stormCoreTarget),
                CreateEncounter(
                    stormChamber,
                    "Chapter 3 Wave 3 - Mixed Breach",
                    new Vector3(4.2f, 1f, 6.4f),
                    130,
                    new[]
                    {
                        EnemySpec.Strider("wave3-strider-left", new Vector3(-5f, 1f, -5f)),
                        EnemySpec.Strider("wave3-strider-center", new Vector3(0f, 1f, -3f)),
                        EnemySpec.Skitter("wave3-skitter", new Vector3(-4.8f, 1f, 1f)),
                        EnemySpec.Pulser("wave3-pulser", new Vector3(5f, 1f, -2f))
                    },
                    darkMaterial,
                    warningMaterial,
                    stormCoreTarget),
                CreateEncounter(
                    stormChamber,
                    "Chapter 3 Wave 4 - Armored Escort",
                    new Vector3(4.2f, 1f, 6.4f),
                    130,
                    new[]
                    {
                        EnemySpec.Bulwark("wave4-bulwark", new Vector3(-4.5f, 1f, 7.8f)),
                        EnemySpec.Strider("wave4-strider-left", new Vector3(-5f, 1f, 2.8f)),
                        EnemySpec.Strider("wave4-strider-right", new Vector3(0f, 1f, 0.8f)),
                        EnemySpec.Skitter("wave4-skitter-left", new Vector3(-4.8f, 1f, -3.5f)),
                        EnemySpec.Skitter("wave4-skitter-right", new Vector3(4.8f, 1f, -3.5f))
                    },
                    darkMaterial,
                    warningMaterial,
                    stormCoreTarget),
                CreateEncounter(
                    stormChamber,
                    "Chapter 3 Wave 5 - Final Convergence",
                    new Vector3(4.2f, 1f, 6.4f),
                    150,
                    new[]
                    {
                        EnemySpec.Bulwark("wave5-bulwark-left", new Vector3(-5f, 1f, 8f)),
                        EnemySpec.Bulwark("wave5-bulwark-center", new Vector3(0f, 1f, 8f)),
                        EnemySpec.Pulser("wave5-pulser-left", new Vector3(-5.2f, 1f, 3.5f)),
                        EnemySpec.Pulser("wave5-pulser-right", new Vector3(5.2f, 1f, 2.2f)),
                        EnemySpec.Skitter("wave5-skitter-left", new Vector3(-4.8f, 1f, -1f)),
                        EnemySpec.Skitter("wave5-skitter-right", new Vector3(4.8f, 1f, -1f))
                    },
                    darkMaterial,
                    warningMaterial,
                    stormCoreTarget)
            };

            DemoCalibrationConsole relayRecoveryConsole = CreateCampaignConsole(
                relayCompartment,
                "Power Compartment Recovery Console",
                new Vector3(0f, 1f, 8.55f),
                relayMaterial,
                darkMaterial,
                warningMaterial,
                relayMaterial);
            DemoCalibrationConsole stormCalibrationConsole = CreateCampaignConsole(
                stormChamber,
                "Storm Core Calibration Console",
                new Vector3(0f, 1f, 8.55f),
                stormMaterial,
                darkMaterial,
                warningMaterial,
                stormMaterial);

            CreateZoneLight(relayCompartment, "Power Compartment Work Light", new Vector3(0f, 4.2f, 0.5f), relayMaterial);
            CreateZoneLight(stormChamber, "Storm Chamber Work Light", new Vector3(0f, 4.2f, 1.8f), stormMaterial);
            relayCompartment.position = RelayCompartmentOffset;
            stormChamber.position = StormChamberOffset;

            var campaign = root.gameObject.AddComponent<SinglePlayerDemoController>();
            campaign.Configure(
                incident,
                relayDefense,
                relays,
                stormWaves,
                relayRecoveryConsole,
                stormCalibrationConsole);
            root.gameObject.AddComponent<SinglePlayerDemoOverlay>().Configure(campaign);
            var guidance = root.gameObject.AddComponent<DemoObjectiveGuidancePresenter>();
            guidance.Configure(
                campaign,
                incident,
                UnityEngine.Object.FindFirstObjectByType<ContextInteractor>(),
                chapterOneCombat);
            root.gameObject.AddComponent<DemoScreenshotCheckpoint>();
            presentation.Configure(
                campaign,
                stormChamber.GetComponentsInChildren<Transform>(true)
                    .Where(item => item.name.StartsWith("Storm Ceiling Node", StringComparison.Ordinal)).ToArray(),
                stormChamber.GetComponentsInChildren<Renderer>(true)
                    .Where(item => item.name.StartsWith("Storm Ceiling Node", StringComparison.Ordinal)).ToArray(),
                root.GetComponentsInChildren<Light>(true),
                relayCompartment.gameObject,
                stormChamber.gameObject,
                relayAccessDoor,
                stormAccessDoor);
            CreateEasterEgg325(root, campaign, warningMaterial, darkMaterial);

            ContextPromptOverlay prompt = UnityEngine.Object.FindFirstObjectByType<ContextPromptOverlay>();
            prompt?.ConfigureDemoCampaign(campaign);
            CoolingCombatStatusOverlay combatOverlay = UnityEngine.Object.FindFirstObjectByType<CoolingCombatStatusOverlay>();
            combatOverlay?.ConfigureDemoCampaign(campaign);
            ConfigureCheckpoint();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"无法保存三章单人演示场景：{ScenePath}");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Demo] 三章单人演示场景生成成功。");
        }

        [MenuItem("FunGame/Demo/构建三章单人 Windows 开发版本")]
        public static void BuildWindowsDevelopment()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                ConfigureCurrent();
            }
            else
            {
                UpgradeChapterConsoleLayout();
            }

            Directory.CreateDirectory("Builds/SinglePlayerDemo-Windows");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/SinglePlayerDemo-Windows/FunGame-SinglePlayerDemo.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"三章单人演示构建失败：{report.summary.result}");
            }

            Debug.Log($"[Demo] Windows 开发构建成功：{report.summary.totalSize} bytes。");
        }

        [MenuItem("FunGame/Demo/升级章节终端分舱布局")]
        public static void UpgradeChapterConsoleLayout()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = scene.GetRootGameObjects()
                .FirstOrDefault(item => item.name == "Single Player Three Chapter Demo");
            if (root == null)
            {
                throw new InvalidDataException("三章演示场景缺少流程根对象。");
            }

            Transform relayCompartment = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "Chapter 2 - Power Relay Compartment");
            Transform stormChamber = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == "Chapter 3 - Storm Core Chamber");
            if (relayCompartment == null || stormChamber == null)
            {
                throw new InvalidDataException("三章演示场景缺少配电舱或风暴核心舱。");
            }

            DemoCalibrationConsole[] existingConsoles = root.GetComponentsInChildren<DemoCalibrationConsole>(true);
            bool alreadySeparated = existingConsoles.Count(item =>
                                        item.Role == DemoCalibrationConsoleRole.RelayRecovery &&
                                        item.transform.IsChildOf(relayCompartment)) == 1 &&
                                    existingConsoles.Count(item =>
                                        item.Role == DemoCalibrationConsoleRole.StormCalibration &&
                                        item.transform.IsChildOf(stormChamber)) == 1;
            if (alreadySeparated)
            {
                return;
            }

            Material relayMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Demo_Relay.mat");
            Material stormMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Demo_Storm.mat");
            Material warningMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Demo_Warning.mat");
            Material darkMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Demo_Dark.mat");
            if (relayMaterial == null || stormMaterial == null || warningMaterial == null || darkMaterial == null)
            {
                throw new InvalidDataException("章节终端升级缺少演示材质。");
            }

            DemoCalibrationConsole relayRecoveryConsole = existingConsoles.FirstOrDefault(item =>
                item.transform.IsChildOf(relayCompartment));
            if (relayRecoveryConsole == null)
            {
                throw new InvalidDataException("配电舱缺少可迁移的旧章节控制台。");
            }

            relayRecoveryConsole.name = "Power Compartment Recovery Console";
            Renderer relayBody = relayRecoveryConsole.GetComponent<Renderer>();
            if (relayBody != null)
            {
                relayBody.sharedMaterial = relayMaterial;
            }

            DemoCalibrationConsole stormCalibrationConsole = existingConsoles.FirstOrDefault(item =>
                item.transform.IsChildOf(stormChamber));
            if (stormCalibrationConsole == null)
            {
                stormCalibrationConsole = CreateCampaignConsole(
                    stormChamber,
                    "Storm Core Calibration Console",
                    new Vector3(0f, 1f, 8.55f),
                    stormMaterial,
                    darkMaterial,
                    warningMaterial,
                    stormMaterial);
            }

            CoolingIncidentController incident = UnityEngine.Object.FindFirstObjectByType<CoolingIncidentController>();
            SinglePlayerDemoController campaign = root.GetComponent<SinglePlayerDemoController>();
            CombatEncounterController relayDefense = relayCompartment.GetComponentsInChildren<CombatEncounterController>(true)
                .FirstOrDefault(item => item.name == "Chapter 2 Relay Surge");
            DemoRelayTarget[] relays = relayCompartment.GetComponentsInChildren<DemoRelayTarget>(true)
                .OrderBy(item => item.name)
                .ToArray();
            CombatEncounterController[] stormWaves = stormChamber.GetComponentsInChildren<CombatEncounterController>(true)
                .Where(item => item.name.StartsWith("Chapter 3 Wave", StringComparison.Ordinal))
                .OrderBy(item => item.name)
                .ToArray();
            if (incident == null || campaign == null || relayDefense == null || relays.Length == 0 || stormWaves.Length == 0)
            {
                throw new InvalidDataException("章节终端升级无法解析完整流程引用。");
            }

            campaign.Configure(
                incident,
                relayDefense,
                relays,
                stormWaves,
                relayRecoveryConsole,
                stormCalibrationConsole);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"无法保存章节终端升级：{ScenePath}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Demo] 章节终端已按配电舱与风暴核心舱完成分舱升级。");
        }

        private static void RemoveLegacyVisualClutter()
        {
            GameObject routeMarkers = GameObject.Find("Silent Tutorial Route - Rack Enemy Device");
            if (routeMarkers != null)
            {
                UnityEngine.Object.DestroyImmediate(routeMarkers);
            }

            GameObject scaleReference = GameObject.Find("Remote Maintenance Suit Scale Reference");
            if (scaleReference != null)
            {
                UnityEngine.Object.DestroyImmediate(scaleReference);
            }

            GameObject rearWall = GameObject.Find("Rear Wall");
            if (rearWall != null)
            {
                rearWall.SetActive(false);
            }
        }

        private static void CreateExtendedShipCompartments(
            Transform parent,
            Material structure,
            Material warning,
            Material relay,
            Material storm,
            out GameObject relayAccessDoor,
            out GameObject stormAccessDoor)
        {
            CreateBlock(parent, "Extended Ship Floor", new Vector3(0f, -0.25f, 30f),
                new Vector3(14f, 0.5f, 40f), structure, true);
            CreateBlock(parent, "Extended Ship Ceiling", new Vector3(0f, 5f, 30f),
                new Vector3(14f, 0.5f, 40f), structure, true);
            CreateBlock(parent, "Extended Ship Left Wall", new Vector3(-7f, 2.5f, 30f),
                new Vector3(0.5f, 5f, 40f), structure, true);
            CreateBlock(parent, "Extended Ship Right Wall", new Vector3(7f, 2.5f, 30f),
                new Vector3(0.5f, 5f, 40f), structure, true);
            CreateBlock(parent, "Extended Ship Rear Wall", new Vector3(0f, 2.5f, 50f),
                new Vector3(14f, 5f, 0.5f), structure, true);

            relayAccessDoor = CreateBulkheadGate(
                parent, "Sealed Power Compartment Door", 9.72f, relay, warning, "POWER RELAY 02");
            stormAccessDoor = CreateBulkheadGate(
                parent, "Sealed Storm Chamber Door", 29.72f, storm, warning, "STORM CORE 03");
            CreateOpenBulkheadFrame(parent, "Power Compartment Frame", 10f, warning);
            CreateOpenBulkheadFrame(parent, "Storm Chamber Frame", 30f, warning);
        }

        private static GameObject CreateBulkheadGate(
            Transform parent,
            string name,
            float z,
            Material doorMaterial,
            Material accentMaterial,
            string label)
        {
            var gate = new GameObject(name);
            gate.transform.SetParent(parent);
            CreateBlock(gate.transform, "Left Door Panel", new Vector3(-3.05f, 2.15f, z),
                new Vector3(5.9f, 4.3f, 0.3f), doorMaterial, true);
            CreateBlock(gate.transform, "Right Door Panel", new Vector3(3.05f, 2.15f, z),
                new Vector3(5.9f, 4.3f, 0.3f), doorMaterial, true);
            CreateDecoration(gate.transform, "Door Warning Bar", PrimitiveType.Cube,
                new Vector3(0f, 2.15f, z - 0.18f), new Vector3(1.1f, 0.16f, 0.08f), accentMaterial);
            CreateWorldLabel(gate.transform, label + " Door Label", label,
                new Vector3(0f, 3.55f, z - 0.2f), new Color(1f, 0.62f, 0.18f));
            return gate;
        }

        private static void CreateOpenBulkheadFrame(Transform parent, string name, float z, Material material)
        {
            var frame = new GameObject(name).transform;
            frame.SetParent(parent);
            CreateBlock(frame, "Left Frame", new Vector3(-6.25f, 2.5f, z),
                new Vector3(1f, 5f, 0.55f), material, true);
            CreateBlock(frame, "Right Frame", new Vector3(6.25f, 2.5f, z),
                new Vector3(1f, 5f, 0.55f), material, true);
            CreateBlock(frame, "Top Frame", new Vector3(0f, 4.55f, z),
                new Vector3(11.6f, 0.45f, 0.55f), material, true);
        }

        private static void CreateWorldLabel(
            Transform parent,
            string name,
            string content,
            Vector3 position,
            Color color)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent);
            labelObject.transform.SetPositionAndRotation(position, Quaternion.identity);
            var text = labelObject.AddComponent<TextMesh>();
            text.text = content;
            text.fontSize = 72;
            text.characterSize = 0.075f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
        }

        private static void CreateZoneLight(Transform parent, string name, Vector3 position, Material colorSource)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 13f;
            light.intensity = 5f;
            light.color = colorSource.color;
        }

        private static void CreateCompartmentToolRack(
            Transform parent,
            string name,
            Vector3 position,
            ToolKind tool,
            Material bodyMaterial,
            Material accentMaterial)
        {
            GameObject rack = CreateBlock(
                parent, name, position, new Vector3(0.55f, 1.05f, 0.9f), bodyMaterial, true);
            rack.AddComponent<ToolRackInteractable>().Configure(name.ToLowerInvariant().Replace(' ', '-'), tool);
            BoxCollider rackCollider = rack.GetComponent<BoxCollider>();
            rackCollider.center = new Vector3(-0.25f, 0f, 0f);
            rackCollider.size = new Vector3(1.7f, 1.2f, 1.25f);
            if (tool == ToolKind.CircuitBridger)
            {
                CreateDecoration(parent, name + " Scanner", PrimitiveType.Cube,
                    position + new Vector3(-0.34f, 0.12f, 0f), new Vector3(0.16f, 0.3f, 0.38f), accentMaterial);
                CreateDecoration(parent, name + " Probe Upper", PrimitiveType.Cylinder,
                    position + new Vector3(-0.42f, 0.13f, 0.26f), new Vector3(0.04f, 0.16f, 0.04f),
                    bodyMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateDecoration(parent, name + " Probe Lower", PrimitiveType.Cylinder,
                    position + new Vector3(-0.42f, 0.13f, -0.26f), new Vector3(0.04f, 0.16f, 0.04f),
                    bodyMaterial, Quaternion.Euler(90f, 0f, 0f));
            }
            else
            {
                CreateDecoration(parent, name + " Grip", PrimitiveType.Cylinder,
                    position + new Vector3(-0.34f, -0.12f, 0f), new Vector3(0.08f, 0.32f, 0.08f),
                    accentMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateDecoration(parent, name + " Motor", PrimitiveType.Cylinder,
                    position + new Vector3(-0.38f, 0.22f, 0f), new Vector3(0.17f, 0.13f, 0.17f),
                    bodyMaterial, Quaternion.Euler(0f, 0f, 90f));
            }
        }

        private static DemoRelayTarget[] CreateRelayTargets(Transform parent, Material relay, Material warning)
        {
            Vector3[] positions =
            {
                new Vector3(-5.85f, 1.15f, -5.2f),
                new Vector3(5.85f, 1.15f, -5.2f),
                new Vector3(5.85f, 1.15f, 0.8f),
                new Vector3(-5.85f, 1.15f, 6.5f),
                new Vector3(5.85f, 1.15f, 6.5f)
            };
            var relays = new DemoRelayTarget[positions.Length];
            for (int index = 0; index < positions.Length; index++)
            {
                GameObject relayObject = CreateBlock(
                    parent,
                    $"Storm Relay {index + 1}",
                    positions[index],
                    new Vector3(0.35f, 1.45f, 1.1f),
                    relay,
                    true);
                relays[index] = relayObject.AddComponent<DemoRelayTarget>();
                relays[index].Configure($"storm-relay-{index + 1}", $"风暴继电器 {index + 1}");
                BoxCollider relayCollider = relayObject.GetComponent<BoxCollider>();
                relayCollider.center = new Vector3(0f, 0.275f, -0.07f);
                relayCollider.size = new Vector3(1f, 1.6f, 1.15f);
                CreateLocalDecoration(relayObject.transform, "Relay Phase Coil", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.15f, 0f), new Vector3(0.62f, 0.45f, 0.62f), warning,
                    Quaternion.Euler(0f, 0f, 90f));
                CreateLocalDecoration(relayObject.transform, "Relay Crown", PrimitiveType.Sphere,
                    new Vector3(0f, 0.85f, 0f), new Vector3(0.4f, 0.22f, 0.4f), warning);
                CreateLocalDecoration(relayObject.transform, "Relay Contact Upper", PrimitiveType.Cube,
                    new Vector3(0f, 0.52f, -0.58f), new Vector3(0.44f, 0.12f, 0.12f), relay);
                CreateLocalDecoration(relayObject.transform, "Relay Contact Lower", PrimitiveType.Cube,
                    new Vector3(0f, -0.38f, -0.58f), new Vector3(0.44f, 0.12f, 0.12f), relay);
            }

            return relays;
        }

        private static DemoCalibrationConsole CreateCampaignConsole(
            Transform parent,
            string name,
            Vector3 position,
            Material bodyMaterial,
            Material screenMaterial,
            Material warningMaterial,
            Material stageMaterial)
        {
            GameObject consoleObject = CreateBlock(
                parent,
                name,
                position,
                new Vector3(2.2f, 2f, 0.7f),
                bodyMaterial,
                false);
            var campaignConsole = consoleObject.AddComponent<DemoCalibrationConsole>();
            BoxCollider consoleCollider = consoleObject.GetComponent<BoxCollider>();
            consoleCollider.center = new Vector3(0f, 0f, -0.18f);
            consoleCollider.size = new Vector3(1f, 1f, 1.4f);
            CreateLocalDecoration(consoleObject.transform, "Calibration Console Screen", PrimitiveType.Cube,
                new Vector3(0f, 0.18f, -0.58f), new Vector3(0.62f, 0.24f, 0.08f), screenMaterial);
            CreateLocalDecoration(consoleObject.transform, "Calibration Console Lever", PrimitiveType.Cylinder,
                new Vector3(0.32f, -0.15f, -0.62f), new Vector3(0.07f, 0.22f, 0.07f), warningMaterial,
                Quaternion.Euler(62f, 0f, 0f));
            for (int index = 0; index < 5; index++)
            {
                CreateLocalDecoration(consoleObject.transform, $"Calibration Stage Light {index + 1}", PrimitiveType.Sphere,
                    new Vector3(-0.42f + index * 0.21f, -0.2f, -0.62f), new Vector3(0.055f, 0.055f, 0.035f),
                    index == 2 ? warningMaterial : stageMaterial);
            }

            return campaignConsole;
        }

        private static CombatEncounterController CreateEncounter(
            Transform parent,
            string name,
            Vector3 targetPosition,
            int targetIntegrity,
            IReadOnlyList<EnemySpec> specs,
            Material enemyMaterial,
            Material warningMaterial,
            DefendableSystemTarget sharedTarget = null)
        {
            var encounterObject = new GameObject(name);
            encounterObject.transform.SetParent(parent);
            var encounter = encounterObject.AddComponent<CombatEncounterController>();
            DefendableSystemTarget target = sharedTarget != null
                ? sharedTarget
                : CreateDefenseTarget(
                    encounterObject.transform,
                    name + " Target",
                    targetPosition,
                    targetIntegrity,
                    warningMaterial);

            var enemies = new List<InterferenceEnemy>(specs.Count);
            foreach (EnemySpec spec in specs)
            {
                enemies.Add(CreateEnemy(encounterObject.transform, target, encounter, spec, enemyMaterial, warningMaterial));
            }

            encounter.Configure(target, enemies, false);
            return encounter;
        }

        private static DefendableSystemTarget CreateDefenseTarget(
            Transform parent,
            string name,
            Vector3 position,
            int integrity,
            Material material)
        {
            GameObject targetObject = CreateBlock(
                parent,
                name,
                position,
                new Vector3(1.7f, 2f, 1.7f),
                material,
                true);
            var target = targetObject.AddComponent<DefendableSystemTarget>();
            target.Configure(integrity);
            targetObject.AddComponent<AudioSource>();
            targetObject.AddComponent<DeviceDamageFeedback>().Configure(target);
            CreateLocalDecoration(targetObject.transform, "Defense Core Lens", PrimitiveType.Sphere,
                new Vector3(0f, 0.12f, -0.58f), new Vector3(0.52f, 0.52f, 0.2f), material);
            for (int index = 0; index < 4; index++)
            {
                float angle = index * Mathf.PI * 0.5f;
                CreateLocalDecoration(targetObject.transform, $"Defense Core Fin {index + 1}", PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(angle) * 0.68f, Mathf.Sin(angle) * 0.68f, 0f),
                    index % 2 == 0 ? new Vector3(0.18f, 0.52f, 0.38f) : new Vector3(0.52f, 0.18f, 0.38f),
                    material);
            }
            return target;
        }

        private static InterferenceEnemy CreateEnemy(
            Transform parent,
            DefendableSystemTarget target,
            CombatEncounterController encounter,
            EnemySpec spec,
            Material material,
            Material accentMaterial)
        {
            GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = spec.DisplayName;
            enemyObject.transform.SetParent(parent);
            enemyObject.transform.position = spec.Position;
            enemyObject.transform.localScale = spec.Scale;
            enemyObject.GetComponent<Renderer>().sharedMaterial = material;
            var enemy = enemyObject.AddComponent<InterferenceEnemy>();
            enemy.ConfigureIdentity(spec.Id, spec.DisplayName);
            enemy.Configure(
                target,
                encounter,
                configuredMaxHealth: spec.Health,
                configuredMoveSpeed: spec.Speed,
                configuredAttackRange: spec.AttackRange,
                configuredAttackIntervalSeconds: spec.AttackInterval,
                configuredInterferenceDamage: spec.Damage,
                configuredWrenchDamage: 1,
                configuredKnockbackDistance: spec.Knockback,
                configuredBehavior: spec.Behavior,
                configuredAttackWindupSeconds: spec.Windup);

            var line = enemyObject.AddComponent<LineRenderer>();
            line.startColor = spec.Behavior == InterferenceEnemyBehavior.RangedPulse
                ? new Color(0.15f, 0.75f, 1f)
                : new Color(1f, 0.7f, 0.08f);
            line.endColor = new Color(0.85f, 0.12f, 0.95f);
            enemyObject.AddComponent<InterferenceLinkFeedback>().Configure(enemy, target, accentMaterial);
            AddEnemySilhouette(enemyObject.transform, spec, accentMaterial);
            return enemy;
        }

        private static void AddEnemySilhouette(Transform enemy, EnemySpec spec, Material accent)
        {
            if (spec.Archetype == "skitter")
            {
                CreateLocalDecoration(enemy, "Left Fin", PrimitiveType.Cube,
                    new Vector3(-0.55f, 0f, 0f), new Vector3(0.55f, 0.08f, 0.25f), accent, keepCollider: true);
                CreateLocalDecoration(enemy, "Right Fin", PrimitiveType.Cube,
                    new Vector3(0.55f, 0f, 0f), new Vector3(0.55f, 0.08f, 0.25f), accent, keepCollider: true);
            }
            else if (spec.Archetype == "pulser")
            {
                CreateLocalDecoration(enemy, "Pulse Crown", PrimitiveType.Sphere,
                    new Vector3(0f, 0.72f, 0f), new Vector3(0.42f, 0.18f, 0.42f), accent, keepCollider: true);
            }
            else if (spec.Archetype == "bulwark")
            {
                CreateLocalDecoration(enemy, "Front Armor", PrimitiveType.Cube,
                    new Vector3(0f, 0f, -0.55f), new Vector3(0.85f, 0.65f, 0.18f), accent, keepCollider: true);
                CreateLocalDecoration(enemy, "Armor Crown", PrimitiveType.Cube,
                    new Vector3(0f, 0.8f, 0f), new Vector3(0.75f, 0.2f, 0.7f), accent, keepCollider: true);
            }
        }

        private static DemoChapterPresentation CreateChapterArt(
            Transform presentationParent,
            Transform relayParent,
            Transform stormParent,
            Material relay,
            Material storm,
            Material warning,
            Material dark)
        {
            var art = new GameObject("Demo Chapter Art").transform;
            art.SetParent(presentationParent);
            CreateDecoration(relayParent, "Relay Conduit Left", PrimitiveType.Cylinder,
                new Vector3(-5.45f, 3.25f, 0.5f), new Vector3(0.12f, 4.3f, 0.12f), relay, Quaternion.Euler(90f, 0f, 0f));
            CreateDecoration(relayParent, "Relay Conduit Right", PrimitiveType.Cylinder,
                new Vector3(5.45f, 3.25f, 0.5f), new Vector3(0.12f, 4.3f, 0.12f), relay, Quaternion.Euler(90f, 0f, 0f));
            for (int index = 0; index < 5; index++)
            {
                CreateDecoration(stormParent, $"Storm Ceiling Node {index + 1}", PrimitiveType.Sphere,
                    new Vector3(-4.8f + index * 2.4f, 4.45f, -5.8f + index * 2.6f),
                    new Vector3(0.22f, 0.16f, 0.22f), index % 2 == 0 ? storm : warning);
            }

            CreateDecoration(stormParent, "Storm Core Outer", PrimitiveType.Cylinder,
                new Vector3(4.2f, 1f, 6.4f), new Vector3(1.2f, 1.35f, 1.2f), dark, Quaternion.Euler(90f, 0f, 0f));
            CreateDecoration(stormParent, "Storm Core Inner", PrimitiveType.Sphere,
                new Vector3(4.2f, 1f, 6.4f), new Vector3(0.55f, 0.55f, 0.55f), storm);
            return art.gameObject.AddComponent<DemoChapterPresentation>();
        }

        private static void CreateEasterEgg325(
            Transform parent,
            SinglePlayerDemoController campaign,
            Material plateMaterial,
            Material textMaterial)
        {
            GameObject plate = CreateBlock(
                parent,
                "Hidden Maintenance Plate 325",
                new Vector3(-6.15f, 1.25f, -7.25f),
                new Vector3(0.16f, 0.9f, 1.5f),
                plateMaterial,
                true);
            plate.AddComponent<DemoEasterEgg325Interactable>().Configure(campaign);

            var textObject = new GameObject("Engraved Number 325");
            textObject.transform.SetParent(plate.transform, false);
            textObject.transform.localPosition = new Vector3(0.56f, 0f, 0f);
            textObject.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var text = textObject.AddComponent<TextMesh>();
            text.text = "325";
            text.fontSize = 64;
            text.characterSize = 0.12f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = textMaterial.color;
        }

        private static void ConfigureCheckpoint()
        {
            DevelopmentCheckpoint checkpoint = UnityEngine.Object.FindFirstObjectByType<DevelopmentCheckpoint>();
            if (checkpoint == null)
            {
                checkpoint = new GameObject("Single Player Demo Checkpoint").AddComponent<DevelopmentCheckpoint>();
            }

            checkpoint.name = "Single Player Demo Checkpoint";
            checkpoint.Configure("singleplayer-three-chapter-demo", "--singleplayer-demo-smoke");
        }

        private static GameObject CreateBlock(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool worldPosition)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, !worldPosition);
            if (worldPosition)
            {
                block.transform.position = position;
            }
            else
            {
                block.transform.localPosition = position;
            }

            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static GameObject CreateDecoration(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Material material,
            Quaternion rotation = default)
        {
            GameObject decoration = GameObject.CreatePrimitive(type);
            decoration.name = name;
            decoration.transform.SetParent(parent);
            decoration.transform.SetPositionAndRotation(position, rotation == default ? Quaternion.identity : rotation);
            decoration.transform.localScale = scale;
            decoration.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(decoration.GetComponent<Collider>());
            return decoration;
        }

        private static GameObject CreateLocalDecoration(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Quaternion localRotation = default,
            bool keepCollider = false)
        {
            GameObject decoration = GameObject.CreatePrimitive(type);
            decoration.name = name;
            decoration.transform.SetParent(parent, false);
            decoration.transform.localPosition = localPosition;
            decoration.transform.localRotation = localRotation == default ? Quaternion.identity : localRotation;
            decoration.transform.localScale = localScale;
            decoration.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                UnityEngine.Object.DestroyImmediate(decoration.GetComponent<Collider>());
            }
            return decoration;
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

        private readonly struct EnemySpec
        {
            private EnemySpec(
                string id,
                string displayName,
                string archetype,
                Vector3 position,
                Vector3 scale,
                InterferenceEnemyBehavior behavior,
                int health,
                float speed,
                float attackRange,
                float attackInterval,
                int damage,
                float knockback,
                float windup)
            {
                Id = id;
                DisplayName = displayName;
                Archetype = archetype;
                Position = position;
                Scale = scale;
                Behavior = behavior;
                Health = health;
                Speed = speed;
                AttackRange = attackRange;
                AttackInterval = attackInterval;
                Damage = damage;
                Knockback = knockback;
                Windup = windup;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Archetype { get; }
            public Vector3 Position { get; }
            public Vector3 Scale { get; }
            public InterferenceEnemyBehavior Behavior { get; }
            public int Health { get; }
            public float Speed { get; }
            public float AttackRange { get; }
            public float AttackInterval { get; }
            public int Damage { get; }
            public float Knockback { get; }
            public float Windup { get; }

            public static EnemySpec Strider(string id, Vector3 position) => new EnemySpec(
                id, "直行干扰体", "strider", position, new Vector3(0.75f, 0.75f, 0.75f),
                InterferenceEnemyBehavior.Direct, 3, 1.25f, 1.15f, 1.5f, 10, 1.1f, 0.55f);

            public static EnemySpec Skitter(string id, Vector3 position) => new EnemySpec(
                id, "侧袭附着体", "skitter", position, new Vector3(0.62f, 0.5f, 0.9f),
                InterferenceEnemyBehavior.FlankingAttach, 2, 1.65f, 1.1f, 1.3f, 8, 1.35f, 0.42f);

            public static EnemySpec Pulser(string id, Vector3 position) => new EnemySpec(
                id, "远距脉冲体", "pulser", position, new Vector3(0.7f, 0.82f, 0.7f),
                InterferenceEnemyBehavior.RangedPulse, 3, 0.95f, 3.1f, 1.8f, 9, 0.8f, 0.8f);

            public static EnemySpec Bulwark(string id, Vector3 position) => new EnemySpec(
                id, "重甲干扰体", "bulwark", position, new Vector3(1.05f, 1.05f, 1.05f),
                InterferenceEnemyBehavior.Direct, 6, 0.72f, 1.4f, 2.2f, 16, 0.45f, 1.05f);
        }
    }
}

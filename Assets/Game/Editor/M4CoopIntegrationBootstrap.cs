using System;
using System.IO;
using FunGame.Combat;
using FunGame.Demo;
using FunGame.Incident;
using FunGame.Interaction;
using FunGame.Networking;
using FunGame.Player;
using FunGame.Tools;
using FunGame.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Editor
{
    /// <summary>
    /// M4-1 只整合三舱空间、网络会话与联网玩家。
    /// 单人事故和战斗逻辑会冻结到 M4-2/M4-3，避免客户端产生未经同步的本地状态。
    /// </summary>
    public static class M4CoopIntegrationBootstrap
    {
        public const string ScenePath = "Assets/Game/Scenes/M4_CoopThreeChapterDemo.unity";
        private const string BuildFolder = "Builds/M4-Coop-Windows";
        private const string BuildPath = BuildFolder + "/FunGame-M4-Coop.exe";
        private const string CommunicationPrefabPath =
            "Assets/Game/Content/Networking/M4_NetworkCommunication.prefab";
        private const string IncidentPrefabPath = "Assets/Game/Content/Networking/M4_CoolingIncident.prefab";
        private const string PipePrefabPath = "Assets/Game/Content/Networking/M4_ReplacementPipe.prefab";
        private const string CampaignPrefabPath = "Assets/Game/Content/Networking/M4_Campaign.prefab";
        private const string EnemyPrefabPath = "Assets/Game/Content/Networking/M4_Enemy.prefab";

        [MenuItem("FunGame/M4/生成多人三舱整合场景")]
        public static void ConfigureCurrent()
        {
            // 从已经验收的三章场景生成，而不是每次重写其源场景。
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SinglePlayerDemoBootstrap.ScenePath) == null)
            {
                SinglePlayerDemoBootstrap.ConfigureCurrent();
            }
            Scene scene = EditorSceneManager.OpenScene(SinglePlayerDemoBootstrap.ScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, ScenePath, false))
            {
                throw new IOException($"无法复制 M4 多人三舱场景：{ScenePath}");
            }

            RemoveLocalPlayer(scene);
            FreezeUnsynchronizedGameplay(scene);
            ConfigureNetworkRepairStations();
            var pumpProxy = FindRequired("Modular Cooling Pump").GetComponent<ContextInteractionProxy>();
            pumpProxy.Configure(FindRequired("Cooling Pump Inspection Panel").GetComponent<NetworkIncidentStation>());
            pumpProxy.enabled = true;
            // 铭牌只记录本地发现，不控制任务；保留原来的彩蛋交互。
            foreach (DemoEasterEgg325Interactable plate in
                     UnityEngine.Object.FindObjectsByType<DemoEasterEgg325Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                plate.Configure(null);
                plate.enabled = true;
            }
            foreach (FunGame.Diagnostics.DevelopmentCheckpoint checkpoint in
                     UnityEngine.Object.FindObjectsByType<FunGame.Diagnostics.DevelopmentCheckpoint>(FindObjectsSortMode.None))
                checkpoint.Configure("m4-coop-three-chapter-demo", "--m4-coop-smoke");
            ConfigureMenuForNetworkSession();
            var menuCamera = new GameObject("M4 Menu Camera");
            menuCamera.transform.SetPositionAndRotation(new Vector3(-2f, 1.65f, -4f), Quaternion.Euler(0f, 15f, 0f));
            menuCamera.AddComponent<Camera>();
            menuCamera.AddComponent<NetworkMenuCamera>();
            CreateNetworkSession();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"无法保存 M4 多人三舱场景：{ScenePath}");
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(SinglePlayerDemoBootstrap.ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M4] 多人三章完整整合场景已生成。 ");
        }

        [MenuItem("FunGame/M4/构建 Windows 开发版本")]
        public static void BuildWindowsDevelopment()
        {
            // 两种模式都从已合并的生成器重建，避免场景快照仍保留旧工具模型。
            SinglePlayerDemoBootstrap.ConfigureCurrent();
            ConfigureCurrent();
            Directory.CreateDirectory(BuildFolder);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath, SinglePlayerDemoBootstrap.ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"M4 完整整合版构建失败：{report.summary.result}");
            }

            Debug.Log($"[M4] Windows 完整整合开发构建成功：{report.summary.totalSize} bytes。 ");
        }

        private static void RemoveLocalPlayer(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                FirstPersonController controller = root.GetComponentInChildren<FirstPersonController>(true);
                if (controller != null)
                {
                    UnityEngine.Object.DestroyImmediate(controller.gameObject);
                    return;
                }
            }

            throw new InvalidDataException("三章源场景缺少本地第一人称玩家，无法建立明确的联网所有权边界。 ");
        }

        private static void FreezeUnsynchronizedGameplay(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour is InterferenceEnemy dormantEnemy)
                    {
                        // 原单人敌人仅作为联网敌人的布置参考，运行时不能重复显示或碰撞。
                        dormantEnemy.SetEncounterActive(false);
                    }
                    if (behaviour is DemoChapterPresentation presentation)
                    {
                        presentation.ConfigureNetworkMode();
                        continue;
                    }
                    if (ShouldFreeze(behaviour))
                    {
                        behaviour.enabled = false;
                    }
                }
            }
        }

        private static bool ShouldFreeze(MonoBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return false;
            }

            string componentNamespace = behaviour.GetType().Namespace ?? string.Empty;
            return behaviour is IContextInteractable
                   || behaviour is IToolTarget
                   || componentNamespace == typeof(SinglePlayerDemoController).Namespace
                   || componentNamespace == typeof(CoolingIncidentController).Namespace
                   || componentNamespace == typeof(CombatEncounterController).Namespace;
        }

        private static void CreateNetworkSession()
        {
            // M4-2 为联网玩家补齐第三工具模型，因此明确刷新共享玩家预制体。
            GameObject playerPrefab = M3NetworkBootstrap.CreateOrUpdatePlayerPrefab();
            GameObject communicationPrefab = CreateOrUpdateCommunicationPrefab();
            GameObject incidentPrefab = CreateOrUpdateIncidentPrefab();
            GameObject pipePrefab = CreateOrUpdatePipePrefab();
            GameObject enemyPrefab = CreateOrUpdateEnemyPrefab();
            GameObject campaignPrefab = CreateOrUpdateCampaignPrefab(enemyPrefab);
            M3NetworkBootstrap.RegisterNetworkPrefab(communicationPrefab);
            M3NetworkBootstrap.RegisterNetworkPrefab(incidentPrefab);
            M3NetworkBootstrap.RegisterNetworkPrefab(pipePrefab);
            M3NetworkBootstrap.RegisterNetworkPrefab(enemyPrefab);
            M3NetworkBootstrap.RegisterNetworkPrefab(campaignPrefab);
            var sessionObject = new GameObject("M4 Network Session");
            var networkManager = sessionObject.AddComponent<NetworkManager>();
            var transport = sessionObject.AddComponent<UnityTransport>();
            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.EnableSceneManagement = false;
            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;

            sessionObject.AddComponent<NetworkSessionController>()
                .Configure(networkManager, transport, "维修队 · 协作会话", false);
            sessionObject.AddComponent<NetworkDiagnosticsOverlay>().Configure(networkManager, transport);
            sessionObject.AddComponent<NetworkCommunicationSpawner>()
                .Configure(networkManager, communicationPrefab);
            sessionObject.AddComponent<NetworkIncidentSpawner>().Configure(networkManager, incidentPrefab);
            sessionObject.AddComponent<NetworkSharedItemSpawner>()
                .Configure(networkManager, pipePrefab, FindRequired("Replacement Pipe").transform.position);
            sessionObject.AddComponent<NetworkCampaignSpawner>().Configure(networkManager, campaignPrefab);
        }

        private static GameObject CreateOrUpdateEnemyPrefab()
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "M4 Network Interference Enemy";
            enemy.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            enemy.AddComponent<NetworkObject>();
            var transformSync = enemy.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            transformSync.AuthorityMode = Unity.Netcode.Components.NetworkTransform.AuthorityModes.Server;
            transformSync.Interpolate = true;
            enemy.AddComponent<NetworkCombatEnemy>();
            CreateEnemyPart(enemy.transform, "Shield Armor", new Vector3(0f, 0.12f, 0f), new Vector3(1.35f, 0.65f, 1.35f));
            CreateEnemyPart(enemy.transform, "Flank Wing Left", new Vector3(-0.65f, 0f, 0f), new Vector3(0.85f, 0.18f, 0.4f));
            CreateEnemyPart(enemy.transform, "Flank Wing Right", new Vector3(0.65f, 0f, 0f), new Vector3(0.85f, 0.18f, 0.4f));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefabPath);
            UnityEngine.Object.DestroyImmediate(enemy);
            return prefab;
        }

        private static void CreateEnemyPart(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        private static GameObject CreateOrUpdateCampaignPrefab(GameObject enemyPrefab)
        {
            var campaign = new GameObject("M4 Network Campaign");
            campaign.AddComponent<NetworkObject>();
            campaign.AddComponent<NetworkCampaignController>().Configure(enemyPrefab);
            campaign.AddComponent<NetworkCampaignOverlay>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(campaign, CampaignPrefabPath);
            UnityEngine.Object.DestroyImmediate(campaign);
            return prefab;
        }

        private static GameObject CreateOrUpdateIncidentPrefab()
        {
            var incidentObject = new GameObject("M4 Network Cooling Incident");
            incidentObject.AddComponent<NetworkObject>();
            var incident = incidentObject.AddComponent<NetworkCoolingIncidentController>();
            incident.ConfigureExtendedIncident(true);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(incidentObject, IncidentPrefabPath);
            UnityEngine.Object.DestroyImmediate(incidentObject);
            return prefab;
        }

        private static GameObject CreateOrUpdatePipePrefab()
        {
            GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = "M4 Replacement Pipe";
            pipe.transform.localScale = new Vector3(0.32f, 0.8f, 0.32f);
            pipe.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pipe.AddComponent<NetworkObject>();
            var networkTransform = pipe.AddComponent<Unity.Netcode.Components.NetworkTransform>();
            networkTransform.AuthorityMode = Unity.Netcode.Components.NetworkTransform.AuthorityModes.Server;
            networkTransform.Interpolate = true;
            pipe.AddComponent<Rigidbody>();
            pipe.AddComponent<Unity.Netcode.Components.NetworkRigidbody>();
            pipe.AddComponent<NetworkCarryableItem>().ConfigureIdentity("m3-shared-task-part", "共享替换管件");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(pipe, PipePrefabPath);
            UnityEngine.Object.DestroyImmediate(pipe);
            return prefab;
        }

        private static void ConfigureNetworkRepairStations()
        {
            AddStation("Diagnostic Pressure Gauge", "m4-pressure", "压力表", NetworkIncidentAction.InspectPressure);
            AddStation("Cooling Pump Inspection Panel", "m4-pump-inspection", "冷却泵检查面板", NetworkIncidentAction.InspectPump);
            AddStation("Cooling Control Circuit Interlock", "m4-circuit", "冷却控制联锁", NetworkIncidentAction.BridgeCircuit);
            AddStation("Sealant Leak Demo", "m4-leak", "冷却管线泄漏", NetworkIncidentAction.SealLeak);
            AddStation("Mechanical Fastener Demo", "m4-fastener", "机械连接件", NetworkIncidentAction.OperateFastener);
            AddStation("Replacement Pipe Install Anchor", "m4-pipe-socket", "替换管安装接口", NetworkIncidentAction.InstallPipe);
            AddStation("Interactive Control Console", "m4-console", "冷却控制台", NetworkIncidentAction.OperatePump);

            AddToolRack("Impact Wrench Rack", "m4-wrench-rack", ToolKind.ImpactWrench);
            AddToolRack("Sealant Gun Rack", "m4-sealant-rack", ToolKind.SealantGun);
            AddToolRack("Circuit Bridger Rack", "m4-bridger-rack", ToolKind.CircuitBridger);
            AddToolRack("Relay Bridger Station", "m4-relay-bridger", ToolKind.CircuitBridger);
            AddToolRack("Relay Wrench Station", "m4-relay-wrench", ToolKind.ImpactWrench);
            AddToolRack("Storm Wrench Station", "m4-storm-wrench", ToolKind.ImpactWrench);

            for (int index = 0; index < 5; index++)
            {
                GameObject relay = FindRequired($"Storm Relay {index + 1}");
                if (relay.GetComponent<Collider>() == null) relay.AddComponent<BoxCollider>();
                relay.AddComponent<NetworkCampaignStation>().Configure(index, false);
            }
            GameObject calibration = FindRequired("Storm Core Calibration Console");
            if (calibration.GetComponent<Collider>() == null) calibration.AddComponent<BoxCollider>();
            calibration.AddComponent<NetworkCampaignStation>().Configure(0, true);

            // 原单人物品只保留建模来源；联网版本由服务器生成，避免出现两个可见管件。
            FindRequired("Replacement Pipe").SetActive(false);
        }

        private static void AddStation(string objectName, string id, string displayName, NetworkIncidentAction action)
        {
            GameObject target = FindRequired(objectName);
            if (target.GetComponent<Collider>() == null)
            {
                // 部分单人目标只用子模型表达位置；网络交互站需要自己的射线碰撞体。
                target.AddComponent<BoxCollider>();
            }
            target.AddComponent<NetworkIncidentStation>().Configure(id, displayName, action);
        }

        private static void AddToolRack(string objectName, string id, ToolKind tool)
        {
            GameObject rack = FindRequired(objectName);
            rack.AddComponent<NetworkToolRackInteractable>().Configure(id, tool);
        }

        private static GameObject FindRequired(string objectName)
        {
            foreach (Transform candidate in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }

            throw new InvalidDataException($"M4 场景缺少必要对象：{objectName}");
        }

        private static GameObject CreateOrUpdateCommunicationPrefab()
        {
            var communicationObject = new GameObject("M4 Network Communication");
            communicationObject.AddComponent<NetworkObject>();
            communicationObject.AddComponent<NetworkChatController>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(communicationObject, CommunicationPrefabPath);
            UnityEngine.Object.DestroyImmediate(communicationObject);
            return prefab;
        }

        private static void ConfigureMenuForNetworkSession()
        {
            GameMenuController menu = UnityEngine.Object.FindFirstObjectByType<GameMenuController>(FindObjectsInactive.Include);
            if (menu == null)
            {
                throw new InvalidDataException("三章源场景缺少主菜单，无法接入联网会话流程。 ");
            }

            menu.ConfigureForNetworkSession();
        }
    }
}

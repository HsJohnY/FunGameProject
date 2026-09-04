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
            ConfigureMenuForNetworkSession();
            CreateNetworkSession();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"无法保存 M4 多人三舱场景：{ScenePath}");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M4-2] 多人三舱场景已生成；联网冷却事故与共享管件已接入。 ");
        }

        [MenuItem("FunGame/M4/构建 Windows 开发版本")]
        public static void BuildWindowsDevelopment()
        {
            ConfigureCurrent();
            Directory.CreateDirectory(BuildFolder);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"M4-2 构建失败：{report.summary.result}");
            }

            Debug.Log($"[M4-2] Windows 开发构建成功：{report.summary.totalSize} bytes。 ");
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
            M3NetworkBootstrap.RegisterNetworkPrefab(communicationPrefab);
            M3NetworkBootstrap.RegisterNetworkPrefab(incidentPrefab);
            M3NetworkBootstrap.RegisterNetworkPrefab(pipePrefab);
            var sessionObject = new GameObject("M4 Network Session");
            var networkManager = sessionObject.AddComponent<NetworkManager>();
            var transport = sessionObject.AddComponent<UnityTransport>();
            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.EnableSceneManagement = false;
            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;

            sessionObject.AddComponent<NetworkSessionController>()
                .Configure(networkManager, transport, "M4-1 多人三舱验证", false);
            sessionObject.AddComponent<NetworkDiagnosticsOverlay>().Configure(networkManager, transport);
            sessionObject.AddComponent<NetworkCommunicationSpawner>()
                .Configure(networkManager, communicationPrefab);
            sessionObject.AddComponent<NetworkIncidentSpawner>().Configure(networkManager, incidentPrefab);
            sessionObject.AddComponent<NetworkSharedItemSpawner>()
                .Configure(networkManager, pipePrefab, FindRequired("Replacement Pipe").transform.position);
        }

        private static GameObject CreateOrUpdateIncidentPrefab()
        {
            var incidentObject = new GameObject("M4 Network Cooling Incident");
            incidentObject.AddComponent<NetworkObject>();
            var incident = incidentObject.AddComponent<NetworkCoolingIncidentController>();
            incident.ConfigureExtendedIncident(true);
            incidentObject.AddComponent<NetworkIncidentOverlay>().Configure(incident);
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

using System.IO;
using FunGame.Networking;
using FunGame.Player;
using FunGame.Interaction;
using FunGame.Tools;
using Unity.Netcode;
using Unity.Netcode.Components;
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
    /// 独立生成 M3 网络验证场景，避免网络实验污染已经验收的 M1 灰盒场景。
    /// </summary>
    public static class M3NetworkBootstrap
    {
        private const string ScenePath = "Assets/Game/Scenes/M3_NetworkSlice.unity";
        public const string PlayerPrefabPath = "Assets/Game/Content/Networking/M3_NetworkPlayer.prefab";
        private const string CarryablePrefabPath = "Assets/Game/Content/Networking/M3_SharedTaskPart.prefab";
        private const string IncidentPrefabPath = "Assets/Game/Content/Networking/M3_NetworkIncident.prefab";

        [MenuItem("FunGame/M3/生成网络验证场景")]
        public static void ConfigureCurrent()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "M3_NetworkSlice";

            CreateEnvironment();
            GameObject playerPrefab = CreateOrUpdatePlayerPrefab();
            GameObject carryablePrefab = CreateCarryablePrefab();
            GameObject incidentPrefab = CreateIncidentPrefab();
            RegisterNetworkPrefab(carryablePrefab);
            RegisterNetworkPrefab(incidentPrefab);

            var sessionObject = new GameObject("M3 Network Session");
            var networkManager = sessionObject.AddComponent<NetworkManager>();
            var transport = sessionObject.AddComponent<UnityTransport>();
            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.EnableSceneManagement = false;
            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;

            var sessionController = sessionObject.AddComponent<NetworkSessionController>();
            sessionController.Configure(networkManager, transport);
            sessionObject.AddComponent<NetworkDiagnosticsOverlay>().Configure(networkManager, transport);
            var itemSpawner = sessionObject.AddComponent<NetworkSharedItemSpawner>();
            itemSpawner.Configure(networkManager, carryablePrefab);
            var incidentSpawner = sessionObject.AddComponent<NetworkIncidentSpawner>();
            incidentSpawner.Configure(networkManager, incidentPrefab);

            CreateNetworkIncidentStations();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"无法保存 M3 网络验证场景：{ScenePath}");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M3-6] 完整网络切片验证场景生成成功。");
        }

        /// <summary>
        /// 生成 M3 网络切片的 Windows x64 开发构建，供双进程和最终冒烟验证使用。
        /// </summary>
        [MenuItem("FunGame/M3/构建 Windows 开发版本")]
        public static void BuildWindowsDevelopment()
        {
            ConfigureCurrent();
            Directory.CreateDirectory("Builds/M3-Network-Windows");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/M3-Network-Windows/FunGame-M3-Network.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"M3 网络切片构建失败：{report.summary.result}");
            }

            Debug.Log($"[M3-6] Windows 开发构建成功：{report.summary.totalSize} bytes。");
        }

        private static void CreateEnvironment()
        {
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Network Test Floor";
            floor.transform.localScale = new Vector3(3f, 1f, 3f);

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Central Orientation Marker";
            marker.transform.position = new Vector3(0f, 1.5f, 4f);
            marker.transform.localScale = new Vector3(2f, 3f, 2f);

            // 围墙既提供清晰的空间参照，也防止玩家在同步测试时轻易离开地面。
            CreateBlock("North Wall", new Vector3(0f, 1.5f, 15f), new Vector3(30f, 3f, 1f));
            CreateBlock("South Wall", new Vector3(0f, 1.5f, -15f), new Vector3(30f, 3f, 1f));
            CreateBlock("East Wall", new Vector3(15f, 1.5f, 0f), new Vector3(1f, 3f, 30f));
            CreateBlock("West Wall", new Vector3(-15f, 1.5f, 0f), new Vector3(1f, 3f, 30f));

            // 出生区标记用于快速判断双方是否生成在不同位置。
            CreateBlock("Host Spawn Marker", new Vector3(-2f, 0.05f, -4f), new Vector3(2f, 0.1f, 2f));
            CreateBlock("Client Spawn Marker", new Vector3(2f, 0.05f, -4f), new Vector3(2f, 0.1f, 2f));

            GameObject wrenchRack = CreateBlock(
                "Network Impact Wrench Rack",
                new Vector3(-6f, 1f, 1f),
                new Vector3(1f, 2f, 1f));
            wrenchRack.AddComponent<NetworkToolRackInteractable>()
                .Configure("network-impact-wrench-rack", ToolKind.ImpactWrench);

            GameObject sealantRack = CreateBlock(
                "Network Sealant Gun Rack",
                new Vector3(6f, 0.75f, 1f),
                new Vector3(2f, 1.5f, 1f));
            sealantRack.AddComponent<NetworkToolRackInteractable>()
                .Configure("network-sealant-gun-rack", ToolKind.SealantGun);
        }

        private static GameObject CreateBlock(string name, Vector3 position, Vector3 scale)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = position;
            block.transform.localScale = scale;
            return block;
        }

        /// <summary>
        /// 创建或更新共享的联网玩家预制体，供后续整合场景复用同一所有权实现。
        /// </summary>
        public static GameObject CreateOrUpdatePlayerPrefab()
        {
            EnsureFolder("Assets/Game/Content", "Networking");

            var player = new GameObject("M3 Network Player");
            player.AddComponent<NetworkObject>();

            var networkTransform = player.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            networkTransform.SyncRotAngleX = false;
            networkTransform.SyncRotAngleY = true;
            networkTransform.SyncRotAngleZ = false;
            networkTransform.Interpolate = true;

            var characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.4f;
            characterController.center = Vector3.zero;

            var cameraObject = new GameObject("First Person Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            Camera viewCamera = cameraObject.AddComponent<Camera>();
            viewCamera.enabled = false;
            AudioListener audioListener = cameraObject.AddComponent<AudioListener>();
            audioListener.enabled = false;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Remote Player Body";
            body.transform.SetParent(player.transform, false);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            Renderer bodyRenderer = body.GetComponent<Renderer>();

            var toolVisualAnchor = new GameObject("Main Tool Visual Anchor");
            toolVisualAnchor.transform.SetParent(cameraObject.transform, false);
            toolVisualAnchor.transform.localPosition = new Vector3(0.35f, -0.25f, 0.75f);

            GameObject wrenchVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wrenchVisual.name = "Impact Wrench Visual";
            wrenchVisual.transform.SetParent(toolVisualAnchor.transform, false);
            wrenchVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            wrenchVisual.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            Object.DestroyImmediate(wrenchVisual.GetComponent<Collider>());

            GameObject sealantVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sealantVisual.name = "Sealant Gun Visual";
            sealantVisual.transform.SetParent(toolVisualAnchor.transform, false);
            sealantVisual.transform.localScale = new Vector3(0.18f, 0.18f, 0.5f);
            Object.DestroyImmediate(sealantVisual.GetComponent<Collider>());

            GameObject bridgerVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridgerVisual.name = "Circuit Bridger Visual";
            bridgerVisual.transform.SetParent(toolVisualAnchor.transform, false);
            bridgerVisual.transform.localScale = new Vector3(0.18f, 0.14f, 0.58f);
            Object.DestroyImmediate(bridgerVisual.GetComponent<Collider>());

            FirstPersonController firstPersonController = player.AddComponent<FirstPersonController>();
            firstPersonController.enabled = false;
            var playerToolbelt = player.AddComponent<PlayerToolbelt>();
            playerToolbelt.ConfigureVisuals(wrenchVisual, sealantVisual, bridgerVisual);
            player.AddComponent<NetworkPlayerToolbelt>();
            var contextInteractor = player.AddComponent<ContextInteractor>();
            contextInteractor.enabled = false;
            var toolController = player.AddComponent<ToolController>();
            toolController.enabled = false;
            player.AddComponent<NetworkPlayerCarryController>();
            player.AddComponent<NetworkPlayerIncidentAgent>();
            player.AddComponent<NetworkPlayerCampaignAgent>();
            var promptOverlay = player.AddComponent<ContextPromptOverlay>();
            promptOverlay.enabled = false;
            NetworkPlayerController networkPlayer = player.AddComponent<NetworkPlayerController>();

            var serializedPlayer = new SerializedObject(networkPlayer);
            serializedPlayer.FindProperty("firstPersonController").objectReferenceValue = firstPersonController;
            serializedPlayer.FindProperty("viewCamera").objectReferenceValue = viewCamera;
            serializedPlayer.FindProperty("audioListener").objectReferenceValue = audioListener;
            SerializedProperty renderers = serializedPlayer.FindProperty("remoteBodyRenderers");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = bodyRenderer;
            SerializedProperty ownerBehaviours = serializedPlayer.FindProperty("ownerOnlyBehaviours");
            ownerBehaviours.arraySize = 3;
            ownerBehaviours.GetArrayElementAtIndex(0).objectReferenceValue = contextInteractor;
            ownerBehaviours.GetArrayElementAtIndex(1).objectReferenceValue = promptOverlay;
            ownerBehaviours.GetArrayElementAtIndex(2).objectReferenceValue = toolController;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Object.DestroyImmediate(player);
            return prefab;
        }

        private static GameObject CreateIncidentPrefab()
        {
            var incidentObject = new GameObject("M3 Network Cooling Incident");
            incidentObject.AddComponent<NetworkObject>();
            var incident = incidentObject.AddComponent<NetworkCoolingIncidentController>();
            incidentObject.AddComponent<NetworkIncidentOverlay>().Configure(incident);
            incidentObject.AddComponent<NetworkChatController>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(incidentObject, IncidentPrefabPath);
            Object.DestroyImmediate(incidentObject);
            return prefab;
        }

        private static void CreateNetworkIncidentStations()
        {
            CreateIncidentStation(
                "Network Leak Station",
                NetworkIncidentLayout.GetStationPosition(NetworkIncidentAction.SealLeak),
                new Vector3(2f, 2f, 1f),
                "network-leak",
                "联网泄漏点",
                NetworkIncidentAction.SealLeak);
            CreateIncidentStation(
                "Network Fastener Station",
                NetworkIncidentLayout.GetStationPosition(NetworkIncidentAction.OperateFastener),
                new Vector3(2f, 2f, 1f),
                "network-fastener",
                "联网管件连接",
                NetworkIncidentAction.OperateFastener);
            CreateIncidentStation(
                "Network Pump Console",
                NetworkIncidentLayout.GetStationPosition(NetworkIncidentAction.OperatePump),
                new Vector3(2f, 2f, 1f),
                "network-pump-console",
                "联网冷却控制台",
                NetworkIncidentAction.OperatePump);
            CreateIncidentStation(
                "Network Pipe Install Socket",
                NetworkIncidentLayout.GetStationPosition(NetworkIncidentAction.InstallPipe),
                new Vector3(2.5f, 2f, 1f),
                "network-pipe-install-socket",
                "联网管件安装接口",
                NetworkIncidentAction.InstallPipe);
        }

        private static void CreateIncidentStation(
            string objectName,
            Vector3 position,
            Vector3 scale,
            string id,
            string displayName,
            NetworkIncidentAction action)
        {
            GameObject station = CreateBlock(objectName, position, scale);
            station.AddComponent<NetworkIncidentStation>().Configure(id, displayName, action);
        }

        private static GameObject CreateCarryablePrefab()
        {
            EnsureFolder("Assets/Game/Content", "Networking");

            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = "M3 Shared Task Part";
            item.transform.localScale = new Vector3(0.6f, 0.6f, 1.5f);
            item.AddComponent<NetworkObject>();
            var networkTransform = item.AddComponent<NetworkTransform>();
            networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;
            networkTransform.Interpolate = true;
            item.AddComponent<Rigidbody>();
            item.AddComponent<NetworkRigidbody>();
            item.AddComponent<NetworkCarryableItem>()
                .ConfigureIdentity("m3-shared-task-part", "共享替换管件");

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(item, CarryablePrefabPath);
            Object.DestroyImmediate(item);
            return prefab;
        }

        public static void RegisterNetworkPrefab(GameObject prefab)
        {
            const string defaultListPath = "Assets/DefaultNetworkPrefabs.asset";
            NetworkPrefabsList prefabList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(defaultListPath);
            if (prefabList != null && !prefabList.Contains(prefab))
            {
                prefabList.Add(new NetworkPrefab { Prefab = prefab });
                EditorUtility.SetDirty(prefabList);
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}

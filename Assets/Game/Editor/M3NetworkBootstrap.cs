using System.IO;
using FunGame.Networking;
using FunGame.Player;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
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
        private const string PlayerPrefabPath = "Assets/Game/Content/Networking/M3_NetworkPlayer.prefab";

        [MenuItem("FunGame/M3/生成网络验证场景")]
        public static void ConfigureCurrent()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "M3_NetworkSlice";

            CreateEnvironment();
            GameObject playerPrefab = CreatePlayerPrefab();

            var sessionObject = new GameObject("M3 Network Session");
            var networkManager = sessionObject.AddComponent<NetworkManager>();
            var transport = sessionObject.AddComponent<UnityTransport>();
            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.EnableSceneManagement = false;
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;

            var sessionController = sessionObject.AddComponent<NetworkSessionController>();
            sessionController.Configure(networkManager, transport);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"无法保存 M3 网络验证场景：{ScenePath}");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M3-2] 玩家生成与移动同步验证场景生成成功。");
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

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "M3 Scene Marker";
            marker.transform.position = new Vector3(0f, 0.5f, 0f);
        }

        private static GameObject CreatePlayerPrefab()
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

            FirstPersonController firstPersonController = player.AddComponent<FirstPersonController>();
            firstPersonController.enabled = false;
            NetworkPlayerController networkPlayer = player.AddComponent<NetworkPlayerController>();

            var serializedPlayer = new SerializedObject(networkPlayer);
            serializedPlayer.FindProperty("firstPersonController").objectReferenceValue = firstPersonController;
            serializedPlayer.FindProperty("viewCamera").objectReferenceValue = viewCamera;
            serializedPlayer.FindProperty("audioListener").objectReferenceValue = audioListener;
            SerializedProperty renderers = serializedPlayer.FindProperty("remoteBodyRenderers");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = bodyRenderer;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Object.DestroyImmediate(player);
            return prefab;
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

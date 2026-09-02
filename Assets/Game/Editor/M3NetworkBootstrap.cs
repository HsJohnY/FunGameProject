using System.IO;
using FunGame.Networking;
using Unity.Netcode;
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

        [MenuItem("FunGame/M3/生成网络验证场景")]
        public static void ConfigureCurrent()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "M3_NetworkSlice";

            CreateEnvironment();

            var sessionObject = new GameObject("M3 Network Session");
            var networkManager = sessionObject.AddComponent<NetworkManager>();
            var transport = sessionObject.AddComponent<UnityTransport>();
            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.EnableSceneManagement = false;

            var sessionController = sessionObject.AddComponent<NetworkSessionController>();
            sessionController.Configure(networkManager, transport);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"无法保存 M3 网络验证场景：{ScenePath}");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M3-1] 网络验证场景生成成功。");
        }

        private static void CreateEnvironment()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 3f, -6f), Quaternion.Euler(15f, 0f, 0f));

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
    }
}

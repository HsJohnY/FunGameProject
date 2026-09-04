using System;
using System.IO;
using FunGame.Combat;
using FunGame.Demo;
using FunGame.Incident;
using FunGame.Interaction;
using FunGame.Networking;
using FunGame.Player;
using FunGame.Tools;
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
            CreateNetworkSession();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"无法保存 M4 多人三舱场景：{ScenePath}");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[M4-1] 多人三舱整合场景生成成功；玩法状态保持冻结。 ");
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
                throw new BuildFailedException($"M4-1 构建失败：{report.summary.result}");
            }

            Debug.Log($"[M4-1] Windows 开发构建成功：{report.summary.totalSize} bytes。 ");
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
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(M3NetworkBootstrap.PlayerPrefabPath);
            if (playerPrefab == null)
            {
                playerPrefab = M3NetworkBootstrap.CreateOrUpdatePlayerPrefab();
            }
            var sessionObject = new GameObject("M4 Network Session");
            var networkManager = sessionObject.AddComponent<NetworkManager>();
            var transport = sessionObject.AddComponent<UnityTransport>();
            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.EnableSceneManagement = false;
            networkManager.NetworkConfig.ConnectionApproval = true;
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;

            sessionObject.AddComponent<NetworkSessionController>()
                .Configure(networkManager, transport, "M4-1 多人三舱验证");
            sessionObject.AddComponent<NetworkDiagnosticsOverlay>().Configure(networkManager, transport);

            // 聊天是会话级能力，不应继续依附于 M3 事故对象。
            var communicationObject = new GameObject("M4 Network Communication");
            communicationObject.AddComponent<NetworkObject>();
            communicationObject.AddComponent<NetworkChatController>();
        }
    }
}

using System.Linq;
using FunGame.Combat;
using FunGame.Demo;
using FunGame.Networking;
using FunGame.Player;
using FunGame.UI;
using FunGame.Tools;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor.SceneManagement;
using UnityEditor;
using FunGame.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Tests.EditMode
{
    public sealed class M4CoopIntegrationSceneTests
    {
        private const string ScenePath = "Assets/Game/Scenes/SinglePlayer_ThreeChapterDemo.unity";

        [Test]
        public void M4场景包含三舱网络会话且不保留本地玩家()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                NetworkManager manager = roots.SelectMany(item => item.GetComponentsInChildren<NetworkManager>(true))
                    .SingleOrDefault();
                NetworkSessionController session = roots
                    .SelectMany(item => item.GetComponentsInChildren<NetworkSessionController>(true)).SingleOrDefault();
                NetworkCommunicationSpawner communicationSpawner = roots
                    .SelectMany(item => item.GetComponentsInChildren<NetworkCommunicationSpawner>(true)).SingleOrDefault();
                NetworkIncidentSpawner incidentSpawner = roots
                    .SelectMany(item => item.GetComponentsInChildren<NetworkIncidentSpawner>(true)).SingleOrDefault();
                NetworkSharedItemSpawner itemSpawner = roots
                    .SelectMany(item => item.GetComponentsInChildren<NetworkSharedItemSpawner>(true)).SingleOrDefault();
                NetworkCampaignSpawner campaignSpawner = roots
                    .SelectMany(item => item.GetComponentsInChildren<NetworkCampaignSpawner>(true)).SingleOrDefault();
                GameMenuController menu = roots
                    .SelectMany(item => item.GetComponentsInChildren<GameMenuController>(true)).SingleOrDefault();

                Assert.That(manager, Is.Not.Null);
                Assert.That(session, Is.Not.Null);
                Assert.That(communicationSpawner, Is.Not.Null);
                Assert.That(communicationSpawner.CommunicationPrefab, Is.Not.Null);
                Assert.That(communicationSpawner.CommunicationPrefab.GetComponent<NetworkChatController>(), Is.Not.Null);
                Assert.That(incidentSpawner, Is.Not.Null);
                Assert.That(itemSpawner, Is.Not.Null);
                Assert.That(campaignSpawner, Is.Not.Null);
                Assert.That(campaignSpawner.CampaignPrefab, Is.Not.Null);
                Assert.That(campaignSpawner.CampaignPrefab.GetComponent<NetworkCampaignController>(), Is.Not.Null);
                Assert.That(roots.SelectMany(item => item.GetComponentsInChildren<NetworkChatController>(true)), Is.Empty,
                    "关闭 NGO 场景管理时，聊天不能作为未注册的场景内 NetworkObject 存在。 ");
                Assert.That(menu, Is.Not.Null);
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<SharedMapModeController>(true)).Single(), Is.Not.Null);
                Assert.That(session.EscapeStopsSession, Is.False, "M4 的 Esc 应由暂停菜单处理，不能同时断开会话。 ");
                Assert.That(manager.NetworkConfig.PlayerPrefab, Is.Not.Null);
                Assert.That(manager.NetworkConfig.PlayerPrefab.GetComponent<NetworkPlayerController>(), Is.Not.Null);
                Transform[] playerParts = manager.NetworkConfig.PlayerPrefab.GetComponentsInChildren<Transform>(true);
                Assert.That(playerParts.Any(t => t.name == "Wrench Motor Drum"), Is.True);
                Assert.That(playerParts.Any(t => t.name == "Sealant Pressure Gauge"), Is.True);
                Assert.That(playerParts.Any(t => t.name == "Bridger Probe Tip Right"), Is.True);
                Assert.That(manager.NetworkConfig.PlayerPrefab.GetComponent<NetworkObjectiveGuidance>(), Is.Not.Null);
                Assert.That(manager.NetworkConfig.PlayerPrefab.GetComponent<DemoObjectiveGuidancePresenter>(), Is.Not.Null);
                Assert.That(manager.NetworkConfig.PlayerPrefab.GetComponent<ToolbeltStatusOverlay>(), Is.Not.Null);
                Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path.EndsWith("SinglePlayer_ThreeChapterDemo.unity")), Is.True);
                ContextInteractionProxy proxy = FindTransform(roots, "Modular Cooling Pump").GetComponent<ContextInteractionProxy>();
                Assert.That(proxy.enabled, Is.True);
                var proxyData = new SerializedObject(proxy);
                Assert.That(proxyData.FindProperty("targetBehaviour").objectReferenceValue, Is.Not.Null);
                Assert.That(roots.SelectMany(item => item.GetComponentsInChildren<FirstPersonController>(true)).Count(), Is.EqualTo(1),
                    "共享地图保留一个单人玩家，由模式控制器切换所有权。 ");
                Assert.That(FindTransform(roots, "Chapter 2 - Power Relay Compartment"), Is.Not.Null);
                Assert.That(FindTransform(roots, "Chapter 3 - Storm Core Chamber"), Is.Not.Null);
                NetworkIncidentStation[] stations = roots
                    .SelectMany(item => item.GetComponentsInChildren<NetworkIncidentStation>(true)).ToArray();
                Assert.That(stations.Length, Is.EqualTo(7));
                Assert.That(stations.Select(item => item.Action).Distinct().Count(), Is.EqualTo(7));
                NetworkToolRackInteractable[] racks = roots
                    .SelectMany(item => item.GetComponentsInChildren<NetworkToolRackInteractable>(true)).ToArray();
                Assert.That(racks.Length, Is.EqualTo(8));
                Assert.That(manager.NetworkConfig.PlayerPrefab.GetComponent<NetworkPlayerToolbelt>(), Is.Not.Null);
                Assert.That(manager.NetworkConfig.PlayerPrefab.GetComponent<PlayerToolbelt>(), Is.Not.Null);
                Assert.That(manager.NetworkConfig.PlayerPrefab.GetComponent<ToolController>(), Is.Not.Null);
                Assert.That(manager.NetworkConfig.PlayerPrefab.GetComponent<NetworkPlayerCampaignAgent>(), Is.Not.Null);
                NetworkCampaignStation[] campaignStations = roots
                    .SelectMany(item => item.GetComponentsInChildren<NetworkCampaignStation>(true)).ToArray();
                Assert.That(campaignStations.Count(item => !item.IsCalibrationConsole), Is.EqualTo(5));
                Assert.That(campaignStations.Count(item => item.IsCalibrationConsole), Is.EqualTo(2));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void M4场景在同步实现前冻结本地事故与战斗状态()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                MonoBehaviour[] behaviours = scene.GetRootGameObjects()
                    .SelectMany(item => item.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();

                Assert.That(behaviours.OfType<SharedMapModeController>().Single().MapRoot.activeSelf, Is.False);
                Assert.That(behaviours.OfType<CombatEncounterController>().All(item => item.enabled), Is.True);
                Assert.That(behaviours.OfType<InterferenceEnemy>().All(item => item.enabled), Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Transform FindTransform(GameObject[] roots, string objectName)
        {
            return roots.SelectMany(item => item.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == objectName);
        }
    }
}

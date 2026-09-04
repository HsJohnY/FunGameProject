using System.Collections;
using FunGame.Incident;
using FunGame.Networking;
using FunGame.UI;
using FunGame.Tools;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class M4CommunicationSpawnPlayModeTests
    {
        [UnityTest]
        public IEnumerator 启动M4主机后生成已注册的通信与维修对象()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("M4_CoopThreeChapterDemo", LoadSceneMode.Additive);
            while (load != null && !load.isDone)
            {
                yield return null;
            }

            Scene scene = SceneManager.GetSceneByName("M4_CoopThreeChapterDemo");
            Assert.That(scene.IsValid(), Is.True);
            GameMenuController menu = Object.FindFirstObjectByType<GameMenuController>();
            menu?.EnterGameplayForAutomation();
            NetworkSessionController session = Object.FindFirstObjectByType<NetworkSessionController>();
            NetworkManager manager = Object.FindFirstObjectByType<NetworkManager>();
            Assert.That(session, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(session.TrySetEndpointInput("127.0.0.1", "17841"), Is.True);
            Assert.That(session.StartHost(), Is.True, session.StatusText);

            float deadline = Time.realtimeSinceStartup + 3f;
            NetworkChatController chat = null;
            NetworkCoolingIncidentController incident = null;
            NetworkCarryableItem replacementPipe = null;
            NetworkCampaignController campaign = null;
            while ((chat == null || incident == null || replacementPipe == null || campaign == null)
                   && Time.realtimeSinceStartup < deadline)
            {
                chat = Object.FindFirstObjectByType<NetworkChatController>();
                incident = Object.FindFirstObjectByType<NetworkCoolingIncidentController>();
                replacementPipe = Object.FindFirstObjectByType<NetworkCarryableItem>();
                campaign = Object.FindFirstObjectByType<NetworkCampaignController>();
                yield return null;
            }

            Assert.That(chat, Is.Not.Null, "主机启动后应生成已注册的唯一通信预制体。 ");
            Assert.That(chat.IsSpawned, Is.True);
            Assert.That(Object.FindObjectsByType<NetworkChatController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(incident, Is.Not.Null, "主机启动后应生成服务器权威冷却事故。 ");
            Assert.That(incident.IsSpawned, Is.True);
            Assert.That(incident.DiagnosticChecksEnabled, Is.True);
            Assert.That(incident.Phase, Is.EqualTo(CoolingIncidentPhase.AssessSymptoms));
            Assert.That(replacementPipe, Is.Not.Null, "主机启动后应生成共享替换管件。 ");
            Assert.That(replacementPipe.IsSpawned, Is.True);
            Assert.That(campaign, Is.Not.Null, "主机启动后应生成唯一联网战役控制器。 ");
            Assert.That(campaign.IsSpawned, Is.True);
            Assert.That(campaign.Chapter, Is.EqualTo(NetworkCampaignChapter.CoolingRepair));

            CompleteCoolingIncident(incident);
            yield return null;
            Assert.That(campaign.Chapter, Is.EqualTo(NetworkCampaignChapter.RelaySurge));
            Assert.That(campaign.EnemiesRemaining, Is.EqualTo(4));

            for (int relay = 0; relay < 5; relay++)
            for (int step = 0; step < 3; step++)
                campaign.TryOperateRelayServer(relay);
            DefeatAllEnemies();
            yield return null;
            Assert.That(campaign.Chapter, Is.EqualTo(NetworkCampaignChapter.StormDefense));

            for (int wave = 0; wave < 3; wave++)
            {
                DefeatAllEnemies();
                yield return null;
                Assert.That(campaign.CanConfirmStormWave, Is.True);
                campaign.ConfirmStormWaveServer();
                yield return null;
            }
            Assert.That(campaign.Chapter, Is.EqualTo(NetworkCampaignChapter.Completed));

            manager.Shutdown();
            yield return null;
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static void CompleteCoolingIncident(NetworkCoolingIncidentController incident)
        {
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.InspectPressure, ToolKind.None), Is.True);
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.InspectPump, ToolKind.None), Is.True);
            for (int index = 0; index < 3; index++)
                Assert.That(incident.TryExecuteServer(NetworkIncidentAction.BridgeCircuit, ToolKind.CircuitBridger), Is.True);
            for (int index = 0; index < 4; index++)
                Assert.That(incident.TryExecuteServer(NetworkIncidentAction.SealLeak, ToolKind.SealantGun), Is.True);
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.OperateFastener, ToolKind.ImpactWrench), Is.True);
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.InstallPipe, ToolKind.None, true), Is.True);
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.OperateFastener, ToolKind.ImpactWrench), Is.True);
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.InspectPressure, ToolKind.None), Is.True);
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.OperatePump, ToolKind.None), Is.True);
        }

        private static void DefeatAllEnemies()
        {
            NetworkCombatEnemy[] enemies = Object.FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None);
            foreach (NetworkCombatEnemy enemy in enemies)
            {
                if (enemy.IsShielded) enemy.ApplyToolServer(ToolKind.CircuitBridger, Vector3.zero);
                for (int hit = 0; hit < 4 && enemy != null && enemy.Health > 0; hit++)
                    enemy.ApplyToolServer(ToolKind.ImpactWrench, Vector3.zero);
            }
        }
    }
}

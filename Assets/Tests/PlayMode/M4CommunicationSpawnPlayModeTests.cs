using System.Collections;
using FunGame.Incident;
using FunGame.Networking;
using FunGame.UI;
using FunGame.Tools;
using FunGame.Interaction;
using FunGame.Demo;
using System.Linq;
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

            var localPlayer = manager.LocalClient.PlayerObject;
            var interactor = localPlayer.GetComponent<ContextInteractor>();
            var guidance = localPlayer.GetComponent<DemoObjectiveGuidancePresenter>();
            Assert.That(guidance.enabled, Is.True);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(guidance.CurrentTarget, Is.Not.Null);
            Assert.That(guidance.CurrentInstruction.PrimaryTarget, Is.EqualTo(DemoGuidanceTargetKind.PressureGauge));

            // 通过实际准星和 E 入口验证：禁用的单人组件不能抢走网络请求。
            NetworkIncidentStation pressure = Object.FindObjectsByType<NetworkIncidentStation>(FindObjectsSortMode.None)
                .Single(s => s.Action == NetworkIncidentAction.InspectPressure);
            AimAt(localPlayer.gameObject, pressure.transform.position, Vector3.right);
            interactor.RefreshTarget();
            Assert.That(interactor.CurrentOption?.TargetId, Is.EqualTo("m4-pressure"));
            Assert.That(interactor.ExecuteCurrentInteraction(), Is.True);
            yield return null;
            Assert.That(incident.HasInspectedPressure, Is.True);
            var pump = GameObject.Find("Modular Cooling Pump");
            Assert.That(pump.GetComponent<ContextInteractionProxy>().enabled, Is.True);
            AimAt(localPlayer.gameObject, new Vector3(0f, 1.05f, 5.8f), Vector3.back);
            interactor.RefreshTarget();
            Assert.That(interactor.CurrentOption?.TargetId, Is.EqualTo("m4-pump-inspection"));
            Assert.That(interactor.ExecuteCurrentInteraction(), Is.True);
            yield return null;
            Assert.That(incident.HasInspectedPump, Is.True);
            yield return CompleteCoolingIncident(incident);
            Assert.That(campaign.Chapter, Is.EqualTo(NetworkCampaignChapter.CoolingRepair));
            Assert.That(campaign.CoolingRunsCompleted, Is.EqualTo(1));
            yield return CompleteCoolingIncident(incident);
            Assert.That(campaign.Chapter, Is.EqualTo(NetworkCampaignChapter.RelaySurge));
            var source = Object.FindFirstObjectByType<SinglePlayerDemoController>(FindObjectsInactive.Include);
            Assert.That(campaign.EnemiesRemaining, Is.EqualTo(source.RelayDefenseEncounter.Enemies.Count));
            Assert.That(campaign.StormWaveCount, Is.EqualTo(source.StormEncounters.Count));
            foreach (NetworkCombatEnemy enemy in Object.FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None))
            {
                Assert.That(enemy.Template, Is.Not.Null);
                Assert.That(enemy.Health, Is.EqualTo(enemy.Template.MaxHealth));
                Assert.That(Vector3.Distance(enemy.transform.position, enemy.Template.transform.position), Is.LessThan(0.5f));
            }

            campaign.ApplyCoreDamageServer(10000);
            Assert.That(campaign.IsCurrentChapterFailed, Is.True);
            NetworkCampaignStation recovery = Object.FindObjectsByType<NetworkCampaignStation>(FindObjectsSortMode.None)
                .Single(s => s.IsCalibrationConsole && s.StationIndex == 1);
            AimAt(localPlayer.gameObject, recovery.transform.position, Vector3.back);
            interactor.RefreshTarget();
            Assert.That(interactor.ExecuteCurrentInteraction(), Is.True);
            yield return null;
            Assert.That(campaign.IsCurrentChapterFailed, Is.False);
            Assert.That(campaign.EnemiesRemaining, Is.EqualTo(source.RelayDefenseEncounter.Enemies.Count));

            for (int relay = 0; relay < 5; relay++)
            for (int step = 0; step < 3; step++)
                campaign.TryOperateRelayServer(relay);
            DefeatAllEnemies();
            yield return null;
            Assert.That(campaign.Chapter, Is.EqualTo(NetworkCampaignChapter.StormDefense));

            for (int wave = 0; wave < campaign.StormWaveCount; wave++)
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

        private static IEnumerator CompleteCoolingIncident(NetworkCoolingIncidentController incident)
        {
            if (!incident.HasInspectedPressure) Assert.That(incident.TryExecuteServer(NetworkIncidentAction.InspectPressure, ToolKind.None), Is.True);
            if (!incident.HasInspectedPump) Assert.That(incident.TryExecuteServer(NetworkIncidentAction.InspectPump, ToolKind.None), Is.True);
            for (int index = 0; index < 3; index++)
                Assert.That(incident.TryExecuteServer(NetworkIncidentAction.BridgeCircuit, ToolKind.CircuitBridger), Is.True);
            for (int index = 0; index < 4; index++)
                Assert.That(incident.TryExecuteServer(NetworkIncidentAction.SealLeak, ToolKind.SealantGun), Is.True);
            yield return null;
            NetworkCombatEnemy[] spawned = Object.FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None);
            Assert.That(spawned.Count(e => e.Kind == NetworkEnemyKind.Swarm), Is.EqualTo(5));
            NetworkCombatEnemy elite = spawned.Single(e => e.IsShielded);
            int eliteHealth = elite.Health;
            elite.ApplyToolServer(ToolKind.ImpactWrench, elite.transform.position + Vector3.back);
            Assert.That(elite.Health, Is.EqualTo(eliteHealth));
            elite.ApplyToolServer(ToolKind.CircuitBridger, elite.transform.position + Vector3.back);
            Assert.That(elite.IsShielded, Is.False);
            Assert.That(elite.IsStunned, Is.True);
            NetworkCombatEnemy[] swarm = spawned.Where(e => e.Kind == NetworkEnemyKind.Swarm).ToArray();
            NetworkCombatEnemy primary = swarm.OrderByDescending(e => swarm.Count(other => Vector3.Distance(e.transform.position, other.transform.position) <= 1.65f)).First();
            var campaign = Object.FindFirstObjectByType<NetworkCampaignController>();
            int before = campaign.EnemiesRemaining;
            primary.ApplyToolServer(ToolKind.SealantGun, primary.transform.position + Vector3.back);
            Assert.That(before - campaign.EnemiesRemaining, Is.GreaterThan(1), "Spray must clear nearby swarm members");
            DefeatAllEnemies();
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.OperateFastener, ToolKind.ImpactWrench), Is.True);
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.InstallPipe, ToolKind.None, true), Is.True);
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.OperateFastener, ToolKind.ImpactWrench), Is.True);
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.InspectPressure, ToolKind.None), Is.True);
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.OperatePump, ToolKind.None), Is.True);
            yield return null;
        }

        private static void DefeatAllEnemies()
        {
            NetworkCombatEnemy[] enemies = Object.FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None);
            foreach (NetworkCombatEnemy enemy in enemies)
            {
                if (enemy.IsShielded) enemy.ApplyToolServer(ToolKind.CircuitBridger, Vector3.zero);
                for (int hit = 0; hit < 8 && enemy != null && enemy.Health > 0; hit++)
                    enemy.ApplyToolServer(ToolKind.ImpactWrench, Vector3.zero);
            }
        }

        private static void AimAt(GameObject player, Vector3 target, Vector3 outward)
        {
            var controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = target + outward * 2f - Vector3.up * 0.65f;
            Camera camera = player.GetComponentInChildren<Camera>(true);
            camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position);
            controller.enabled = true;
            Physics.SyncTransforms();
        }
    }
}

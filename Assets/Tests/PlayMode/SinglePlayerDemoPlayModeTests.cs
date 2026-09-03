using System.Collections;
using FunGame.Combat;
using FunGame.Demo;
using FunGame.Incident;
using FunGame.Interaction;
using FunGame.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class SinglePlayerDemoPlayModeTests
    {
        [UnityTest]
        public IEnumerator 三章控制器可以从冷却事故一路推进到最终结算()
        {
            var incidentObject = new GameObject("Demo Test Incident");
            var incident = incidentObject.AddComponent<CoolingIncidentController>();
            incident.ConfigureExtendedIncident(true);
            incident.ConfigureTemperature(65f, 100f, 0f);

            CombatEncounterController relayDefense = CreateEmptyEncounter("Relay Defense");
            var relayObjects = new GameObject[5];
            var relays = new DemoRelayTarget[5];
            for (int index = 0; index < relays.Length; index++)
            {
                relayObjects[index] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                relays[index] = relayObjects[index].AddComponent<DemoRelayTarget>();
                relays[index].Configure($"relay-{index}", $"继电器 {index + 1}");
            }

            var stormWaves = new[]
            {
                CreateEmptyEncounter("Storm Wave 1"),
                CreateEmptyEncounter("Storm Wave 2"),
                CreateEmptyEncounter("Storm Wave 3"),
                CreateEmptyEncounter("Storm Wave 4"),
                CreateEmptyEncounter("Storm Wave 5")
            };
            var consoleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var campaignConsole = consoleObject.AddComponent<DemoCalibrationConsole>();
            var campaignObject = new GameObject("Demo Campaign");
            var campaign = campaignObject.AddComponent<SinglePlayerDemoController>();
            campaign.Configure(incident, relayDefense, relays, stormWaves, campaignConsole);
            var relayCompartment = new GameObject("Relay Compartment");
            var stormChamber = new GameObject("Storm Chamber");
            var relayDoor = new GameObject("Relay Door");
            var stormDoor = new GameObject("Storm Door");
            var presentationObject = new GameObject("Demo Presentation");
            var presentation = presentationObject.AddComponent<DemoChapterPresentation>();
            presentation.Configure(
                campaign,
                new Transform[0],
                new Renderer[0],
                new Light[0],
                relayCompartment,
                stormChamber,
                relayDoor,
                stormDoor);
            CreateCircuitActor(out GameObject actor, out PlayerToolbelt toolbelt);

            yield return null;
            Assert.That(relayCompartment.activeSelf, Is.False);
            Assert.That(stormChamber.activeSelf, Is.False);
            Assert.That(relayDoor.activeSelf, Is.True);
            Assert.That(stormDoor.activeSelf, Is.False);

            CompleteIncident(incident);
            yield return null;
            Assert.That(campaign.Chapter, Is.EqualTo(SinglePlayerDemoChapter.CoolingEmergency));
            Assert.That(campaign.CoolingRunsCompleted, Is.EqualTo(1));
            Assert.That(incident.RunState, Is.EqualTo(CoolingIncidentRunState.Active));
            Assert.That(campaign.ShipCapabilityStatus, Does.Contain("配电离线"));

            CompleteIncident(incident);
            yield return null;
            Assert.That(campaign.Chapter, Is.EqualTo(SinglePlayerDemoChapter.RelaySurge));
            Assert.That(campaign.ShipCapabilityStatus, Does.Contain("冷却在线"));
            presentation.RefreshChapterVisibility(true);
            Assert.That(relayCompartment.activeSelf, Is.True);
            Assert.That(stormChamber.activeSelf, Is.False);
            Assert.That(relayDoor.activeSelf, Is.False);
            Assert.That(stormDoor.activeSelf, Is.True);

            foreach (DemoRelayTarget relay in relays)
            {
                Assert.That(relay.ApplyTool(toolbelt), Is.True);
                Assert.That(relay.ApplyTool(toolbelt), Is.True);
                Assert.That(relay.ApplyTool(toolbelt), Is.True);
            }

            relayDefense.NotifyEnemyDefeated();
            yield return null;
            Assert.That(campaign.Chapter, Is.EqualTo(SinglePlayerDemoChapter.StormCalibration));
            Assert.That(campaign.ShipCapabilityStatus, Does.Contain("配电在线"));
            presentation.RefreshChapterVisibility(true);
            Assert.That(stormChamber.activeSelf, Is.True);
            Assert.That(stormDoor.activeSelf, Is.False);

            for (int wave = 0; wave < stormWaves.Length; wave++)
            {
                stormWaves[wave].NotifyEnemyDefeated();
                yield return null;
                Assert.That(campaign.IsCampaignConsoleAvailable, Is.True);
                Assert.That(campaign.ExecuteCampaignConsole(), Is.True);
                yield return null;
            }

            Assert.That(campaign.IsCompleted, Is.True);
            Assert.That(campaign.ShipCapabilityStatus, Does.Contain("全部在线"));

            Object.Destroy(actor);
            Object.Destroy(campaignObject);
            Object.Destroy(presentationObject);
            Object.Destroy(relayCompartment);
            Object.Destroy(stormChamber);
            Object.Destroy(relayDoor);
            Object.Destroy(stormDoor);
            Object.Destroy(consoleObject);
            foreach (GameObject relayObject in relayObjects) Object.Destroy(relayObject);
            foreach (CombatEncounterController wave in stormWaves) Object.Destroy(wave.gameObject);
            Object.Destroy(relayDefense.gameObject);
            Object.Destroy(incidentObject);
        }

        private static CombatEncounterController CreateEmptyEncounter(string name)
        {
            var encounterObject = new GameObject(name);
            var encounter = encounterObject.AddComponent<CombatEncounterController>();
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.transform.SetParent(encounterObject.transform);
            var target = targetObject.AddComponent<DefendableSystemTarget>();
            target.Configure(100);
            encounter.Configure(target, new InterferenceEnemy[0], false);
            return encounter;
        }

        private static void CompleteIncident(CoolingIncidentController incident)
        {
            incident.TryInspectPressure();
            incident.TryInspectPump();
            incident.TryAdvanceCircuitBridge();
            incident.TryAdvanceCircuitBridge();
            incident.TryAdvanceCircuitBridge();
            incident.AddSealProgress(1f);
            incident.TryLoosen();
            incident.TryInstallPipe();
            incident.TryTighten();
            incident.TryInspectPressure();
            incident.TryResetPump();
        }

        private static void CreateCircuitActor(out GameObject actor, out PlayerToolbelt toolbelt)
        {
            actor = new GameObject("Demo Circuit Actor");
            var cameraObject = new GameObject("Demo Circuit Camera");
            cameraObject.transform.SetParent(actor.transform, false);
            cameraObject.AddComponent<Camera>();
            toolbelt = actor.AddComponent<PlayerToolbelt>();
            actor.AddComponent<ContextInteractor>();
            toolbelt.Equip(ToolKind.CircuitBridger);
        }
    }
}

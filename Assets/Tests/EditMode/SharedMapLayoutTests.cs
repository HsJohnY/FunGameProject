using System.Linq;
using FunGame.Combat;
using FunGame.Demo;
using FunGame.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Tests.EditMode
{
    public sealed class SharedMapLayoutTests
    {
        [Test]
        public void BothModesLoadOnlyTheCanonicalSoloMap()
        {
            Assert.That(GameMenuController.CooperativeScene, Is.EqualTo(GameMenuController.SinglePlayerScene));
            Assert.That(EditorBuildSettings.scenes.Count(s => s.enabled), Is.EqualTo(1));
            Assert.That(EditorBuildSettings.scenes.Single(s => s.enabled).path, Does.EndWith("SinglePlayer_ThreeChapterDemo.unity"));
            Scene scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/SinglePlayer_ThreeChapterDemo.unity", OpenSceneMode.Additive);
            try
            {
                var mode = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SharedMapModeController>(true)).Single();
                Assert.That(mode.MapRoot, Is.Not.Null);
                Assert.That(mode.SoloPlayer, Is.Not.Null);
                var campaign = mode.MapRoot.GetComponentInChildren<SinglePlayerDemoController>(true);
                int previousHealth = 0;
                foreach (CombatEncounterController wave in campaign.StormEncounters)
                {
                    Assert.That(wave.Briefing, Is.Not.Empty);
                    Assert.That(wave.Enemies.All(e => e.HasCombatPosition && e.DeploymentDelay >= 4f), Is.True);
                    int health = wave.Enemies.Sum(e => e.MaxHealth);
                    Assert.That(health, Is.GreaterThan(previousHealth));
                    previousHealth = health;
                }
                Assert.That(campaign.StormEncounters.Last().Enemies.Select(e => e.DeploymentDelay).Distinct().Count(), Is.EqualTo(2));
                Assert.That(campaign.StormEncounters.SelectMany(w => w.Enemies).Where(e => e.RequiresCircuitDisruption)
                    .All(e => e.DeploymentDelay == 8f), Is.True);
                var chapterRacks = new[]
                {
                    new[] { "Impact Wrench Rack", "Sealant Gun Rack", "Circuit Bridger Rack" },
                    new[] { "Relay Wrench Station", "Relay Sealant Station", "Relay Bridger Station" },
                    new[] { "Storm Wrench Station", "Storm Sealant Station", "Storm Bridger Station" }
                };
                var racks = mode.MapRoot.GetComponentsInChildren<FunGame.Tools.ToolRackInteractable>(true);
                Assert.That(racks.Length, Is.EqualTo(9));
                for (int chapter = 0; chapter < chapterRacks.Length; chapter++)
                {
                    var localRacks = chapterRacks[chapter].Select(name => racks.Single(r => r.name == name)).ToArray();
                    Assert.That(localRacks.Select(r => r.OfferedTool), Is.EquivalentTo(new[]
                    {
                        FunGame.Tools.ToolKind.ImpactWrench,
                        FunGame.Tools.ToolKind.SealantGun,
                        FunGame.Tools.ToolKind.CircuitBridger
                    }), $"Chapter {chapter + 1} must provide every tool exactly once.");
                    Assert.That(localRacks.All(r => r.transform.position.z >= chapter * 20f - 10f &&
                        r.transform.position.z < chapter * 20f + 10f), Is.True,
                        $"Chapter {chapter + 1} racks must be inside their own room.");
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void RelayRacksAreSeparatedAndPlateFacesTheWalkway()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/SinglePlayer_ThreeChapterDemo.unity", OpenSceneMode.Additive);
            try
            {
                Transform[] transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                transforms.Single(t => t.name == "Shared Expedition Map").gameObject.SetActive(true);
                transforms.Single(t => t.name == "Chapter 2 - Power Relay Compartment").gameObject.SetActive(true);
                Physics.SyncTransforms();
                var relays = transforms.Where(t => t.GetComponent<DemoRelayTarget>() != null).Select(t => t.GetComponent<Collider>()).ToArray();
                foreach (string name in new[] { "Relay Bridger Station", "Relay Wrench Station", "Relay Sealant Station" })
                {
                    Bounds rack = transforms.Single(t => t.name == name).GetComponent<Collider>().bounds;
                    foreach (Collider relay in relays)
                    {
                        Bounds safe = relay.bounds;
                        safe.Expand(1f);
                        Assert.That(rack.Intersects(safe), Is.False, name + " / " + relay.name);
                    }
                }
                Transform text = transforms.Single(t => t.name == "Engraved Number 325");
                Assert.That(Vector3.Dot(-text.forward, Vector3.right), Is.GreaterThan(0.99f));
                Assert.That(text.GetComponent<TextMesh>().text, Is.EqualTo("325"));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}

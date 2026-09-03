using System.Linq;
using FunGame.Combat;
using FunGame.Demo;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Tests.EditMode
{
    public sealed class SinglePlayerDemoSceneTests
    {
        private const string ScenePath = "Assets/Game/Scenes/SinglePlayer_ThreeChapterDemo.unity";

        [Test]
        public void 三章场景包含完整章节门禁怪物变式与325彩蛋()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "Single Player Three Chapter Demo");
                Assert.That(root, Is.Not.Null);

                SinglePlayerDemoController campaign = root.GetComponent<SinglePlayerDemoController>();
                DemoObjectiveGuidancePresenter guidance = root.GetComponent<DemoObjectiveGuidancePresenter>();
                DemoRelayTarget[] relays = root.GetComponentsInChildren<DemoRelayTarget>(true);
                CombatEncounterController[] encounters = root.GetComponentsInChildren<CombatEncounterController>(true);
                InterferenceEnemy[] enemies = root.GetComponentsInChildren<InterferenceEnemy>(true);
                DemoEasterEgg325Interactable secret = root.GetComponentInChildren<DemoEasterEgg325Interactable>(true);
                TextMesh engravedNumber = root.GetComponentsInChildren<TextMesh>(true).FirstOrDefault(item => item.text == "325");
                Transform stormCore = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(item => item.name == "Shared Storm Calibration Core");
                Transform relayCompartment = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(item => item.name == "Chapter 2 - Power Relay Compartment");
                Transform stormChamber = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(item => item.name == "Chapter 3 - Storm Core Chamber");
                Transform player = scene.GetRootGameObjects()
                    .FirstOrDefault(item => item.name == "Local First Person Player")?.transform;

                Assert.That(campaign, Is.Not.Null);
                Assert.That(guidance, Is.Not.Null);
                Assert.That(campaign.RequiredCoolingRunCount, Is.EqualTo(2));
                Assert.That(campaign.RequiredRelayCount, Is.EqualTo(5));
                Assert.That(campaign.StormWaveCount, Is.EqualTo(5));
                Assert.That(relays.Length, Is.EqualTo(5));
                Assert.That(encounters.Length, Is.EqualTo(6));
                Assert.That(enemies.Length, Is.EqualTo(27));
                Assert.That(enemies.Count(item => item.Behavior == InterferenceEnemyBehavior.Direct), Is.GreaterThan(0));
                Assert.That(enemies.Count(item => item.Behavior == InterferenceEnemyBehavior.FlankingAttach), Is.GreaterThan(0));
                Assert.That(enemies.Count(item => item.Behavior == InterferenceEnemyBehavior.RangedPulse), Is.GreaterThan(0));
                Assert.That(secret, Is.Not.Null);
                Assert.That(engravedNumber, Is.Not.Null);
                Assert.That(stormCore, Is.Not.Null);
                Assert.That(relayCompartment, Is.Not.Null);
                Assert.That(stormChamber, Is.Not.Null);
                Assert.That(player, Is.Not.Null);
                Assert.That(relays.All(item => item.transform.IsChildOf(relayCompartment)), Is.True);
                Assert.That(stormCore.IsChildOf(stormChamber), Is.True);
                Assert.That(relayCompartment.gameObject.activeSelf, Is.False, "第二章舱室不应在第一章提前显示。");
                Assert.That(stormChamber.gameObject.activeSelf, Is.False, "第三章舱室不应在第一章提前显示。");
                Assert.That(FindTransform(scene, "Sealed Power Compartment Door")?.gameObject.activeSelf, Is.True);
                Assert.That(FindTransform(scene, "Sealed Storm Chamber Door")?.gameObject.activeSelf, Is.False,
                    "第三章舱门不应隔着第一章舱门提前显示。");
                Assert.That(FindTransform(scene, "Silent Tutorial Route - Rack Enemy Device"), Is.Null);
                Assert.That(FindTransform(scene, "Remote Maintenance Suit Scale Reference"), Is.Null);
                Assert.That(FindTransform(scene, "Pressure Gauge Needle Model"), Is.Not.Null);
                Assert.That(FindTransform(scene, "Replacement Pipe Model"), Is.Not.Null);
                Assert.That(FindTransform(scene, "Relay Phase Coil"), Is.Not.Null);
                Assert.That(FindTransform(scene, "Calibration Console Screen"), Is.Not.Null);
                Assert.That(Vector3.Distance(player.position, stormCore.position), Is.GreaterThan(35f),
                    "风暴核心必须位于独立后舱，不能与第一章冷却设备混在一起。");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void 后续章节关键交互物与怪物外形均有可命中的碰撞范围()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject root = scene.GetRootGameObjects().First(item => item.name == "Single Player Three Chapter Demo");
                Transform relayCompartment = FindTransform(scene, "Chapter 2 - Power Relay Compartment");
                Transform stormChamber = FindTransform(scene, "Chapter 3 - Storm Core Chamber");
                relayCompartment.gameObject.SetActive(true);
                stormChamber.gameObject.SetActive(true);

                foreach (DemoRelayTarget relay in root.GetComponentsInChildren<DemoRelayTarget>(true))
                {
                    AssertRenderersCovered(relay.gameObject, relay.GetComponentsInChildren<Renderer>(true));
                }

                GameObject console = FindTransform(scene, "Ship Systems Command Console").gameObject;
                AssertRenderersCovered(console, console.GetComponentsInChildren<Renderer>(true));

                AssertSiblingDecorationsCovered(scene, "Relay Bridger Station");
                AssertSiblingDecorationsCovered(scene, "Relay Wrench Station");
                AssertSiblingDecorationsCovered(scene, "Storm Wrench Station");

                foreach (InterferenceEnemy enemy in root.GetComponentsInChildren<InterferenceEnemy>(true))
                {
                    for (Transform current = enemy.transform; current != null; current = current.parent)
                    {
                        current.gameObject.SetActive(true);
                    }
                    foreach (MeshRenderer renderer in enemy.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        Assert.That(renderer.GetComponent<Collider>(), Is.Not.Null,
                            $"{enemy.name} / {renderer.name} 的可见外形缺少对应碰撞体。");
                    }
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertSiblingDecorationsCovered(Scene scene, string rackName)
        {
            GameObject rack = FindTransform(scene, rackName).gameObject;
            Collider collider = rack.GetComponent<Collider>();
            Renderer[] renderers = rack.transform.parent.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.name.StartsWith(rackName)).ToArray();
            foreach (Renderer renderer in renderers)
            {
                Assert.That(Contains(collider.bounds, renderer.bounds), Is.True,
                    $"{rackName} / {renderer.name} 的可见范围超出工具架交互碰撞体。");
            }
        }

        private static void AssertRenderersCovered(GameObject target, Renderer[] renderers)
        {
            Collider collider = target.GetComponent<Collider>();
            Assert.That(collider, Is.Not.Null, $"{target.name} 缺少交互碰撞体。");
            foreach (Renderer renderer in renderers)
            {
                Assert.That(Contains(collider.bounds, renderer.bounds), Is.True,
                    $"{target.name} / {renderer.name} 的可见范围超出交互碰撞体。");
            }
        }

        private static bool Contains(Bounds outer, Bounds inner)
        {
            outer.Expand(0.03f);
            return outer.Contains(inner.min) && outer.Contains(inner.max);
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            return scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == name);
        }
    }
}

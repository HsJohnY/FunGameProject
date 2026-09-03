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
                Assert.That(player, Is.Not.Null);
                Assert.That(FindTransform(scene, "Pressure Gauge Needle Model"), Is.Not.Null);
                Assert.That(FindTransform(scene, "Replacement Pipe Model"), Is.Not.Null);
                Assert.That(FindTransform(scene, "Relay Phase Coil"), Is.Not.Null);
                Assert.That(FindTransform(scene, "Calibration Console Screen"), Is.Not.Null);
                Assert.That(Vector3.Distance(player.position, stormCore.position), Is.GreaterThan(4f),
                    "风暴核心不能遮挡第一章的玩家出生镜头。");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            return scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == name);
        }
    }
}

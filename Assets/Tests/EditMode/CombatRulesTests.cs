using FunGame.Combat;
using FunGame.Tools;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Tests.EditMode
{
    public sealed class CombatRulesTests
    {
        [Test]
        public void InterferenceEnemy_三次扳手命中后被击败()
        {
            var rules = new InterferenceEnemyRules(3, 1f);

            Assert.That(rules.ReceiveHit(1), Is.False);
            Assert.That(rules.Health, Is.EqualTo(2));
            Assert.That(rules.ReceiveHit(1), Is.False);
            Assert.That(rules.ReceiveHit(1), Is.True);
            Assert.That(rules.Health, Is.Zero);
        }

        [Test]
        public void InterferenceEnemy_进入范围后先蓄力再按间隔攻击()
        {
            var rules = new InterferenceEnemyRules(3, 1f, 0.4f);

            Assert.That(rules.Advance(0f, false), Is.EqualTo(InterferenceEnemyAction.None));
            Assert.That(rules.Advance(0f, true), Is.EqualTo(InterferenceEnemyAction.TelegraphStarted));
            Assert.That(rules.IsTelegraphing, Is.True);
            Assert.That(rules.Advance(0.39f, true), Is.EqualTo(InterferenceEnemyAction.None));
            Assert.That(rules.Advance(0.02f, true), Is.EqualTo(InterferenceEnemyAction.AttackCommitted));
            Assert.That(rules.Advance(1f, true), Is.EqualTo(InterferenceEnemyAction.TelegraphStarted));
        }

        [Test]
        public void InterferenceEnemy_重置后恢复生命和蓄力资格()
        {
            var rules = new InterferenceEnemyRules(2, 1f);
            rules.ReceiveHit(2);

            rules.Reset();

            Assert.That(rules.Health, Is.EqualTo(2));
            Assert.That(rules.IsDefeated, Is.False);
            Assert.That(rules.Advance(0f, true), Is.EqualTo(InterferenceEnemyAction.TelegraphStarted));
        }

        [Test]
        public void DefendableSystem_完整度耗尽后离线且可重置()
        {
            var rules = new DefendableSystemRules(20);

            Assert.That(rules.ApplyInterference(10), Is.False);
            Assert.That(rules.ApplyInterference(10), Is.True);
            Assert.That(rules.IsOffline, Is.True);

            rules.Reset();

            Assert.That(rules.Integrity, Is.EqualTo(20));
            Assert.That(rules.IsOffline, Is.False);
        }

        [Test]
        public void CombatScene_敌人和设备均配置实体碰撞体积()
        {
            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Game/Scenes/Combat_DefenseSandbox.unity",
                OpenSceneMode.Additive);

            try
            {
                GameObject enemy = FindRoot(scene, "Line Interference Creature");
                GameObject target = FindRoot(scene, "Defendable Cooling Control Unit");

                Assert.That(enemy, Is.Not.Null);
                Assert.That(target, Is.Not.Null);
                Assert.That(enemy.TryGetComponent(out CapsuleCollider enemyCollider), Is.True);
                Assert.That(enemyCollider.isTrigger, Is.False);
                Assert.That(enemy.TryGetComponent(out Rigidbody enemyBody), Is.True);
                Assert.That(enemyBody.isKinematic, Is.True);
                Assert.That(enemyBody.useGravity, Is.False);
                Assert.That(target.TryGetComponent(out BoxCollider targetCollider), Is.True);
                Assert.That(targetCollider.isTrigger, Is.False);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void CombatRepairScene_包含双行为敌人和维修联动组件()
        {
            Scene scene = EditorSceneManager.OpenScene(
                "Assets/Game/Scenes/Combat_CoolingBayIntegration.unity",
                OpenSceneMode.Additive);

            try
            {
                CoolingCombatIntegrationController integration = null;
                DefendableSystemIndicator indicator = null;
                int enemyCount = 0;
                bool hasDirect = false;
                bool hasFlanking = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    integration = integration ?? root.GetComponentInChildren<CoolingCombatIntegrationController>(true);
                    indicator = indicator ?? root.GetComponentInChildren<DefendableSystemIndicator>(true);
                    foreach (InterferenceEnemy enemy in root.GetComponentsInChildren<InterferenceEnemy>(true))
                    {
                        enemyCount++;
                        hasDirect |= enemy.Behavior == InterferenceEnemyBehavior.Direct;
                        hasFlanking |= enemy.Behavior == InterferenceEnemyBehavior.FlankingAttach;
                    }
                }

                Assert.That(integration, Is.Not.Null);
                Assert.That(indicator, Is.Not.Null);
                Assert.That(enemyCount, Is.EqualTo(2));
                Assert.That(hasDirect, Is.True);
                Assert.That(hasFlanking, Is.True);
                GameObject walkwayA = FindInScene(scene, "Walkway A");
                GameObject walkwayB = FindInScene(scene, "Walkway B");
                Assert.That(walkwayA, Is.Not.Null);
                Assert.That(walkwayB, Is.Not.Null);
                Assert.That(walkwayA.GetComponent<Collider>(), Is.Null, "步道标记只能用于视觉引导，不应阻挡干扰体");
                Assert.That(walkwayB.GetComponent<Collider>(), Is.Null, "步道标记只能用于视觉引导，不应阻挡干扰体");
                Assert.That(FindInScene(scene, "Impact Wrench Rack"), Is.Not.Null);
                Assert.That(FindInScene(scene, "Sealant Gun Rack"), Is.Not.Null);
                Assert.That(FindInScene(scene, "Circuit Bridger Rack"), Is.Not.Null);
                Assert.That(FindInScene(scene, "Mechanical Fastener Demo"), Is.Not.Null);
                Assert.That(FindInScene(scene, "Sealant Leak Demo"), Is.Not.Null);
                GameObject circuitTask = FindInScene(scene, "Circuit Bridge Demo");
                Assert.That(circuitTask, Is.Not.Null);
                Assert.That(circuitTask.GetComponent<CircuitBridgeTarget>(), Is.Not.Null);
                Assert.That(FindInScene(scene, "Circuit Bridger Visual"), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    return root;
                }
            }

            return null;
        }

        private static GameObject FindInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                {
                    if (candidate.name == objectName)
                    {
                        return candidate.gameObject;
                    }
                }
            }

            return null;
        }
    }
}

using System.Collections;
using FunGame.Combat;
using FunGame.Interaction;
using FunGame.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class CombatPlayModeTests
    {
        [UnityTest]
        public IEnumerator ToolController_冲击扳手三次击退基础敌人并完成遭遇()
        {
            CreateEncounter(
                new Vector3(0f, 100f, 6f),
                new Vector3(0f, 100f, 2f),
                out GameObject encounterObject,
                out CombatEncounterController encounter,
                out GameObject targetObject,
                out _,
                out GameObject enemyObject,
                out InterferenceEnemy enemy,
                knockbackDistance: 0f);
            CreateToolActor(new Vector3(0f, 100f, 0f), out GameObject actor, out PlayerToolbelt toolbelt, out ToolController controller);
            toolbelt.Equip(ToolKind.ImpactWrench);
            Physics.SyncTransforms();

            yield return null;

            for (int hitIndex = 0; hitIndex < 3; hitIndex++)
            {
                controller.RefreshTarget();
                Assert.That(controller.CurrentOption.HasValue, Is.True);
                Assert.That(controller.CurrentOption.Value.ActionLabel, Is.EqualTo("击退"));
                Assert.That(controller.ExecuteCurrentToolAction(), Is.True);
            }

            Assert.That(enemy.IsDefeated, Is.True);
            Assert.That(encounter.State, Is.EqualTo(CombatEncounterState.Succeeded));

            Object.Destroy(actor);
            Object.Destroy(enemyObject);
            Object.Destroy(targetObject);
            Object.Destroy(encounterObject);
        }

        [UnityTest]
        public IEnumerator InterferenceEnemy_错误工具不会造成伤害()
        {
            CreateEncounter(
                new Vector3(0f, 110f, 6f),
                new Vector3(0f, 110f, 2f),
                out GameObject encounterObject,
                out _,
                out GameObject targetObject,
                out _,
                out GameObject enemyObject,
                out InterferenceEnemy enemy,
                knockbackDistance: 0f);
            CreateToolActor(new Vector3(0f, 110f, 0f), out GameObject actor, out PlayerToolbelt toolbelt, out ToolController controller);
            toolbelt.Equip(ToolKind.SealantGun);
            Physics.SyncTransforms();

            yield return null;
            controller.RefreshTarget();

            Assert.That(controller.CurrentOption.HasValue, Is.True);
            Assert.That(controller.CurrentOption.Value.IsAvailable, Is.False);
            Assert.That(controller.CurrentOption.Value.BlockedReason, Is.EqualTo("需要冲击扳手"));
            Assert.That(controller.ExecuteCurrentToolAction(), Is.False);
            Assert.That(enemy.Health, Is.EqualTo(enemy.MaxHealth));

            Object.Destroy(actor);
            Object.Destroy(enemyObject);
            Object.Destroy(targetObject);
            Object.Destroy(encounterObject);
        }

        [UnityTest]
        public IEnumerator InterferenceEnemy_被推向设备时不会穿入且仍可连续命中()
        {
            CreateEncounter(
                new Vector3(0f, 115f, 4f),
                new Vector3(0f, 115f, 2f),
                out GameObject encounterObject,
                out CombatEncounterController encounter,
                out GameObject targetObject,
                out _,
                out GameObject enemyObject,
                out InterferenceEnemy enemy,
                knockbackDistance: 2f);
            CreateToolActor(new Vector3(0f, 115f, 0f), out GameObject actor, out PlayerToolbelt toolbelt, out ToolController controller);
            toolbelt.Equip(ToolKind.ImpactWrench);
            Physics.SyncTransforms();

            yield return null;

            Collider targetCollider = targetObject.GetComponent<Collider>();
            Collider enemyCollider = enemyObject.GetComponent<Collider>();
            for (int hitIndex = 0; hitIndex < 3; hitIndex++)
            {
                controller.RefreshTarget();
                Assert.That(controller.CurrentOption.HasValue, Is.True, $"第 {hitIndex + 1} 次命中前敌人应保持可选中");
                Assert.That(controller.ExecuteCurrentToolAction(), Is.True);
                Physics.SyncTransforms();

                if (!enemy.IsDefeated)
                {
                    bool overlaps = Physics.ComputePenetration(
                        enemyCollider,
                        enemyObject.transform.position,
                        enemyObject.transform.rotation,
                        targetCollider,
                        targetObject.transform.position,
                        targetObject.transform.rotation,
                        out _,
                        out _);
                    Assert.That(overlaps, Is.False, "击退不得让敌人穿入被保护设备");
                }
            }

            Assert.That(enemy.IsDefeated, Is.True);
            Assert.That(encounter.State, Is.EqualTo(CombatEncounterState.Succeeded));

            Object.Destroy(actor);
            Object.Destroy(enemyObject);
            Object.Destroy(targetObject);
            Object.Destroy(encounterObject);
        }

        [UnityTest]
        public IEnumerator InterferenceEnemy_接近设备后持续干扰并触发失败()
        {
            CreateEncounter(
                new Vector3(0f, 120f, 0f),
                new Vector3(0f, 120f, 0.5f),
                out GameObject encounterObject,
                out CombatEncounterController encounter,
                out GameObject targetObject,
                out DefendableSystemTarget target,
                out GameObject enemyObject,
                out _,
                targetIntegrity: 20,
                attackRange: 2f,
                attackIntervalSeconds: 0.05f,
                interferenceDamage: 10);

            yield return new WaitForSeconds(0.12f);

            Assert.That(target.IsOffline, Is.True);
            Assert.That(encounter.State, Is.EqualTo(CombatEncounterState.Failed));

            Object.Destroy(enemyObject);
            Object.Destroy(targetObject);
            Object.Destroy(encounterObject);
        }

        [UnityTest]
        public IEnumerator CombatEncounter_失败后重置会恢复敌人与设备()
        {
            CreateEncounter(
                new Vector3(0f, 130f, 0f),
                new Vector3(0f, 130f, 0.5f),
                out GameObject encounterObject,
                out CombatEncounterController encounter,
                out GameObject targetObject,
                out DefendableSystemTarget target,
                out GameObject enemyObject,
                out InterferenceEnemy enemy,
                targetIntegrity: 10,
                attackRange: 2f,
                attackIntervalSeconds: 0.05f,
                interferenceDamage: 10);

            yield return null;
            Assert.That(encounter.State, Is.EqualTo(CombatEncounterState.Failed));

            Assert.That(encounter.ResetEncounter(), Is.True);
            Assert.That(encounter.State, Is.EqualTo(CombatEncounterState.Active));
            Assert.That(target.Integrity, Is.EqualTo(target.MaxIntegrity));
            Assert.That(enemy.Health, Is.EqualTo(enemy.MaxHealth));
            Assert.That(enemy.GetComponent<Collider>().enabled, Is.True);
            Assert.That(encounter.ResetCount, Is.EqualTo(1));

            Object.Destroy(enemyObject);
            Object.Destroy(targetObject);
            Object.Destroy(encounterObject);
        }

        private static void CreateEncounter(
            Vector3 targetPosition,
            Vector3 enemyPosition,
            out GameObject encounterObject,
            out CombatEncounterController encounter,
            out GameObject targetObject,
            out DefendableSystemTarget target,
            out GameObject enemyObject,
            out InterferenceEnemy enemy,
            int targetIntegrity = 60,
            float attackRange = 0.1f,
            float attackIntervalSeconds = 1f,
            int interferenceDamage = 10,
            float knockbackDistance = 1.25f)
        {
            encounterObject = new GameObject("Test Combat Encounter");
            encounter = encounterObject.AddComponent<CombatEncounterController>();

            targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.transform.position = targetPosition;
            target = targetObject.AddComponent<DefendableSystemTarget>();
            target.Configure(targetIntegrity);

            enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.position = enemyPosition;
            enemy = enemyObject.AddComponent<InterferenceEnemy>();
            encounter.Configure(target, enemy);
            enemy.Configure(
                target,
                encounter,
                configuredMaxHealth: 3,
                configuredMoveSpeed: 0.1f,
                configuredAttackRange: attackRange,
                configuredAttackIntervalSeconds: attackIntervalSeconds,
                configuredInterferenceDamage: interferenceDamage,
                configuredWrenchDamage: 1,
                configuredKnockbackDistance: knockbackDistance);
        }

        private static void CreateToolActor(
            Vector3 position,
            out GameObject actor,
            out PlayerToolbelt toolbelt,
            out ToolController controller)
        {
            actor = new GameObject("Test Combat Actor");
            actor.transform.position = position;
            var cameraObject = new GameObject("Test Combat Camera");
            cameraObject.transform.SetParent(actor.transform, false);
            cameraObject.AddComponent<Camera>();
            toolbelt = actor.AddComponent<PlayerToolbelt>();
            actor.AddComponent<ContextInteractor>();
            controller = actor.AddComponent<ToolController>();
        }
    }
}

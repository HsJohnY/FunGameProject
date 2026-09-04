using System.Collections;
using FunGame.Combat;
using FunGame.Incident;
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
        public IEnumerator ToolController_冲击扳手两次重击基础敌人并完成遭遇()
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
            GameObject armorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            armorObject.name = "Enemy Child Armor";
            armorObject.transform.SetParent(enemyObject.transform, false);
            armorObject.transform.localPosition = new Vector3(0.35f, 0f, 0f);
            Collider armorCollider = armorObject.GetComponent<Collider>();
            Renderer armorRenderer = armorObject.GetComponent<Renderer>();
            enemy.SetEncounterActive(false);
            Assert.That(armorCollider.enabled, Is.False, "休眠波次不能留下子碰撞体");
            Assert.That(armorRenderer.enabled, Is.False, "休眠波次不能提前显示悬浮护甲零件");
            enemy.SetEncounterActive(true);
            Assert.That(armorCollider.enabled, Is.True);
            Assert.That(armorRenderer.enabled, Is.True);
            Physics.SyncTransforms();

            yield return null;

            for (int hitIndex = 0; hitIndex < 2; hitIndex++)
            {
                controller.RefreshTarget();
                Assert.That(controller.CurrentOption.HasValue, Is.True);
                Assert.That(controller.CurrentOption.Value.ActionLabel, Is.EqualTo("重击"));
                Assert.That(controller.ExecuteCurrentToolAction(), Is.True);
                yield return new WaitForSecondsRealtime(0.4f);
            }

            Assert.That(enemy.IsDefeated, Is.True);
            Assert.That(encounter.State, Is.EqualTo(CombatEncounterState.Succeeded));
            Assert.That(enemyObject.GetComponent<Collider>().enabled, Is.False, "被击败的干扰体应立即停止碰撞和攻击");
            Assert.That(armorCollider.enabled, Is.False, "外伸护甲和侧翼的子碰撞体也必须立即关闭，不能隐形阻挡后续交互");

            yield return new WaitForSecondsRealtime(0.5f);

            Assert.That(enemyObject.GetComponent<Renderer>().enabled, Is.False, "失效动画结束后不应留下黑色残骸");
            Assert.That(armorRenderer.enabled, Is.False, "失效动画结束后护甲、侧翼等子模型也必须隐藏");

            Object.Destroy(actor);
            Object.Destroy(enemyObject);
            Object.Destroy(targetObject);
            Object.Destroy(encounterObject);
        }

        [UnityTest]
        public IEnumerator InterferenceEnemy_密封喷枪范围喷覆会伤害减速并推离()
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
            Assert.That(controller.CurrentOption.Value.IsAvailable, Is.True);
            Assert.That(controller.CurrentOption.Value.ActionLabel, Is.EqualTo("范围喷覆"));
            Rigidbody enemyBody = enemyObject.GetComponent<Rigidbody>();
            float beforePositionZ = enemyBody.position.z;
            Assert.That(controller.ExecuteCurrentToolAction(), Is.True);
            Assert.That(enemy.Health, Is.EqualTo(enemy.MaxHealth - 1));
            Assert.That(enemy.IsSlowed, Is.True);
            Assert.That(enemyBody.position.z, Is.GreaterThan(beforePositionZ));

            Object.Destroy(actor);
            Object.Destroy(enemyObject);
            Object.Destroy(targetObject);
            Object.Destroy(encounterObject);
        }

        [UnityTest]
        public IEnumerator InterferenceEnemy_线路桥接器造成过载并中断攻击蓄力()
        {
            CreateEncounter(
                new Vector3(0f, 112f, 3f),
                new Vector3(0f, 112f, 2f),
                out GameObject encounterObject,
                out _,
                out GameObject targetObject,
                out _,
                out GameObject enemyObject,
                out InterferenceEnemy enemy,
                attackRange: 2f,
                knockbackDistance: 0f);
            CreateToolActor(new Vector3(0f, 112f, 0f), out GameObject actor, out PlayerToolbelt toolbelt, out ToolController controller);
            toolbelt.Equip(ToolKind.CircuitBridger);
            Physics.SyncTransforms();

            yield return null;
            controller.RefreshTarget();

            Assert.That(controller.CurrentOption.HasValue, Is.True);
            Assert.That(controller.CurrentOption.Value.ActionLabel, Is.EqualTo("电击瘫痪"));
            Assert.That(controller.ExecuteCurrentToolAction(), Is.True);
            Assert.That(enemy.IsStunned, Is.True);
            Assert.That(enemy.Health, Is.EqualTo(enemy.MaxHealth - 1));
            Assert.That(controller.CircuitBridgerCooldownRemaining, Is.GreaterThan(0f));

            Object.Destroy(actor);
            Object.Destroy(enemyObject);
            Object.Destroy(targetObject);
            Object.Destroy(encounterObject);
        }

        [UnityTest]
        public IEnumerator InterferenceEnemy_密封喷枪一次范围喷覆清除相邻虫群()
        {
            var encounterObject = new GameObject("Swarm Encounter");
            var encounter = encounterObject.AddComponent<CombatEncounterController>();
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.transform.position = new Vector3(0f, 114f, 6f);
            var target = targetObject.AddComponent<DefendableSystemTarget>();
            target.Configure(60);

            GameObject firstObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            firstObject.transform.position = new Vector3(-0.4f, 114f, 2f);
            var first = firstObject.AddComponent<InterferenceEnemy>();
            first.Configure(target, encounter, configuredMaxHealth: 1, configuredMoveSpeed: 0.1f);
            GameObject secondObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            secondObject.transform.position = new Vector3(0.5f, 114f, 2.2f);
            var second = secondObject.AddComponent<InterferenceEnemy>();
            second.Configure(target, encounter, configuredMaxHealth: 1, configuredMoveSpeed: 0.1f);
            encounter.Configure(target, new[] { first, second });

            var actor = new GameObject("Swarm Tool Actor");
            actor.transform.position = new Vector3(0f, 114f, 0f);
            var toolbelt = actor.AddComponent<PlayerToolbelt>();
            toolbelt.Equip(ToolKind.SealantGun);
            Physics.SyncTransforms();
            yield return null;

            Assert.That(first.ApplyTool(toolbelt), Is.True);
            Assert.That(first.IsDefeated, Is.True);
            Assert.That(second.IsDefeated, Is.True);
            Assert.That(encounter.State, Is.EqualTo(CombatEncounterState.Succeeded));

            Object.Destroy(actor);
            Object.Destroy(secondObject);
            Object.Destroy(firstObject);
            Object.Destroy(targetObject);
            Object.Destroy(encounterObject);
        }

        [UnityTest]
        public IEnumerator InterferenceEnemy_精英护盾要求桥接器先短路再接受重击()
        {
            CreateEncounter(
                new Vector3(0f, 116f, 6f),
                new Vector3(0f, 116f, 2f),
                out GameObject encounterObject,
                out CombatEncounterController encounter,
                out GameObject targetObject,
                out DefendableSystemTarget target,
                out GameObject enemyObject,
                out InterferenceEnemy enemy,
                knockbackDistance: 0f);
            enemy.Configure(
                target,
                encounter,
                configuredMaxHealth: 6,
                configuredMoveSpeed: 0.1f,
                configuredKnockbackDistance: 0f,
                configuredRequiresCircuitDisruption: true);
            var actor = new GameObject("Elite Tool Actor");
            var toolbelt = actor.AddComponent<PlayerToolbelt>();
            toolbelt.Equip(ToolKind.ImpactWrench);
            yield return null;

            ToolActionOption blocked = enemy.GetToolAction(toolbelt);
            Assert.That(blocked.IsAvailable, Is.False);
            Assert.That(blocked.BlockedReason, Is.EqualTo("需要线路桥接器"));
            Assert.That(enemy.ApplyTool(toolbelt), Is.False);

            toolbelt.Equip(ToolKind.CircuitBridger);
            Assert.That(enemy.ApplyTool(toolbelt), Is.True);
            Assert.That(enemy.IsStunned, Is.True);
            Assert.That(enemy.IsDisruptionShieldActive, Is.False);
            Assert.That(enemy.Health, Is.EqualTo(5));

            toolbelt.Equip(ToolKind.ImpactWrench);
            Assert.That(enemy.ApplyTool(toolbelt), Is.True);
            Assert.That(enemy.Health, Is.EqualTo(3));

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
            for (int hitIndex = 0; hitIndex < 2; hitIndex++)
            {
                controller.RefreshTarget();
                Assert.That(controller.CurrentOption.HasValue, Is.True, $"第 {hitIndex + 1} 次命中前敌人应保持可选中");
                Assert.That(controller.ExecuteCurrentToolAction(), Is.True);
                Physics.SyncTransforms();
                yield return new WaitForSecondsRealtime(0.4f);

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

            // 攻击蓄力和冷却跨多帧推进；低帧率时固定 120ms 可能只完成第一击。
            float deadline = Time.time + 1f;
            while (!target.IsOffline && Time.time < deadline) yield return null;

            Assert.That(target.IsOffline, Is.True);
            Assert.That(encounter.State, Is.EqualTo(CombatEncounterState.Failed));

            Object.Destroy(enemyObject);
            Object.Destroy(targetObject);
            Object.Destroy(encounterObject);
        }

        [UnityTest]
        public IEnumerator InterferenceEnemy_遇到实体障碍会绕行并攻击设备()
        {
            var encounterObject = new GameObject("Obstacle Avoidance Encounter");
            var encounter = encounterObject.AddComponent<CombatEncounterController>();
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.transform.position = new Vector3(2.5f, 125f, 0f);
            var target = targetObject.AddComponent<DefendableSystemTarget>();
            target.Configure(60);

            GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.transform.position = new Vector3(-2.5f, 125f, 0f);
            var enemy = enemyObject.AddComponent<InterferenceEnemy>();
            encounter.Configure(target, enemy);
            enemy.Configure(
                target,
                encounter,
                configuredMoveSpeed: 3f,
                configuredAttackRange: 1.3f,
                configuredAttackIntervalSeconds: 0.05f,
                configuredInterferenceDamage: 10,
                configuredKnockbackDistance: 0f,
                configuredAttackWindupSeconds: 0.02f);

            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Finite Navigation Obstacle";
            obstacle.transform.position = new Vector3(0f, 125f, 0f);
            obstacle.transform.localScale = new Vector3(0.35f, 2f, 2.2f);
            Physics.SyncTransforms();

            float timeout = Time.time + 4f;
            float maximumSideOffset = 0f;
            while (target.Integrity == target.MaxIntegrity && Time.time < timeout)
            {
                maximumSideOffset = Mathf.Max(maximumSideOffset, Mathf.Abs(enemyObject.transform.position.z));
                yield return null;
            }

            Assert.That(target.Integrity, Is.LessThan(target.MaxIntegrity), "干扰体应绕过有限障碍并攻击设备");
            Assert.That(maximumSideOffset, Is.GreaterThan(1f), "绕行时应从障碍侧面通过");

            Object.Destroy(obstacle);
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

            yield return new WaitForSeconds(0.06f);
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

        [UnityTest]
        public IEnumerator ToolController_扳手冷却会阻止同帧重复命中()
        {
            CreateEncounter(
                new Vector3(0f, 140f, 6f),
                new Vector3(0f, 140f, 2f),
                out GameObject encounterObject,
                out _,
                out GameObject targetObject,
                out _,
                out GameObject enemyObject,
                out InterferenceEnemy enemy,
                knockbackDistance: 0f);
            CreateToolActor(new Vector3(0f, 140f, 0f), out GameObject actor, out PlayerToolbelt toolbelt, out ToolController controller);
            toolbelt.Equip(ToolKind.ImpactWrench);
            Physics.SyncTransforms();
            yield return null;

            controller.RefreshTarget();
            Assert.That(controller.ExecuteCurrentToolAction(), Is.True);
            int healthAfterFirstHit = enemy.Health;
            controller.RefreshTarget();
            Assert.That(controller.ExecuteCurrentToolAction(), Is.False);
            Assert.That(enemy.Health, Is.EqualTo(healthAfterFirstHit));
            Assert.That(controller.ImpactWrenchCooldownRemaining, Is.GreaterThan(0f));

            Object.Destroy(actor);
            Object.Destroy(enemyObject);
            Object.Destroy(targetObject);
            Object.Destroy(encounterObject);
        }

        [UnityTest]
        public IEnumerator CombatEncounter_双敌人需要全部清除才成功()
        {
            var encounterObject = new GameObject("Multi Enemy Encounter");
            var encounter = encounterObject.AddComponent<CombatEncounterController>();
            GameObject targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.transform.position = new Vector3(0f, 150f, 8f);
            var target = targetObject.AddComponent<DefendableSystemTarget>();
            target.Configure(60);
            GameObject firstObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            firstObject.transform.position = new Vector3(-1f, 150f, 4f);
            var first = firstObject.AddComponent<InterferenceEnemy>();
            GameObject secondObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            secondObject.transform.position = new Vector3(1f, 150f, 4f);
            var second = secondObject.AddComponent<InterferenceEnemy>();
            first.Configure(target, encounter, configuredKnockbackDistance: 0f);
            second.Configure(target, encounter, configuredKnockbackDistance: 0f, configuredBehavior: InterferenceEnemyBehavior.FlankingAttach);
            encounter.Configure(target, new[] { first, second });
            var actor = new GameObject("Multi Enemy Tool Actor");
            var toolbelt = actor.AddComponent<PlayerToolbelt>();
            toolbelt.Equip(ToolKind.ImpactWrench);
            yield return null;

            for (int index = 0; index < 2; index++)
            {
                Assert.That(first.ApplyTool(toolbelt), Is.True);
            }

            Assert.That(encounter.State, Is.EqualTo(CombatEncounterState.Active));
            Assert.That(encounter.RemainingEnemyCount, Is.EqualTo(1));
            for (int index = 0; index < 2; index++)
            {
                Assert.That(second.ApplyTool(toolbelt), Is.True);
            }

            Assert.That(encounter.State, Is.EqualTo(CombatEncounterState.Succeeded));

            Object.Destroy(actor);
            Object.Destroy(secondObject);
            Object.Destroy(firstObject);
            Object.Destroy(targetObject);
            Object.Destroy(encounterObject);
        }

        [UnityTest]
        public IEnumerator CoolingCombatIntegration_密封完成触发防卫且设备受击升温()
        {
            var incidentObject = new GameObject("Integrated Incident");
            var incident = incidentObject.AddComponent<CoolingIncidentController>();
            incident.ConfigureTemperature(65f, 100f, 0f);
            CreateEncounter(
                new Vector3(0f, 160f, 4f),
                new Vector3(0f, 160f, 0f),
                out GameObject encounterObject,
                out CombatEncounterController encounter,
                out GameObject targetObject,
                out DefendableSystemTarget target,
                out GameObject enemyObject,
                out InterferenceEnemy enemy,
                attackRange: 0.1f);
            var integration = encounterObject.AddComponent<CoolingCombatIntegrationController>();
            integration.Configure(incident, encounter, target, 3f);
            yield return null;

            Assert.That(encounter.State, Is.EqualTo(CombatEncounterState.Dormant));
            Assert.That(enemy.IsEncounterActive, Is.False);
            incident.AddSealProgress(1f);
            Assert.That(integration.HasTriggered, Is.True);
            Assert.That(encounter.State, Is.EqualTo(CombatEncounterState.Active));
            float before = incident.Temperature;
            target.ApplyInterference(10);
            Assert.That(incident.Temperature, Is.EqualTo(before + 3f).Within(0.001f));

            incident.ResetIncident();
            Assert.That(integration.HasTriggered, Is.False);
            Assert.That(encounter.State, Is.EqualTo(CombatEncounterState.Dormant));
            Assert.That(enemy.IsEncounterActive, Is.False);

            Object.Destroy(enemyObject);
            Object.Destroy(targetObject);
            Object.Destroy(encounterObject);
            Object.Destroy(incidentObject);
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
                configuredWrenchDamage: 2,
                configuredKnockbackDistance: knockbackDistance,
                configuredAttackWindupSeconds: 0.02f);
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

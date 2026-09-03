using System.Collections;
using System.Reflection;
using FunGame.Interaction;
using FunGame.Incident;
using FunGame.Tools;
using FunGame.Demo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class ToolAndCarryPlayModeTests
    {
        [UnityTest]
        public IEnumerator MechanicalFastener_场景加载后连续三次重置无残留()
        {
            var incidentObject = new GameObject("Test Runtime Registration Incident");
            var incident = incidentObject.AddComponent<CoolingIncidentController>();
            incident.ConfigureTemperature(65f, 100f, 0f);

            var targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var target = targetObject.AddComponent<MechanicalFastenerTarget>();
            SetPrivateField(target, "incident", incident);

            CreateToolActor(out GameObject actor, out PlayerToolbelt toolbelt, out _);
            toolbelt.Equip(ToolKind.ImpactWrench);

            // 等待 Start，模拟由 Unity 场景反序列化引用、而不是由 Configure 注入的真实加载路径。
            yield return null;

            for (int resetIndex = 1; resetIndex <= 3; resetIndex++)
            {
                incident.AddSealProgress(1f);
                Assert.That(target.ApplyTool(toolbelt), Is.True);
                Assert.That(target.IsTightened, Is.False);

                incident.ResetIncident();
                Assert.That(target.IsTightened, Is.True);
                Assert.That(incident.Phase, Is.EqualTo(CoolingIncidentPhase.ContainLeak));
                Assert.That(incident.ResetCount, Is.EqualTo(resetIndex));
            }

            Object.Destroy(actor);
            Object.Destroy(targetObject);
            Object.Destroy(incidentObject);
        }

        [UnityTest]
        public IEnumerator CoolingIncident_温度超限后失败并可重置()
        {
            var incidentObject = new GameObject("Test Cooling Incident");
            var incident = incidentObject.AddComponent<CoolingIncidentController>();
            incident.ConfigureTemperature(1f, 1.01f, 100f);

            yield return null;

            Assert.That(incident.RunState, Is.EqualTo(CoolingIncidentRunState.Failed));
            Assert.That(incident.ResetIncident(), Is.True);
            Assert.That(incident.RunState, Is.EqualTo(CoolingIncidentRunState.Active));
            Assert.That(incident.Phase, Is.EqualTo(CoolingIncidentPhase.ContainLeak));
            Assert.That(incident.ResetCount, Is.EqualTo(1));

            Object.Destroy(incidentObject);
        }

        [UnityTest]
        public IEnumerator CoolingIncident_完成固定阶段后标记成功()
        {
            var incidentObject = new GameObject("Test Cooling Incident Success");
            var incident = incidentObject.AddComponent<CoolingIncidentController>();
            incident.ConfigureTemperature(65f, 100f, 0f);

            yield return null;

            incident.AddSealProgress(1f);
            incident.TryLoosen();
            incident.TryInstallPipe();
            incident.TryTighten();
            incident.TryResetPump();

            Assert.That(incident.RunState, Is.EqualTo(CoolingIncidentRunState.Succeeded));
            Assert.That(incident.Phase, Is.EqualTo(CoolingIncidentPhase.Stabilized));

            Object.Destroy(incidentObject);
        }

        [UnityTest]
        public IEnumerator CoolingIncident_扩展诊断验证与结算指标形成完整闭环()
        {
            var incidentObject = new GameObject("Test Extended Cooling Incident");
            var incident = incidentObject.AddComponent<CoolingIncidentController>();
            incident.ConfigureExtendedIncident(true);
            incident.ConfigureTemperature(65f, 100f, 0f);

            yield return null;

            Assert.That(incident.Phase, Is.EqualTo(CoolingIncidentPhase.AssessSymptoms));
            Assert.That(incident.TryInspectPressure(), Is.True);
            Assert.That(incident.TryInspectPump(), Is.True);
            Assert.That(incident.Phase, Is.EqualTo(CoolingIncidentPhase.RestoreControlPower));
            Assert.That(incident.TryAdvanceCircuitBridge(), Is.True);
            Assert.That(incident.TryAdvanceCircuitBridge(), Is.True);
            Assert.That(incident.TryAdvanceCircuitBridge(), Is.True);
            incident.RecordRejectedAction("tool:test", "需要密封喷枪");
            incident.AddSealProgress(1f);
            incident.TryLoosen();
            incident.TryInstallPipe();
            incident.TryTighten();
            Assert.That(incident.TryResetPump(), Is.False);
            Assert.That(incident.TryInspectPressure(), Is.True);
            Assert.That(incident.TryResetPump(), Is.True);

            Assert.That(incident.RunState, Is.EqualTo(CoolingIncidentRunState.Succeeded));
            Assert.That(incident.RejectedActionCount, Is.EqualTo(1));

            incident.ResetIncident();
            Assert.That(incident.Phase, Is.EqualTo(CoolingIncidentPhase.AssessSymptoms));
            Assert.That(incident.RejectedActionCount, Is.Zero);

            Object.Destroy(incidentObject);
        }

        [UnityTest]
        public IEnumerator ControlledLayout_每次重置轮换位置并找回手持任务物()
        {
            var incidentObject = new GameObject("Layout Incident");
            var incident = incidentObject.AddComponent<CoolingIncidentController>();
            var leak = new GameObject("Layout Leak");
            var repair = new GameObject("Layout Repair");
            var recovery = new GameObject("Layout Recovery");
            var itemObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemObject.AddComponent<Rigidbody>();
            var item = itemObject.AddComponent<CarryableInteractable>();
            ContextInteractor interactor = CreateCarryActor(out GameObject actor);
            var layoutObject = new GameObject("Layout Controller");
            var layout = layoutObject.AddComponent<CoolingIncidentLayoutController>();
            layout.Configure(
                incident,
                leak.transform,
                repair.transform,
                recovery.transform,
                item,
                new[] { Vector3.zero, Vector3.right },
                new[] { Vector3.forward, Vector3.forward * 2f },
                new[] { Vector3.left, Vector3.left * 2f });
            layout.ConfigurePlayer(interactor);

            yield return null;
            Assert.That(interactor.TryPickup(item), Is.True);
            Assert.That(interactor.IsHoldingItem, Is.True);

            incident.ResetIncident();

            Assert.That(layout.CurrentLayoutIndex, Is.EqualTo(1));
            Assert.That(leak.transform.position, Is.EqualTo(Vector3.right));
            Assert.That(item.transform.position, Is.EqualTo(Vector3.left * 2f));
            Assert.That(interactor.IsHoldingItem, Is.False);

            Object.Destroy(layoutObject);
            Object.Destroy(actor);
            Object.Destroy(itemObject);
            Object.Destroy(recovery);
            Object.Destroy(repair);
            Object.Destroy(leak);
            Object.Destroy(incidentObject);
        }

        [UnityTest]
        public IEnumerator ToolRack_重复取用同类工具会恢复空手()
        {
            CreateToolActor(out GameObject actor, out PlayerToolbelt toolbelt, out _);
            var rackObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var rack = rackObject.AddComponent<ToolRackInteractable>();
            rack.Configure("test-rack", ToolKind.ImpactWrench);

            yield return null;
            Assert.That(rack.ExecuteInteraction(actor.GetComponent<ContextInteractor>()), Is.True);
            Assert.That(toolbelt.EquippedTool, Is.EqualTo(ToolKind.ImpactWrench));
            Assert.That(rack.GetInteractionOption(actor.GetComponent<ContextInteractor>()).ActionLabel, Is.EqualTo("放回冲击扳手"));
            Assert.That(rack.ExecuteInteraction(actor.GetComponent<ContextInteractor>()), Is.True);
            Assert.That(toolbelt.EquippedTool, Is.EqualTo(ToolKind.None));

            Object.Destroy(actor);
            Object.Destroy(rackObject);
        }

        [UnityTest]
        public IEnumerator ToolController_正确扳手切换机械连接状态()
        {
            CreateToolActor(out GameObject actor, out PlayerToolbelt toolbelt, out ToolController controller);
            actor.transform.position = new Vector3(0f, 100f, 0f);
            toolbelt.Equip(ToolKind.ImpactWrench);

            var targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.transform.position = new Vector3(0f, 100f, 2f);
            var target = targetObject.AddComponent<MechanicalFastenerTarget>();
            Physics.SyncTransforms();

            yield return null;
            controller.RefreshTarget();

            Assert.That(controller.CurrentOption.HasValue, Is.True);
            Assert.That(controller.CurrentOption.Value.ActionLabel, Is.EqualTo("松开"));
            Assert.That(controller.ExecuteCurrentToolAction(), Is.True);
            Assert.That(target.IsTightened, Is.False);

            Object.Destroy(actor);
            Object.Destroy(targetObject);
        }

        [UnityTest]
        public IEnumerator CircuitBridgeTarget_三次离散操作恢复控制联锁()
        {
            var incidentObject = new GameObject("Circuit Incident");
            var incident = incidentObject.AddComponent<CoolingIncidentController>();
            incident.ConfigureExtendedIncident(true);
            incident.TryInspectPressure();
            incident.TryInspectPump();
            CreateToolActor(out GameObject actor, out PlayerToolbelt toolbelt, out _);
            toolbelt.Equip(ToolKind.CircuitBridger);
            var targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var target = targetObject.AddComponent<CircuitBridgeTarget>();
            target.Configure(incident);

            yield return null;

            for (int step = 1; step <= CoolingIncidentRules.RequiredCircuitBridgeSteps; step++)
            {
                Assert.That(target.ApplyTool(toolbelt), Is.True);
                Assert.That(target.CompletedSteps, Is.EqualTo(step));
            }

            Assert.That(target.IsBridged, Is.True);
            Assert.That(incident.Phase, Is.EqualTo(CoolingIncidentPhase.ContainLeak));

            incident.ResetIncident();
            Assert.That(target.CompletedSteps, Is.Zero);

            Object.Destroy(targetObject);
            Object.Destroy(actor);
            Object.Destroy(incidentObject);
        }

        [UnityTest]
        public IEnumerator DemoRelayTarget_章节启用后复用桥接器完成三步稳定()
        {
            CreateToolActor(out GameObject actor, out PlayerToolbelt toolbelt, out _);
            toolbelt.Equip(ToolKind.CircuitBridger);
            var relayObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var relay = relayObject.AddComponent<DemoRelayTarget>();
            relay.Configure("test-relay", "测试继电器");
            relay.SetChapterActive(true, true);
            int stabilizedEvents = 0;
            relay.Stabilized += _ => stabilizedEvents++;

            yield return null;

            Assert.That(relay.ApplyTool(toolbelt), Is.True);
            Assert.That(relay.ApplyTool(toolbelt), Is.True);
            Assert.That(relay.ApplyTool(toolbelt), Is.True);
            Assert.That(relay.IsStabilized, Is.True);
            Assert.That(stabilizedEvents, Is.EqualTo(1));
            Assert.That(relay.ApplyTool(toolbelt), Is.False);

            Object.Destroy(relayObject);
            Object.Destroy(actor);
        }

        [UnityTest]
        public IEnumerator ToolController_错误工具不会改变机械连接()
        {
            CreateToolActor(out GameObject actor, out PlayerToolbelt toolbelt, out ToolController controller);
            actor.transform.position = new Vector3(0f, 110f, 0f);
            toolbelt.Equip(ToolKind.SealantGun);

            var targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.transform.position = new Vector3(0f, 110f, 2f);
            var target = targetObject.AddComponent<MechanicalFastenerTarget>();
            Physics.SyncTransforms();

            yield return null;
            controller.RefreshTarget();

            Assert.That(controller.CurrentOption.Value.IsAvailable, Is.False);
            Assert.That(controller.CurrentOption.Value.BlockedReason, Is.EqualTo("需要冲击扳手"));
            Assert.That(controller.ExecuteCurrentToolAction(), Is.False);
            Assert.That(target.IsTightened, Is.True);

            Object.Destroy(actor);
            Object.Destroy(targetObject);
        }

        [UnityTest]
        public IEnumerator ContextInteractor_抛放后刚体获得向前和向上速度()
        {
            ContextInteractor interactor = CreateCarryActor(out GameObject actor);
            var itemObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var body = itemObject.AddComponent<Rigidbody>();
            var item = itemObject.AddComponent<CarryableInteractable>();

            yield return null;
            Assert.That(interactor.TryPickup(item), Is.True);
            Assert.That(interactor.DropHeldItem(), Is.True);

            // Rigidbody 从运动学状态恢复后，等待一次物理步再检查冲量结果。
            yield return new WaitForFixedUpdate();

            Assert.That(body.linearVelocity.z, Is.GreaterThan(0f));
            Assert.That(body.linearVelocity.y, Is.GreaterThan(0f));

            Object.Destroy(actor);
            Object.Destroy(itemObject);
        }

        [UnityTest]
        public IEnumerator Carryable_手持时缩小且抛出后恢复原尺寸()
        {
            ContextInteractor interactor = CreateCarryActor(out GameObject actor);
            var itemObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemObject.transform.localScale = new Vector3(0.6f, 0.6f, 1.5f);
            itemObject.AddComponent<Rigidbody>();
            var item = itemObject.AddComponent<CarryableInteractable>();
            Vector3 originalScale = itemObject.transform.lossyScale;

            yield return null;
            Assert.That(interactor.TryPickup(item), Is.True);
            Assert.That(itemObject.transform.lossyScale.magnitude, Is.LessThan(originalScale.magnitude));
            Assert.That(interactor.DropHeldItem(), Is.True);
            Assert.That(Vector3.Distance(itemObject.transform.lossyScale, originalScale), Is.LessThan(0.001f));

            Object.Destroy(actor);
            Object.Destroy(itemObject);
        }

        [UnityTest]
        public IEnumerator TaskItemRecovery_越过高度边界后返回恢复点()
        {
            var recoveryPoint = new GameObject("Test Recovery Point");
            recoveryPoint.transform.position = new Vector3(2f, 1f, 3f);

            var itemObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemObject.AddComponent<Rigidbody>();
            itemObject.AddComponent<CarryableInteractable>();
            var recovery = itemObject.AddComponent<TaskItemRecovery>();
            recovery.Configure(recoveryPoint.transform, -3f);
            itemObject.transform.position = new Vector3(0f, -4f, 0f);

            yield return null;

            Assert.That(recovery.RecoveryCount, Is.EqualTo(1));
            Assert.That(itemObject.transform.position, Is.EqualTo(recoveryPoint.transform.position));

            Object.Destroy(itemObject);
            Object.Destroy(recoveryPoint);
        }

        private static void CreateToolActor(
            out GameObject actor,
            out PlayerToolbelt toolbelt,
            out ToolController controller)
        {
            actor = new GameObject("Test Tool Actor");
            var cameraObject = new GameObject("Test Tool Camera");
            cameraObject.transform.SetParent(actor.transform, false);
            cameraObject.AddComponent<Camera>();
            toolbelt = actor.AddComponent<PlayerToolbelt>();
            actor.AddComponent<ContextInteractor>();
            controller = actor.AddComponent<ToolController>();
        }

        private static ContextInteractor CreateCarryActor(out GameObject actor)
        {
            actor = new GameObject("Test Carry Actor");
            var cameraObject = new GameObject("Test Carry Camera");
            cameraObject.transform.SetParent(actor.transform, false);
            cameraObject.AddComponent<Camera>();
            return actor.AddComponent<ContextInteractor>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"找不到测试字段：{fieldName}");
            field.SetValue(target, value);
        }
    }
}

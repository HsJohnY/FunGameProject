using System.Collections;
using FunGame.Interaction;
using FunGame.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class ToolAndCarryPlayModeTests
    {
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
    }
}

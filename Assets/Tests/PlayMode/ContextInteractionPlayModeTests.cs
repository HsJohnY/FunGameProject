using System.Collections;
using FunGame.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class ContextInteractionPlayModeTests
    {
        [UnityTest]
        public IEnumerator ContextInteractor_只执行准星命中的控制台动作()
        {
            ContextInteractor interactor = CreateInteractor();
            var consoleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            consoleObject.transform.position = new Vector3(0f, 0f, 2f);
            var console = consoleObject.AddComponent<ToggleConsoleInteractable>();

            yield return new WaitForFixedUpdate();
            interactor.RefreshTarget();

            Assert.That(interactor.CurrentOption.HasValue, Is.True);
            Assert.That(interactor.CurrentOption.Value.TargetId, Is.EqualTo("cooling-console"));
            Assert.That(interactor.ExecuteCurrentInteraction(), Is.True);
            Assert.That(console.IsOn, Is.True);

            Object.Destroy(interactor.gameObject);
            Object.Destroy(consoleObject);
        }

        [UnityTest]
        public IEnumerator ContextInteractor_拾取和丢下只占用一个手持位()
        {
            ContextInteractor interactor = CreateInteractor();
            var itemObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemObject.transform.position = new Vector3(0f, 0f, 2f);
            itemObject.AddComponent<Rigidbody>();
            var item = itemObject.AddComponent<CarryableInteractable>();

            yield return null;

            Assert.That(interactor.TryPickup(item), Is.True);
            Assert.That(interactor.IsHoldingItem, Is.True);
            Assert.That(item.IsHeld, Is.True);
            Assert.That(interactor.DropHeldItem(), Is.True);
            Assert.That(interactor.IsHoldingItem, Is.False);
            Assert.That(item.IsHeld, Is.False);

            Object.Destroy(interactor.gameObject);
            Object.Destroy(itemObject);
        }

        [UnityTest]
        public IEnumerator ContextInteractor_轻微偏离中心仍能选中小目标()
        {
            ContextInteractor interactor = CreateInteractor();
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "Small Assisted Target";
            target.transform.position = new Vector3(0.12f, 0f, 2f);
            target.transform.localScale = Vector3.one * 0.08f;
            target.AddComponent<ToggleConsoleInteractable>();
            Physics.SyncTransforms();

            yield return null;
            interactor.RefreshTarget();

            Assert.That(interactor.CurrentOption.HasValue, Is.True);
            Assert.That(interactor.CurrentOption.Value.TargetId, Is.EqualTo("cooling-console"));

            Object.Destroy(interactor.gameObject);
            Object.Destroy(target);
        }

        [UnityTest]
        public IEnumerator ContextInteractor_非交互遮挡物阻止选择后方目标()
        {
            ContextInteractor interactor = CreateInteractor();
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "Blocking Surface";
            blocker.transform.position = new Vector3(0f, 0f, 1f);

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.transform.position = new Vector3(0f, 0f, 2f);
            target.AddComponent<ToggleConsoleInteractable>();
            Physics.SyncTransforms();

            yield return null;
            interactor.RefreshTarget();

            Assert.That(interactor.CurrentOption.HasValue, Is.False);

            Object.Destroy(interactor.gameObject);
            Object.Destroy(blocker);
            Object.Destroy(target);
        }

        private static ContextInteractor CreateInteractor()
        {
            var actor = new GameObject("Test Interaction Actor");
            var cameraObject = new GameObject("Test Interaction Camera");
            cameraObject.transform.SetParent(actor.transform, false);
            cameraObject.AddComponent<Camera>();
            return actor.AddComponent<ContextInteractor>();
        }
    }
}

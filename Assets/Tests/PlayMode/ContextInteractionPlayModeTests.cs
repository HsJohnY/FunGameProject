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

using System.Collections;
using FunGame.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;

namespace FunGame.Tests.PlayMode
{
    public sealed class FirstPersonControllerPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator DisabledGameplayInputBlocksRawWasdTapCompensation()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var player = new GameObject("Chat Input Regression");
            var camera = new GameObject("Camera");
            camera.transform.SetParent(player.transform);
            camera.AddComponent<Camera>();
            var controller = player.AddComponent<FirstPersonController>();
            yield return null;
            try
            {
                controller.SetGameplayInputEnabled(false);
                Vector3 before = player.transform.position;
                PressAndRelease(keyboard.wKey);
                yield return null;
                Assert.That(player.transform.position, Is.EqualTo(before));
                controller.SetGameplayInputEnabled(true);
                PressAndRelease(keyboard.wKey);
                yield return null;
                Assert.That(player.transform.position.z - before.z, Is.GreaterThan(0.3f), "The same input moves the player after chat closes.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                Object.Destroy(player);
            }
        }

        [UnityTest]
        public IEnumerator FirstPersonController_启用后创建并开启输入动作()
        {
            var player = new GameObject("Test First Person Player");
            player.AddComponent<CharacterController>();

            var cameraObject = new GameObject("Test Camera");
            cameraObject.transform.SetParent(player.transform);
            cameraObject.AddComponent<Camera>();

            var controller = player.AddComponent<FirstPersonController>();
            yield return null;

            Assert.That(controller.enabled, Is.True);
            Assert.That(controller.IsInputEnabled, Is.True);

            Object.Destroy(player);
        }
    }
}

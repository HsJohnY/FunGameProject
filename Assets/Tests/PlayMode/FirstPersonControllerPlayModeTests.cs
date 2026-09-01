using System.Collections;
using FunGame.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class FirstPersonControllerPlayModeTests
    {
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

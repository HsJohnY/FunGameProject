using System.Collections;
using FunGame.Networking;
using FunGame.Player;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class NetworkPlayerOwnershipPlayModeTests
    {
        [UnityTest]
        public IEnumerator ApplyOwnershipPresentation_只为拥有者启用输入和镜头()
        {
            var player = new GameObject("Network Player Ownership Test");
            player.SetActive(false);
            player.AddComponent<NetworkObject>();
            player.AddComponent<CharacterController>();

            var cameraObject = new GameObject("View Camera");
            cameraObject.transform.SetParent(player.transform);
            Camera viewCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();

            FirstPersonController firstPersonController = player.AddComponent<FirstPersonController>();
            NetworkPlayerController networkPlayer = player.AddComponent<NetworkPlayerController>();
            player.SetActive(true);

            networkPlayer.ApplyOwnershipPresentation(false);
            Assert.That(firstPersonController.enabled, Is.False);
            Assert.That(viewCamera.enabled, Is.False);

            networkPlayer.ApplyOwnershipPresentation(true);
            Assert.That(firstPersonController.enabled, Is.True);
            Assert.That(viewCamera.enabled, Is.True);

            Object.Destroy(player);
            yield return null;
        }
    }
}

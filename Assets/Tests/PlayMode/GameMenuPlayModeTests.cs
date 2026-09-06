using System.Collections;
using FunGame.Player;
using FunGame.UI;
using FunGame.Networking;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class GameMenuPlayModeTests
    {
        [UnityTest]
        public IEnumerator NetworkLobby_HostDisconnectAndFailedJoinKeepControlsAvailable()
        {
            PlayerPrefs.DeleteKey("FunGame.Menu.OpenAsMain");
            FunGame.Demo.SharedMapModeController.NextMode = FunGame.Demo.ExpeditionMode.Cooperative;
            yield return SceneManager.LoadSceneAsync(GameMenuController.CooperativeScene, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName(GameMenuController.CooperativeScene);
            yield return ModularSceneTestUtility.WaitUntilReady(scene);
            var menu = Object.FindFirstObjectByType<GameMenuController>();
            var session = Object.FindFirstObjectByType<NetworkSessionController>();
            var manager = Object.FindFirstObjectByType<NetworkManager>();
            var transport = manager.GetComponent<UnityTransport>();
            try
            {
                menu.OpenNetworkLobby();
                yield return null;
                Assert.That(menu.IsNetworkLobbyOpen, Is.True);
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                Assert.That(Time.timeScale, Is.EqualTo(1f));
                Assert.That(menu.StartRoom(false, "invalid", "17843"), Is.False);
                Assert.That(session.IsEndpointEditable, Is.True);
                Assert.That(session.StatusText, Does.Contain("IPv4"));
                Assert.That(menu.StartRoom(true, "这不是主机需要填写的地址", "17843"), Is.True,
                    "创建房间应忽略地址栏，只使用端口并监听全部 IPv4 网卡");
                Assert.That(transport.ConnectionData.Address, Is.EqualTo(NetworkEndpointRules.DefaultAddress));
                Assert.That(transport.ConnectionData.ServerListenAddress,
                    Is.EqualTo(NetworkEndpointRules.AnyIpv4Address), "主机必须监听真实和虚拟局域网网卡");
                Assert.That(transport.ConnectionData.Port, Is.EqualTo(17843));
                float deadline = Time.realtimeSinceStartup + 5f;
                while ((!session.HasLocalPlayer || menu.IsMenuOpen) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(session.HasLocalPlayer, Is.True);
                Assert.That(menu.IsMenuOpen, Is.False, "房主创建成功后应直接进入游戏");
                var owner = manager.LocalClient.PlayerObject.GetComponent<FirstPersonController>();
                yield return null;
                Assert.That(owner.IsInputEnabled, Is.True);
                menu.OpenNetworkLobby();
                yield return null;
                Assert.That(owner.IsInputEnabled, Is.False);
                Assert.That(owner.IsCursorLocked, Is.False);
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                Assert.That(session.ConnectedPlayerCount, Is.EqualTo(1));
                menu.DisconnectRoom();
                deadline = Time.realtimeSinceStartup + 5f;
                while (!session.IsEndpointEditable && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(session.IsEndpointEditable, Is.True);
                Assert.That(menu.IsNetworkLobbyOpen, Is.True);
                LogAssert.Expect(LogType.Error, "Failed to connect to server.");
                Assert.That(menu.StartRoom(false, "127.0.0.1", "17843"), Is.True);
                deadline = Time.realtimeSinceStartup + 15f;
                while (!session.IsEndpointEditable && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(session.IsEndpointEditable, Is.True, session.StatusText);
                Assert.That(menu.IsNetworkLobbyOpen, Is.True);
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                Assert.That(menu.StartRoom(true, "", "17843"), Is.True, "Can retry after a failed join");
                deadline = Time.realtimeSinceStartup + 5f;
                while ((!session.HasLocalPlayer || menu.IsMenuOpen) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(menu.IsMenuOpen, Is.False);
                Assert.That(session.HasLocalPlayer, Is.True);
            }
            finally
            {
                manager.Shutdown();
                Object.Destroy(manager.gameObject);
                if (scene.IsValid()) SceneManager.UnloadSceneAsync(scene);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenu_StartsPausedAndReleasesGameplayInput()
        {
            PlayerPrefs.DeleteKey("FunGame.Menu.OpenAsMain");
            var playerObject = new GameObject("Menu Test Player");
            playerObject.AddComponent<CharacterController>();
            var cameraObject = new GameObject("Menu Test Camera");
            cameraObject.transform.SetParent(playerObject.transform);
            cameraObject.AddComponent<Camera>();
            var player = playerObject.AddComponent<FirstPersonController>();

            var menuObject = new GameObject("Menu Under Test");
            var menu = menuObject.AddComponent<GameMenuController>();
            yield return null;

            Assert.That(menu.IsMenuOpen, Is.True);
            Assert.That(GameMenuController.IsAnyMenuOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(player.IsInputEnabled, Is.False);
            Assert.That(player.IsCursorLocked, Is.False);

            menu.OpenSettingsForAutomation();
            Assert.That(menu.IsMenuOpen, Is.True);
            Assert.That(GameMenuController.IsAnyMenuOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);

            Object.Destroy(menuObject);
            Object.Destroy(playerObject);
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }
    }
}

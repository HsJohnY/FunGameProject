using System.Collections;
using FunGame.Player;
using FunGame.UI;
using FunGame.Networking;
using Unity.Netcode;
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
            var menu = Object.FindFirstObjectByType<GameMenuController>();
            var session = Object.FindFirstObjectByType<NetworkSessionController>();
            var manager = Object.FindFirstObjectByType<NetworkManager>();
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
                Assert.That(menu.StartRoom(true, "", "17843"), Is.True);
                float deadline = Time.realtimeSinceStartup + 5f;
                while (menu.IsMenuOpen && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(session.HasLocalPlayer, Is.True);
                Assert.That(menu.IsMenuOpen, Is.False);
                var owner = manager.LocalClient.PlayerObject.GetComponent<FirstPersonController>();
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
                deadline = Time.realtimeSinceStartup + 8f;
                while (!session.IsEndpointEditable && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(session.IsEndpointEditable, Is.True, session.StatusText);
                Assert.That(menu.IsNetworkLobbyOpen, Is.True);
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                Assert.That(menu.StartRoom(true, "", "17843"), Is.True, "Can retry after a failed join");
                deadline = Time.realtimeSinceStartup + 5f;
                while (menu.IsMenuOpen && Time.realtimeSinceStartup < deadline) yield return null;
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

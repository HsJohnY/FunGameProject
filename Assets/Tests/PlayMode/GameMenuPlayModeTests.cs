using System.Collections;
using FunGame.Player;
using FunGame.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class GameMenuPlayModeTests
    {
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

            Object.Destroy(menuObject);
            Object.Destroy(playerObject);
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }
    }
}

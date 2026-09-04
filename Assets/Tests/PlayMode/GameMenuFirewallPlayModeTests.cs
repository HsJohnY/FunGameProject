using System.Collections;
using System.Threading.Tasks;
using FunGame.Networking;
using FunGame.UI;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class GameMenuFirewallPlayModeTests
    {
        private sealed class Access : IHostFirewallAccess
        {
            public Task<HostFirewallResult> Check = Task.FromResult(HostFirewallResult.Missing);
            public HostFirewallResult Configure = HostFirewallResult.Cancelled;
            public int Configurations;
            public Task<HostFirewallResult> CheckAsync(ushort port) => Check;
            public Task<HostFirewallResult> ConfigureAsync(ushort port)
            {
                Configurations++;
                return Task.FromResult(Configure);
            }
        }

        [UnityTest]
        public IEnumerator HostWaitsForConsent_CancelCanRetry_ThenEntersGame()
        {
            PlayerPrefs.DeleteKey("FunGame.Menu.OpenAsMain");
            FunGame.Demo.SharedMapModeController.NextMode = FunGame.Demo.ExpeditionMode.Cooperative;
            yield return SceneManager.LoadSceneAsync(GameMenuController.CooperativeScene, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName(GameMenuController.CooperativeScene);
            yield return ModularSceneTestUtility.WaitUntilReady(scene);
            var menu = Object.FindFirstObjectByType<GameMenuController>();
            var manager = Object.FindFirstObjectByType<NetworkManager>();
            var session = Object.FindFirstObjectByType<NetworkSessionController>();
            var access = new Access();
            try
            {
                menu.ConfigureHostFirewallAccess(access);
                menu.OpenNetworkLobby();
                menu.RequestRoomFromMenu(true, "", "17845");
                yield return null;
                Assert.That(menu.FirewallState, Is.EqualTo(HostFirewallState.NeedsConsent));
                Assert.That(manager.IsListening, Is.False);
                Assert.That(access.Configurations, Is.Zero);
                menu.AuthorizeHostFirewall();
                yield return null;
                Assert.That(menu.FirewallState, Is.EqualTo(HostFirewallState.Failed));
                Assert.That(menu.IsNetworkLobbyOpen, Is.True);
                Assert.That(manager.IsListening, Is.False);
                access.Configure = HostFirewallResult.Allowed;
                menu.RequestRoomFromMenu(true, "", "17845");
                menu.AuthorizeHostFirewall();
                float deadline = Time.realtimeSinceStartup + 5f;
                while ((menu.IsMenuOpen || !session.HasLocalPlayer) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(session.IsHost, Is.True);
                Assert.That(session.HasLocalPlayer, Is.True);
                Assert.That(menu.IsMenuOpen, Is.False);
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
        public IEnumerator LeavingLobbyDuringCheck_DoesNotStartHostWhenCheckCompletes()
        {
            PlayerPrefs.DeleteKey("FunGame.Menu.OpenAsMain");
            FunGame.Demo.SharedMapModeController.NextMode = FunGame.Demo.ExpeditionMode.Cooperative;
            yield return SceneManager.LoadSceneAsync(GameMenuController.CooperativeScene, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName(GameMenuController.CooperativeScene);
            yield return ModularSceneTestUtility.WaitUntilReady(scene);
            var menu = Object.FindFirstObjectByType<GameMenuController>();
            var manager = Object.FindFirstObjectByType<NetworkManager>();
            var pending = new TaskCompletionSource<HostFirewallResult>();
            try
            {
                menu.ConfigureHostFirewallAccess(new Access { Check = pending.Task });
                menu.OpenNetworkLobby();
                menu.RequestRoomFromMenu(true, "", "17845");
                Assert.That(menu.FirewallState, Is.EqualTo(HostFirewallState.Checking));
                menu.DisconnectRoom();
                pending.SetResult(HostFirewallResult.Allowed);
                yield return null;
                yield return null;
                Assert.That(manager.IsListening, Is.False);
                Assert.That(menu.FirewallState, Is.EqualTo(HostFirewallState.Idle));
                Assert.That(menu.IsNetworkLobbyOpen, Is.True);
            }
            finally
            {
                manager.Shutdown();
                Object.Destroy(manager.gameObject);
                if (scene.IsValid()) SceneManager.UnloadSceneAsync(scene);
            }
            yield return null;
        }
    }
}

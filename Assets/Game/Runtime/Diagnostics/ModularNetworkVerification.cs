#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using FunGame.Demo;
using FunGame.Networking;
using FunGame.Tools;
using FunGame.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Diagnostics
{
    /// <summary>仅由显式命令行参数启用的双进程晚加入、重连验证。</summary>
    public sealed class ModularNetworkVerification : MonoBehaviour
    {
        private string _role;
        private string _output;
        private string _port;
        private string _previousNickname;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            string[] args = Environment.GetCommandLineArgs();
            string role = args.FirstOrDefault(a => a.StartsWith("--module-network-role="));
            if (role == null) return;
            var runner = new GameObject("Modular Network Verification").AddComponent<ModularNetworkVerification>();
            runner._role = role.Split('=')[1];
            runner._output = args.First(a => a.StartsWith("--module-network-output=")).Substring("--module-network-output=".Length);
            runner._port = args.First(a => a.StartsWith("--module-network-port=")).Split('=')[1];
            Directory.CreateDirectory(runner._output);
        }

        private IEnumerator Start()
        {
            Application.logMessageReceived += OnLog;
            yield return Until(() => FindFirstObjectByType<SharedMapModeController>()?.IsReady == true, "module readiness");
            Require(SceneManager.sceneCount == 4, "Expected gameplay plus three environment scenes.");
            var menu = FindFirstObjectByType<GameMenuController>();
            _previousNickname = NetworkPlayerController.LocalNickname;
            NetworkPlayerController.LocalNickname = _role == "host" ? "主机维修员" : "客户端维修员";
            if (_role == "host") yield return Host(menu);
            else if (_role == "client") yield return Client(menu);
            else throw new InvalidOperationException("Unknown verification role.");
            Debug.Log("[ModuleNetworkCheck] PASS: " + _role);
            Application.Quit(0);
        }

        private IEnumerator Host(GameMenuController menu)
        {
            menu.OpenNetworkLobby();
            yield return null;
            Require(menu.StartRoom(true, "127.0.0.1", _port), "Host could not start.");
            yield return Until(() => FindFirstObjectByType<NetworkCoolingIncidentController>()?.IsSpawned == true, "host incident");
            var incident = FindFirstObjectByType<NetworkCoolingIncidentController>();
            Require(incident.TryExecuteServer(NetworkIncidentAction.InspectPressure, ToolKind.None), "Pressure action failed.");
            Require(incident.TryExecuteServer(NetworkIncidentAction.InspectPump, ToolKind.None), "Pump action failed.");
            for (int i = 0; i < 3; i++) Require(incident.TryExecuteServer(NetworkIncidentAction.BridgeCircuit, ToolKind.CircuitBridger), "Bridge action failed.");
            for (int i = 0; i < 4; i++) Require(incident.TryExecuteServer(NetworkIncidentAction.SealLeak, ToolKind.SealantGun), "Seal action failed.");
            yield return Until(() => FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None).Length == 7, "host enemies");
            NetworkCombatEnemy[] enemies = FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None);
            foreach (NetworkCombatEnemy enemy in enemies)
            {
                Require(enemy.Template != null, "Host template missing.");
                // Keep the snapshot stable while the remote process disconnects/reconnects.
                // This is runtime-only test control; no configuration or asset is changed.
                enemy.enabled = false;
            }
            string[] expected = new[] { incident.Phase.ToString() }
                .Concat(enemies.Select(e => e.Template.TargetId + ":" + e.Health).OrderBy(s => s, StringComparer.Ordinal)).ToArray();
            File.WriteAllLines(Path.Combine(_output, "host.pending"), expected);
            File.Move(Path.Combine(_output, "host.pending"), Path.Combine(_output, "host.ready"));
            yield return new WaitForSecondsRealtime(2f);
            LogPlayerNames();
            yield return Until(() => FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None)
                .Any(p => p.DisplayName == "客户端维修员"), "client nickname replicated to host");
            var chat = FindFirstObjectByType<NetworkChatController>();
            yield return Until(() => chat.LatestMessage == "客户端维修员：客户端聊天验证 0", "client chat on host");
            Require(chat.ClosedPreviewCount > 0, "Closed host chat must preview the remote message.");
            Require(chat.SendMessage("主机回复"), "Host chat send failed.");
            yield return CaptureChat("host-chat-preview", false);
            yield return Until(() => File.Exists(Path.Combine(_output, "client.reconnected")), "remote reconnect validation", 75f);
            NetworkManager.Singleton.Shutdown();
            yield return null;
        }

        private IEnumerator Client(GameMenuController menu)
        {
            yield return Until(() => File.Exists(Path.Combine(_output, "host.ready")), "host snapshot", 45f);
            string[] expected = File.ReadAllLines(Path.Combine(_output, "host.ready"));
            for (int attempt = 0; attempt < 2; attempt++)
            {
                menu.OpenNetworkLobby();
                yield return null;
                Require(menu.StartRoom(false, "127.0.0.1", _port), "Client could not connect.");
                yield return Until(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient
                    && FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None).Length == expected.Length - 1, "client synchronization");
                NetworkCombatEnemy[] enemies = FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None);
                Require(enemies.All(e => e.Template != null && e.Template.Definition != null), "Client template/configuration missing.");
                string[] actual = new[] { FindFirstObjectByType<NetworkCoolingIncidentController>().Phase.ToString() }
                    .Concat(enemies.Select(e => e.Template.TargetId + ":" + e.Health).OrderBy(s => s, StringComparer.Ordinal)).ToArray();
                Require(expected.SequenceEqual(actual), "Late join/reconnect snapshot mismatch.");
                Require(SceneManager.sceneCount == 4, "Duplicate environment after reconnect.");
                yield return new WaitForSecondsRealtime(1f);
                LogPlayerNames();
                yield return Until(() => FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None)
                    .Any(p => p.DisplayName == "主机维修员") && NetworkManager.Singleton.LocalClient.PlayerObject
                    .GetComponent<NetworkPlayerController>().DisplayName == "客户端维修员", "nickname late join/reconnect");
                var chat = FindFirstObjectByType<NetworkChatController>();
                Require(chat.SendMessage("客户端聊天验证 " + attempt), "Client chat send failed.");
                if (attempt == 0)
                {
                    yield return Until(() => chat.LatestMessage == "主机维修员：主机回复", "host chat on client");
                    Require(chat.ClosedPreviewCount > 0, "Closed client chat preview missing.");
                    yield return CaptureChat("client-chat-preview", false);
                    yield return CaptureChat("client-chat-open", true);
                    chat.SetPanelVisible(false);
                    int count = chat.MessageCount;
                    yield return new WaitForSecondsRealtime(8.2f);
                    Require(chat.ClosedPreviewCount == 0 && chat.MessageCount == count, "Preview must expire without losing history.");
                }
                FindFirstObjectByType<NetworkSessionController>().StopSession();
                yield return Until(() => !NetworkManager.Singleton.IsListening
                    && FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None).Length == 0, "disconnect cleanup");
                yield return new WaitForSecondsRealtime(0.4f);
            }
            File.WriteAllText(Path.Combine(_output, "client.reconnected"), "PASS");
        }

        private static IEnumerator Until(Func<bool> predicate, string description, float timeout = 25f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!predicate() && Time.realtimeSinceStartup < deadline) yield return null;
            Require(predicate(), "Timed out: " + description);
        }

        private void LogPlayerNames() => Debug.Log("[ModuleNetworkCheck] " + _role + " names: " +
            string.Join(", ", FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None)
                .Select(p => p.OwnerClientId + "=" + p.DisplayName)) + " local=" + NetworkPlayerController.LocalNickname);

        private IEnumerator CaptureChat(string name, bool open)
        {
            if (!Environment.GetCommandLineArgs().Contains("--module-network-capture")) yield break;
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            var local = NetworkManager.Singleton.LocalClient.PlayerObject;
            var remote = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None).First(p => !p.IsOwner);
            Camera camera = local.GetComponentInChildren<Camera>();
            local.GetComponent<FunGame.Player.FirstPersonController>().enabled = false;
            camera.transform.rotation = Quaternion.LookRotation(remote.transform.position + Vector3.up * 1.5f - camera.transform.position);
            FindFirstObjectByType<NetworkChatController>().SetPanelVisible(open);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return new WaitForEndOfFrame();
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(_output, name + ".png"), screenshot.EncodeToPNG());
            Destroy(screenshot);
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        private void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception) Application.Quit(1);
        }
        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            if (_previousNickname != null) NetworkPlayerController.LocalNickname = _previousNickname;
        }
    }
}
#endif

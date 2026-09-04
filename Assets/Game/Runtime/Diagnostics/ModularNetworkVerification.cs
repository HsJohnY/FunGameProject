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
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        private void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception) Application.Quit(1);
        }
        private void OnDestroy() => Application.logMessageReceived -= OnLog;
    }
}
#endif

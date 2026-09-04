#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using FunGame.Demo;
using FunGame.Combat;
using FunGame.Incident;
using FunGame.Networking;
using FunGame.Tools;
using FunGame.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Diagnostics
{
    /// <summary>仅在显式开发验证参数下运行，实际加载双模式并导出玩家画面。</summary>
    public sealed class M4BuildVerification : MonoBehaviour
    {
        private string _output;
        private bool _failed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            string argument = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("--m4-check-output="));
            if (argument == null) return;
            var runner = new GameObject("M4 Build Verification").AddComponent<M4BuildVerification>();
            DontDestroyOnLoad(runner.gameObject);
            runner._output = argument.Substring("--m4-check-output=".Length);
            Directory.CreateDirectory(runner._output);
        }

        private IEnumerator Start()
        {
            Application.logMessageReceived += OnLog;
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(4f);
            yield return WaitForModules();
            GameMenuController menu = FindFirstObjectByType<GameMenuController>();
            Require(SceneManager.sceneCountInBuildSettings == 4, "Canonical gameplay and three additive environments are packaged");
            Require(FindFirstObjectByType<SharedMapModeController>().IsReady, "Environment modules loaded before menu entry");
            Require(menu != null && menu.CanStartSinglePlayer, "Single-player button must be available in the built menu");
            yield return Capture("01-mode-menu.png");
            menu.OpenNetworkLobby();
            yield return Capture("00-network-lobby.png");
            Require(menu.IsNetworkLobbyOpen && Cursor.lockState == CursorLockMode.None, "Lobby has usable cursor before spawn");
            NetworkSessionController session = FindFirstObjectByType<NetworkSessionController>();
            Require(menu.StartRoom(true, "127.0.0.1", "17842"), "Host start from lobby");
            yield return new WaitForSecondsRealtime(1f);
            NetworkManager manager = NetworkManager.Singleton;
            Require(manager != null && manager.LocalClient.PlayerObject != null, "Owner player spawned");
            var player = manager.LocalClient.PlayerObject;
            Require(!menu.IsMenuOpen, "Connected owner enters gameplay");
            menu.OpenNetworkLobby();
            yield return null;
            Require(!player.GetComponent<FunGame.Player.FirstPersonController>().IsInputEnabled
                && Cursor.lockState == CursorLockMode.None, "Lobby releases owner cursor and blocks gameplay input");
            yield return Capture("11-connected-room.png");
            menu.EnterGameplayForAutomation();
            var chat = FindFirstObjectByType<NetworkChatController>();
            chat.SetPanelVisible(true);
            for (int i = 0; i < 22; i++) chat.SendMessage($"维修记录 {i + 1}：检查冷却管件，等待队友一起处理配电舱继电器。");
            yield return Capture("12-coop-chat-history.png");
            Require(chat.MessageCount == 22, "Full session chat history retained");
            chat.SetPanelVisible(false);
            Vector3 originalPosition = player.transform.position;
            Camera ownerCamera = player.GetComponentInChildren<Camera>();
            Quaternion originalView = ownerCamera.transform.rotation;
            var pipe = FindFirstObjectByType<NetworkCarryableItem>();
            PoseCamera(player.gameObject, pipe.transform.position + new Vector3(0.7f, -0.2f, -2.1f), pipe.transform.position);
            yield return Capture("13-network-pipe.png");
            var fastener = FindObjectsByType<NetworkIncidentStation>(FindObjectsSortMode.None)
                .Single(s => s.Action == NetworkIncidentAction.OperateFastener);
            PoseCamera(player.gameObject, fastener.transform.position + new Vector3(0.5f, -0.4f, -2f), fastener.transform.position);
            yield return Capture("14-fastener.png");
            player.transform.position = originalPosition;
            ownerCamera.transform.rotation = originalView;
            player.GetComponent<CharacterController>().enabled = true;
            player.GetComponent<FunGame.Player.FirstPersonController>().enabled = true;
            player.GetComponent<NetworkPlayerToolbelt>().RequestToggleTool(ToolKind.ImpactWrench);
            yield return new WaitForSecondsRealtime(0.5f);
            var guidance = player.GetComponent<DemoObjectiveGuidancePresenter>();
            Require(guidance.enabled && guidance.CurrentTarget != null, "Host has working guidance");
            yield return Capture("02-coop-guidance-wrench.png");
            player.GetComponent<NetworkPlayerToolbelt>().RequestToggleTool(ToolKind.SealantGun);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Capture("03-coop-sealant.png");
            player.GetComponent<NetworkPlayerToolbelt>().RequestToggleTool(ToolKind.CircuitBridger);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Capture("04-coop-bridger.png");
            var incident = FindFirstObjectByType<NetworkCoolingIncidentController>();
            incident.TryExecuteServer(NetworkIncidentAction.InspectPressure, ToolKind.None);
            incident.TryExecuteServer(NetworkIncidentAction.InspectPump, ToolKind.None);
            for (int i = 0; i < 3; i++) incident.TryExecuteServer(NetworkIncidentAction.BridgeCircuit, ToolKind.CircuitBridger);
            for (int i = 0; i < 4; i++) incident.TryExecuteServer(NetworkIncidentAction.SealLeak, ToolKind.SealantGun);
            yield return null;
            NetworkCombatEnemy[] networkEnemies = FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None);
            Require(networkEnemies.Length == 7 && networkEnemies.All(e => e.Template != null), "Network cooling encounter uses map templates");
            LookAtCombat(player.gameObject, networkEnemies.Select(e => e.Template.AttackPosition).ToArray());
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture("07-coop-combat.png");
            manager.Shutdown();
            yield return new WaitForSecondsRealtime(0.3f);
            menu.ReturnToModeSelection();
            yield return new WaitForSecondsRealtime(0.5f);
            menu = FindFirstObjectByType<GameMenuController>();
            menu.StartSinglePlayerMode();
            yield return new WaitForSecondsRealtime(0.8f);
            yield return WaitForModules();
            Require(SceneManager.GetActiveScene().name == GameMenuController.SinglePlayerScene, "Single-player scene loaded");
            Require(FindFirstObjectByType<SinglePlayerDemoController>()?.enabled == true, "Original solo campaign active");
            Require(FindFirstObjectByType<SharedMapModeController>().Mode == ExpeditionMode.Solo, "Same scene selected solo logic");
            Require(FindFirstObjectByType<NetworkSessionController>() == null, "Solo requires no network session");
            Require(FindFirstObjectByType<DemoObjectiveGuidancePresenter>()?.CurrentTarget != null, "Solo guidance active");
            yield return Capture("05-solo-guidance.png");
            FindFirstObjectByType<GameMenuController>().OpenSettingsForAutomation();
            yield return Capture("06-solo-settings.png");
            FindFirstObjectByType<GameMenuController>().EnterGameplayForAutomation();
            var encounter = FindFirstObjectByType<CoolingCombatIntegrationController>().Encounter;
            encounter.BeginEncounter();
            LookAtCombat(FindFirstObjectByType<FunGame.Player.FirstPersonController>().gameObject,
                encounter.Enemies.Select(e => e.AttackPosition).ToArray());
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Capture("08-solo-combat.png");
            foreach (InterferenceEnemy enemy in encounter.Enemies) enemy.SetEncounterActive(false);
            var soloPlayer = FindFirstObjectByType<FunGame.Player.FirstPersonController>();
            Transform plate = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == "Hidden Maintenance Plate 325");
            PoseCamera(soloPlayer.gameObject, plate.position + Vector3.right * 2.5f, plate.position);
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Capture("09-plate-325.png");
            FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(t => t.name == "Chapter 2 - Power Relay Compartment").gameObject.SetActive(true);
            Transform rack = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == "Relay Bridger Station");
            PoseCamera(soloPlayer.gameObject, new Vector3(2.5f, 1f, 15.5f), rack.position);
            yield return new WaitForSecondsRealtime(0.2f);
            yield return Capture("10-relay-rack-clearance.png");
            FindFirstObjectByType<GameMenuController>().ReturnToModeSelection();
            yield return new WaitForSecondsRealtime(0.5f);
            yield return WaitForModules();
            Require(SceneManager.GetActiveScene().name == GameMenuController.CooperativeScene, "Return to mode menu");
            Require(FindFirstObjectByType<GameMenuController>().IsMenuOpen, "Mode menu open after return");
            Debug.Log(_failed ? "[M4BuildCheck] FAIL" : "[M4BuildCheck] PASS: host guidance, three models, solo entry, settings, return menu");
            Application.Quit(_failed ? 1 : 0);
        }

        private IEnumerator WaitForModules()
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (FindFirstObjectByType<SharedMapModeController>()?.IsReady != true && Time.realtimeSinceStartup < deadline)
                yield return null;
            Require(FindFirstObjectByType<SharedMapModeController>()?.IsReady == true, "Modules became ready");
            Require(SceneManager.sceneCount == 4, "Mode changes leave exactly one copy of each environment");
        }

        private IEnumerator Capture(string file)
        {
            yield return new WaitForEndOfFrame();
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(_output, file), image.EncodeToPNG());
            Destroy(image);
        }

        private static void LookAtCombat(GameObject player, Vector3[] positions)
        {
            Vector3 center = positions.Aggregate(Vector3.zero, (sum, p) => sum + p) / positions.Length;
            CharacterController body = player.GetComponent<CharacterController>();
            body.enabled = false;
            player.transform.position = new Vector3(center.x, 0.95f, center.z - 4.5f);
            Camera view = player.GetComponentInChildren<Camera>();
            view.transform.rotation = Quaternion.LookRotation(center - view.transform.position);
            body.enabled = true;
            Physics.SyncTransforms();
        }

        private static void PoseCamera(GameObject player, Vector3 position, Vector3 target)
        {
            var controller = player.GetComponent<FunGame.Player.FirstPersonController>();
            controller.enabled = false;
            player.GetComponent<CharacterController>().enabled = false;
            player.transform.position = position;
            Camera view = player.GetComponentInChildren<Camera>();
            view.transform.rotation = Quaternion.LookRotation(target - view.transform.position);
        }

        private void Require(bool condition, string message)
        {
            if (!condition) { _failed = true; Application.Quit(1); throw new InvalidOperationException(message); }
        }
        private void OnLog(string message, string trace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error) _failed = true;
        }
        private void OnDestroy() => Application.logMessageReceived -= OnLog;
    }
}
#endif

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
            GameMenuController menu = FindFirstObjectByType<GameMenuController>();
            Require(menu != null && menu.CanStartSinglePlayer, "Single-player button must be available in the built menu");
            yield return Capture("01-mode-menu.png");
            menu.EnterGameplayForAutomation();
            NetworkSessionController session = FindFirstObjectByType<NetworkSessionController>();
            Require(session.TrySetEndpointInput("127.0.0.1", "17842") && session.StartHost(), "Host start");
            yield return new WaitForSecondsRealtime(1f);
            NetworkManager manager = NetworkManager.Singleton;
            Require(manager != null && manager.LocalClient.PlayerObject != null, "Owner player spawned");
            var player = manager.LocalClient.PlayerObject;
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
            manager.Shutdown();
            yield return new WaitForSecondsRealtime(0.3f);
            menu.ReturnToModeSelection();
            yield return new WaitForSecondsRealtime(0.5f);
            menu = FindFirstObjectByType<GameMenuController>();
            menu.StartSinglePlayerMode();
            yield return new WaitForSecondsRealtime(0.8f);
            Require(SceneManager.GetActiveScene().name == GameMenuController.SinglePlayerScene, "Single-player scene loaded");
            Require(FindFirstObjectByType<SinglePlayerDemoController>()?.enabled == true, "Original solo campaign active");
            Require(FindFirstObjectByType<NetworkSessionController>() == null, "Solo requires no network session");
            Require(FindFirstObjectByType<DemoObjectiveGuidancePresenter>()?.CurrentTarget != null, "Solo guidance active");
            yield return Capture("05-solo-guidance.png");
            FindFirstObjectByType<GameMenuController>().OpenSettingsForAutomation();
            yield return Capture("06-solo-settings.png");
            FindFirstObjectByType<GameMenuController>().ReturnToModeSelection();
            yield return new WaitForSecondsRealtime(0.5f);
            Require(SceneManager.GetActiveScene().name == GameMenuController.CooperativeScene, "Return to mode menu");
            Require(FindFirstObjectByType<GameMenuController>().IsMenuOpen, "Mode menu open after return");
            Debug.Log(_failed ? "[M4BuildCheck] FAIL" : "[M4BuildCheck] PASS: host guidance, three models, solo entry, settings, return menu");
            Application.Quit(_failed ? 1 : 0);
        }

        private IEnumerator Capture(string file)
        {
            yield return new WaitForEndOfFrame();
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(_output, file), image.EncodeToPNG());
            Destroy(image);
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

using System;
using System.Collections;
using System.IO;
using FunGame.UI;
using UnityEngine;

namespace FunGame.Demo
{
    /// <summary>
    /// 仅在专用命令行参数下捕获真实玩家构建画面，供自动视觉检查；普通游玩不产生文件。
    /// </summary>
    public sealed class DemoScreenshotCheckpoint : MonoBehaviour
    {
        private const string MainMenuArgument = "--demo-capture-main-menu";
        private const string GameplayArgument = "--demo-capture-gameplay";
        private const string SettingsArgument = "--demo-capture-settings";

        private IEnumerator Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            bool captureMainMenu = Array.IndexOf(arguments, MainMenuArgument) >= 0;
            bool captureGameplay = Array.IndexOf(arguments, GameplayArgument) >= 0;
            bool captureSettings = Array.IndexOf(arguments, SettingsArgument) >= 0;
            if (!captureMainMenu && !captureGameplay && !captureSettings)
            {
                yield break;
            }

            GameMenuController menu = FindFirstObjectByType<GameMenuController>();
            if (captureGameplay)
            {
                menu?.EnterGameplayForAutomation();
            }
            else if (captureSettings)
            {
                menu?.OpenSettingsForAutomation();
            }

            for (int frame = 0; frame < 12; frame++)
            {
                yield return null;
            }

            string fileName = captureMainMenu
                ? "MainMenuCapture.png"
                : captureSettings
                    ? "SettingsCapture.png"
                    : "GameplayCapture.png";
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", fileName));
            ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Debug.Log($"[Demo] screenshot={path} exists={File.Exists(path)}", this);
            Application.Quit(File.Exists(path) ? 0 : 2);
        }
    }
}

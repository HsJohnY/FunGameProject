using System;
using System.Collections;
using System.IO;
using System.Linq;
using FunGame.Combat;
using FunGame.Incident;
using FunGame.Interaction;
using FunGame.Tools;
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
        private const string CompletionVerificationArgument = "--demo-verify-completion";

        private IEnumerator Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            bool captureMainMenu = Array.IndexOf(arguments, MainMenuArgument) >= 0;
            bool captureGameplay = Array.IndexOf(arguments, GameplayArgument) >= 0;
            bool captureSettings = Array.IndexOf(arguments, SettingsArgument) >= 0;
            bool verifyCompletion = Array.IndexOf(arguments, CompletionVerificationArgument) >= 0;
            if (!captureMainMenu && !captureGameplay && !captureSettings && !verifyCompletion)
            {
                yield break;
            }

            GameMenuController menu = FindFirstObjectByType<GameMenuController>();
            if (verifyCompletion)
            {
                yield return VerifyGeneratedDemoCompletion(menu);
                yield break;
            }

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

        private IEnumerator VerifyGeneratedDemoCompletion(GameMenuController menu)
        {
            menu?.EnterGameplayForAutomation();
            yield return null;
            yield return null;

            SinglePlayerDemoController campaign = FindFirstObjectByType<SinglePlayerDemoController>();
            CoolingIncidentController incident = FindFirstObjectByType<CoolingIncidentController>();
            ContextInteractor interactor = FindFirstObjectByType<ContextInteractor>();
            PlayerToolbelt toolbelt = FindFirstObjectByType<PlayerToolbelt>();
            if (!Require(campaign != null && incident != null && interactor != null && toolbelt != null,
                    "missing-required-runtime-component"))
            {
                yield break;
            }

            yield return CaptureVerificationStep(
                interactor,
                "01-cooling-start.png",
                new Vector3(0f, 0.05f, -7f),
                Vector3.forward);

            for (int run = 0; run < campaign.RequiredCoolingRunCount; run++)
            {
                if (!Require(CompleteCoolingIncident(incident), $"cooling-run-{run + 1}-rejected"))
                {
                    yield break;
                }

                yield return null;
            }

            if (!Require(campaign.Chapter == SinglePlayerDemoChapter.RelaySurge, "relay-chapter-not-entered"))
            {
                yield break;
            }

            yield return CaptureVerificationStep(
                interactor,
                "02-relay-chapter.png",
                new Vector3(0f, 0.05f, 13f),
                Vector3.forward);

            toolbelt.Equip(ToolKind.CircuitBridger);
            DemoRelayTarget[] relays = FindObjectsByType<DemoRelayTarget>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .OrderBy(relay => relay.name)
                .ToArray();
            if (!Require(relays.Length == campaign.RequiredRelayCount, "relay-count-mismatch"))
            {
                yield break;
            }

            foreach (DemoRelayTarget relay in relays)
            {
                for (int step = relay.CompletedSteps; step < 3; step++)
                {
                    if (!Require(relay.ApplyTool(toolbelt), $"relay-rejected:{relay.name}:{step + 1}"))
                    {
                        yield break;
                    }
                }
            }

            toolbelt.Equip(ToolKind.ImpactWrench);
            if (!Require(DefeatEncounter(campaign.RelayDefenseEncounter, toolbelt), "relay-defense-not-completable"))
            {
                yield break;
            }

            yield return null;
            if (!Require(campaign.Chapter == SinglePlayerDemoChapter.StormCalibration, "storm-chapter-not-entered"))
            {
                yield break;
            }

            yield return CaptureVerificationStep(
                interactor,
                "03-storm-chapter.png",
                new Vector3(0f, 0.05f, 33f),
                Vector3.forward);

            DemoEasterEgg325Interactable secret = FindFirstObjectByType<DemoEasterEgg325Interactable>(FindObjectsInactive.Include);
            if (!Require(secret != null && secret.ExecuteInteraction(interactor), "secret-325-not-interactable"))
            {
                yield break;
            }

            for (int wave = 0; wave < campaign.StormWaveCount; wave++)
            {
                CombatEncounterController encounter = campaign.CurrentStormEncounter;
                if (!Require(DefeatEncounter(encounter, toolbelt), $"storm-wave-{wave + 1}-not-completable"))
                {
                    yield break;
                }

                yield return null;
                if (!Require(campaign.IsAwaitingCalibration, $"storm-wave-{wave + 1}-calibration-not-requested") ||
                    !Require(campaign.ExecuteCampaignConsole(), $"storm-wave-{wave + 1}-calibration-rejected"))
                {
                    yield break;
                }

                yield return null;
            }

            if (!Require(campaign.IsCompleted && campaign.EasterEgg325Discovered, "completion-state-mismatch"))
            {
                yield break;
            }

            yield return CaptureVerificationStep(
                interactor,
                "04-demo-completed.png",
                new Vector3(0f, 0.05f, 33f),
                Vector3.forward);

            Debug.Log(
                $"[DemoVerification] result=completed chapter={campaign.Chapter} " +
                $"cooling={campaign.CoolingRunsCompleted}/{campaign.RequiredCoolingRunCount} " +
                $"relays={campaign.StabilizedRelayCount}/{campaign.RequiredRelayCount} " +
                $"waves={campaign.CurrentStormWave + 1}/{campaign.StormWaveCount} secret325={campaign.EasterEgg325Discovered}",
                this);
            Application.Quit(0);
        }

        private static IEnumerator CaptureVerificationStep(
            ContextInteractor interactor,
            string fileName,
            Vector3 playerPosition,
            Vector3 playerForward)
        {
            CharacterController character = interactor.GetComponent<CharacterController>();
            if (character != null)
            {
                character.enabled = false;
            }

            interactor.transform.SetPositionAndRotation(
                playerPosition,
                Quaternion.LookRotation(playerForward, Vector3.up));
            Camera camera = interactor.GetComponentInChildren<Camera>(true);
            if (camera != null)
            {
                camera.transform.localRotation = Quaternion.identity;
            }

            if (character != null)
            {
                character.enabled = true;
            }

            yield return null;
            yield return null;

            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "VerificationCaptures"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Debug.Log($"[DemoVerification] screenshot={fileName} exists={File.Exists(path)} path={path}");
        }

        private static bool CompleteCoolingIncident(CoolingIncidentController incident)
        {
            return incident.TryInspectPressure() &&
                   incident.TryInspectPump() &&
                   incident.TryAdvanceCircuitBridge() &&
                   incident.TryAdvanceCircuitBridge() &&
                   incident.TryAdvanceCircuitBridge() &&
                   incident.AddSealProgress(1f) &&
                   incident.TryLoosen() &&
                   incident.TryInstallPipe() &&
                   incident.TryTighten() &&
                   incident.TryInspectPressure() &&
                   incident.TryResetPump();
        }

        private static bool DefeatEncounter(CombatEncounterController encounter, PlayerToolbelt toolbelt)
        {
            if (encounter == null || encounter.State != CombatEncounterState.Active)
            {
                return false;
            }

            foreach (InterferenceEnemy enemy in encounter.Enemies)
            {
                int remainingHits = enemy.MaxHealth + 1;
                while (!enemy.IsDefeated && remainingHits-- > 0)
                {
                    if (!enemy.ApplyTool(toolbelt))
                    {
                        return false;
                    }
                }

                if (!enemy.IsDefeated)
                {
                    return false;
                }
            }

            return encounter.State == CombatEncounterState.Succeeded;
        }

        private bool Require(bool condition, string failure)
        {
            if (condition)
            {
                return true;
            }

            Debug.LogError($"[DemoVerification] result=failed reason={failure}", this);
            Application.Quit(3);
            return false;
        }
    }
}

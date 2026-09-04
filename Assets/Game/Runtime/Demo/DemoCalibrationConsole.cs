using FunGame.Interaction;
using UnityEngine;

namespace FunGame.Demo
{
    public enum DemoCalibrationConsoleRole
    {
        Shared = 0,
        RelayRecovery = 1,
        StormCalibration = 2
    }

    /// <summary>
    /// 章节本地控制台：配电舱只负责第二章恢复，核心舱只负责第三章校准与恢复。
    /// </summary>
    public sealed class DemoCalibrationConsole : MonoBehaviour, IContextInteractable
    {
        [SerializeField] private SinglePlayerDemoController campaign;
        [SerializeField] private DemoCalibrationConsoleRole role;

        public DemoCalibrationConsoleRole Role => role;

        public void Configure(
            SinglePlayerDemoController configuredCampaign,
            DemoCalibrationConsoleRole configuredRole = DemoCalibrationConsoleRole.Shared)
        {
            campaign = configuredCampaign;
            role = configuredRole;
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            bool supportsCurrentChapter = campaign != null && SupportsChapter(campaign.Chapter);
            bool available = supportsCurrentChapter && campaign.IsCampaignConsoleAvailable;
            return new InteractionOption(
                role == DemoCalibrationConsoleRole.RelayRecovery
                    ? "demo-relay-recovery-console"
                    : "demo-storm-calibration-console",
                role == DemoCalibrationConsoleRole.RelayRecovery ? "配电舱恢复终端" : "风暴核心校准终端",
                campaign != null ? campaign.CampaignConsoleAction : "检查",
                InteractionPriority.Device,
                available,
                campaign == null
                    ? "章节控制器未连接"
                    : supportsCurrentChapter
                        ? campaign.CurrentObjective
                        : role == DemoCalibrationConsoleRole.RelayRecovery
                            ? "该终端只负责配电舱恢复"
                            : "该终端只负责风暴核心校准");
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            return campaign != null && SupportsChapter(campaign.Chapter) && campaign.ExecuteCampaignConsole();
        }

        public bool SupportsChapter(SinglePlayerDemoChapter chapter)
        {
            return role == DemoCalibrationConsoleRole.Shared ||
                   (role == DemoCalibrationConsoleRole.RelayRecovery && chapter == SinglePlayerDemoChapter.RelaySurge) ||
                   (role == DemoCalibrationConsoleRole.StormCalibration && chapter == SinglePlayerDemoChapter.StormCalibration);
        }
    }
}

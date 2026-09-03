using FunGame.Interaction;
using UnityEngine;

namespace FunGame.Demo
{
    /// <summary>
    /// 第二、三章共用的恢复控制台：遭遇失败时重启章节，波次完成时确认校准。
    /// </summary>
    public sealed class DemoCalibrationConsole : MonoBehaviour, IContextInteractable
    {
        [SerializeField] private SinglePlayerDemoController campaign;

        public void Configure(SinglePlayerDemoController configuredCampaign)
        {
            campaign = configuredCampaign;
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            bool available = campaign != null && campaign.IsCampaignConsoleAvailable;
            return new InteractionOption(
                "demo-storm-console",
                "风暴校准控制台",
                campaign != null ? campaign.CampaignConsoleAction : "检查",
                InteractionPriority.Device,
                available,
                campaign != null ? campaign.CurrentObjective : "章节控制器未连接");
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            return campaign != null && campaign.ExecuteCampaignConsole();
        }
    }
}

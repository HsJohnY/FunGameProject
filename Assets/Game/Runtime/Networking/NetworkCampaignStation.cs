using FunGame.Interaction;
using FunGame.Tools;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>第二、三章的场景交互入口；真实状态由主机战役控制器保存。</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class NetworkCampaignStation : MonoBehaviour, IToolTarget, IContextInteractable
    {
        [SerializeField] private int stationIndex;
        [SerializeField] private bool calibrationConsole;

        public int StationIndex => stationIndex;
        public bool IsCalibrationConsole => calibrationConsole;

        public void Configure(int index, bool isConsole)
        {
            stationIndex = index;
            calibrationConsole = isConsole;
        }

        public ToolActionOption GetToolAction(PlayerToolbelt toolbelt)
        {
            NetworkCampaignController campaign = FindFirstObjectByType<NetworkCampaignController>();
            bool available = !calibrationConsole && campaign != null && campaign.CanOperateRelay(stationIndex) &&
                             toolbelt.EquippedTool == ToolKind.CircuitBridger;
            return new ToolActionOption($"m4-relay-{stationIndex}", $"风暴继电器 {stationIndex + 1}",
                "推进相位校准", ToolKind.CircuitBridger, toolbelt.EquippedTool, available,
                campaign == null ? "战役尚未启动" : "当前章节、工具或继电器状态不匹配");
        }

        public bool ApplyTool(PlayerToolbelt toolbelt)
        {
            NetworkPlayerCampaignAgent agent = toolbelt != null ? toolbelt.GetComponent<NetworkPlayerCampaignAgent>() : null;
            return agent != null && agent.RequestRelay(stationIndex);
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            NetworkCampaignController campaign = FindFirstObjectByType<NetworkCampaignController>();
            bool available = calibrationConsole && campaign != null && campaign.CanConfirmStormWave;
            return new InteractionOption("m4-storm-console", "风暴核心校准终端", "写入波次校准",
                InteractionPriority.Device, available,
                available ? string.Empty : "清除当前波次后才能写入校准");
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            NetworkPlayerCampaignAgent agent = actor.GetComponent<NetworkPlayerCampaignAgent>();
            return calibrationConsole && agent != null && agent.RequestCalibration();
        }
    }
}

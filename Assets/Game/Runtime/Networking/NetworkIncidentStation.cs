using FunGame.Interaction;
using FunGame.Tools;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// 普通场景交互站。请求由玩家自身的已注册网络对象发送，本站不参与网络生成。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class NetworkIncidentStation : MonoBehaviour, IContextInteractable
    {
        [SerializeField] private string targetId;
        [SerializeField] private string targetName;
        [SerializeField] private NetworkIncidentAction action;

        public void Configure(
            string id,
            string displayName,
            NetworkIncidentAction configuredAction)
        {
            targetId = id;
            targetName = displayName;
            action = configuredAction;
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            NetworkCoolingIncidentController incident = FindFirstObjectByType<NetworkCoolingIncidentController>();
            NetworkPlayerToolbelt toolbelt = actor.GetComponent<NetworkPlayerToolbelt>();
            NetworkPlayerCarryController carryController = actor.GetComponent<NetworkPlayerCarryController>();
            ToolKind equippedTool = toolbelt != null ? toolbelt.EquippedTool : ToolKind.None;
            bool hasReplacementPipe = carryController != null && carryController.IsHoldingItem("m3-shared-task-part");
            bool available = incident != null && incident.IsActionAvailable(action, equippedTool, hasReplacementPipe);
            ToolKind requiredTool = incident != null
                ? NetworkCoolingIncidentController.GetRequiredTool(action, incident.Phase)
                : ToolKind.None;
            string reason = action == NetworkIncidentAction.InstallPipe
                && incident != null
                && incident.Phase == FunGame.Incident.CoolingIncidentPhase.InstallReplacementPipe
                ? "需要手持共享替换管件"
                : requiredTool == ToolKind.None
                    ? "当前事故阶段不需要此操作"
                : $"需要{requiredTool.GetDisplayName()}";
            return new InteractionOption(
                targetId,
                targetName,
                GetActionLabel(),
                GetInteractionPriority(),
                available,
                available ? string.Empty : reason);
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            NetworkPlayerIncidentAgent agent = actor.GetComponent<NetworkPlayerIncidentAgent>();
            return agent != null && agent.RequestAction(action);
        }

        private string GetActionLabel()
        {
            return action switch
            {
                NetworkIncidentAction.SealLeak => "执行密封",
                NetworkIncidentAction.OperateFastener => "操作连接件",
                NetworkIncidentAction.InstallPipe => "安装替换管件",
                NetworkIncidentAction.OperatePump => "操作控制台",
                _ => "操作"
            };
        }

        private InteractionPriority GetInteractionPriority()
        {
            return action switch
            {
                // 安装任务物必须高于普通拾取，避免准星同时命中管件与安装接口时继续拾取。
                NetworkIncidentAction.InstallPipe => InteractionPriority.TaskItemPlacement,
                NetworkIncidentAction.SealLeak => InteractionPriority.ContinuousAction,
                _ => InteractionPriority.Device
            };
        }
    }
}

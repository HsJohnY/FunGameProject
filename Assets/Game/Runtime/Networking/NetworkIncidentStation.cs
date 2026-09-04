using FunGame.Incident;
using FunGame.Interaction;
using FunGame.Tools;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// 普通场景交互站。请求由玩家自身的已注册网络对象发送，本站不参与网络生成。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class NetworkIncidentStation : MonoBehaviour, IContextInteractable, IToolTarget
    {
        [SerializeField] private string targetId;
        [SerializeField] private string targetName;
        [SerializeField] private NetworkIncidentAction action;
        public NetworkIncidentAction Action => action;

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
            if (requiredTool != ToolKind.None)
            {
                available = false;
            }
            string reason = action == NetworkIncidentAction.InstallPipe
                && incident != null
                && incident.Phase == FunGame.Incident.CoolingIncidentPhase.InstallReplacementPipe
                ? "需要手持共享替换管件"
                : requiredTool != ToolKind.None
                    ? $"装备{requiredTool.GetDisplayName()}后点击左键"
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
            NetworkCoolingIncidentController incident = FindFirstObjectByType<NetworkCoolingIncidentController>();
            if (incident != null && NetworkCoolingIncidentController.GetRequiredTool(action, incident.Phase) != ToolKind.None)
            {
                return false;
            }
            NetworkPlayerIncidentAgent agent = actor.GetComponent<NetworkPlayerIncidentAgent>();
            return agent != null && agent.RequestAction(action);
        }

        public ToolActionOption GetToolAction(PlayerToolbelt toolbelt)
        {
            NetworkCoolingIncidentController incident = FindFirstObjectByType<NetworkCoolingIncidentController>();
            ToolKind equipped = toolbelt != null ? toolbelt.EquippedTool : ToolKind.None;
            ToolKind required = NetworkCoolingIncidentController.GetRequiredTool(
                action, incident != null ? incident.Phase : CoolingIncidentPhase.Stabilized);
            bool available = required != ToolKind.None
                             && incident != null
                             && incident.IsActionAvailable(action, equipped);
            return new ToolActionOption(
                targetId,
                targetName,
                GetActionLabel(),
                required,
                equipped,
                available,
                available ? string.Empty : "当前阶段或工具不匹配");
        }

        public bool ApplyTool(PlayerToolbelt toolbelt)
        {
            NetworkCoolingIncidentController incident = FindFirstObjectByType<NetworkCoolingIncidentController>();
            if (incident == null
                || NetworkCoolingIncidentController.GetRequiredTool(action, incident.Phase) == ToolKind.None)
            {
                return false;
            }
            NetworkPlayerIncidentAgent agent = toolbelt != null
                ? toolbelt.GetComponent<NetworkPlayerIncidentAgent>()
                : null;
            return agent != null && agent.RequestAction(action);
        }

        private string GetActionLabel()
        {
            return action switch
            {
                NetworkIncidentAction.InspectPressure => "读取压力",
                NetworkIncidentAction.InspectPump => "检查泵体",
                NetworkIncidentAction.BridgeCircuit => "桥接线路",
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

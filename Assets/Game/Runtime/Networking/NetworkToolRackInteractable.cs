using FunGame.Interaction;
using FunGame.Tools;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// M3-3A 的静态工具架：场景本身无需网络对象，交互结果写入玩家的服务器权威工具位。
    /// </summary>
    public sealed class NetworkToolRackInteractable : MonoBehaviour, IContextInteractable
    {
        [SerializeField] private string targetId = "network-tool-rack";
        [SerializeField] private ToolKind offeredTool = ToolKind.ImpactWrench;
        public ToolKind OfferedTool => offeredTool;

        public void Configure(string id, ToolKind tool)
        {
            targetId = id;
            offeredTool = tool;
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            NetworkPlayerToolbelt toolbelt = actor.GetComponent<NetworkPlayerToolbelt>();
            bool available = toolbelt != null && toolbelt.IsOwner;
            bool alreadyEquipped = available && toolbelt.EquippedTool == offeredTool;
            return new InteractionOption(
                targetId,
                offeredTool.GetDisplayName() + "工具架",
                alreadyEquipped ? "放回" + offeredTool.GetDisplayName() : "装备" + offeredTool.GetDisplayName(),
                InteractionPriority.Device,
                available,
                available ? string.Empty : "仅本地玩家可操作工具架");
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            NetworkPlayerToolbelt toolbelt = actor.GetComponent<NetworkPlayerToolbelt>();
            return toolbelt != null && toolbelt.RequestToggleTool(offeredTool);
        }
    }
}

using FunGame.Interaction;
using UnityEngine;

namespace FunGame.Tools
{
    /// <summary>
    /// 使用上下文交互把指定工具放入玩家唯一主工具位。
    /// </summary>
    public sealed class ToolRackInteractable : MonoBehaviour, IContextInteractable
    {
        [SerializeField] private string targetId = "tool-rack";
        [SerializeField] private ToolKind offeredTool = ToolKind.ImpactWrench;

        public ToolKind OfferedTool => offeredTool;

        public void Configure(string id, ToolKind tool)
        {
            targetId = id;
            offeredTool = tool;
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            bool hasToolbelt = actor.Toolbelt != null;
            bool alreadyEquipped = hasToolbelt && actor.Toolbelt.EquippedTool == offeredTool;
            return new InteractionOption(
                targetId,
                offeredTool.GetDisplayName() + "工具位",
                alreadyEquipped ? "放回" + offeredTool.GetDisplayName() : "装备" + offeredTool.GetDisplayName(),
                InteractionPriority.Device,
                hasToolbelt,
                hasToolbelt ? string.Empty : "玩家缺少主工具位");
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            if (actor.Toolbelt == null)
            {
                return false;
            }

            return actor.Toolbelt.EquippedTool == offeredTool
                ? actor.Toolbelt.Unequip()
                : actor.Toolbelt.Equip(offeredTool);
        }
    }
}

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
                "装备" + offeredTool.GetDisplayName(),
                InteractionPriority.Device,
                hasToolbelt && !alreadyEquipped,
                !hasToolbelt ? "玩家缺少主工具位" : "已经装备");
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            return actor.Toolbelt != null && actor.Toolbelt.Equip(offeredTool);
        }
    }
}

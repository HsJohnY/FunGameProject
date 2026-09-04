using UnityEngine;

namespace FunGame.Interaction
{
    /// <summary>
    /// 让大型设备的外壳碰撞复用一个真实交互目标，避免装饰模型挡住内部交互面板。
    /// </summary>
    public sealed class ContextInteractionProxy : MonoBehaviour, IContextInteractable
    {
        [SerializeField] private MonoBehaviour targetBehaviour;

        public bool HasTarget => targetBehaviour is IContextInteractable;

        public void Configure(MonoBehaviour configuredTarget)
        {
            targetBehaviour = configuredTarget;
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            return targetBehaviour is IContextInteractable target
                ? target.GetInteractionOption(actor)
                : new InteractionOption(
                    "missing-interaction-proxy-target",
                    "设备外壳",
                    "检查",
                    InteractionPriority.Device,
                    false,
                    "设备交互代理未连接");
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            return targetBehaviour is IContextInteractable target && target.ExecuteInteraction(actor);
        }
    }
}

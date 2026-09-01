using UnityEngine;

namespace FunGame.Interaction
{
    /// <summary>
    /// M1-2 代表性设备动作：使用同一个上下文键切换控制台状态。
    /// </summary>
    public sealed class ToggleConsoleInteractable : MonoBehaviour, IContextInteractable
    {
        [SerializeField] private string targetId = "cooling-console";
        [SerializeField] private string targetName = "冷却控制台";
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private Color offColor = new Color(0.45f, 0.14f, 0.06f);
        [SerializeField] private Color onColor = new Color(0.1f, 0.8f, 0.45f);

        private MaterialPropertyBlock _propertyBlock;

        public bool IsOn { get; private set; }

        private void Awake()
        {
            // MaterialPropertyBlock 是 Unity 引擎对象包装，必须在生命周期回调中创建，
            // 不能放在 MonoBehaviour 字段初始化器中。
            _propertyBlock = new MaterialPropertyBlock();
            if (statusRenderer == null)
            {
                statusRenderer = GetComponent<Renderer>();
            }

            ApplyVisualState();
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            return new InteractionOption(
                targetId,
                targetName,
                IsOn ? "关闭" : "启动",
                InteractionPriority.Device);
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            IsOn = !IsOn;
            ApplyVisualState();
            return true;
        }

        private void ApplyVisualState()
        {
            if (statusRenderer == null)
            {
                return;
            }

            // PropertyBlock 避免为每次状态变化复制一份材质资产。
            statusRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", IsOn ? onColor : offColor);
            statusRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}

using UnityEngine;

namespace FunGame.Tools
{
    /// <summary>
    /// 冲击扳手语义样例，在“松开”和“紧固”之间切换机械连接状态。
    /// </summary>
    public sealed class MechanicalFastenerTarget : MonoBehaviour, IToolTarget
    {
        [SerializeField] private string targetId = "mechanical-fastener";
        [SerializeField] private string targetName = "管件机械连接";
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private bool isTightened = true;

        private MaterialPropertyBlock _propertyBlock;

        public bool IsTightened => isTightened;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            if (statusRenderer == null)
            {
                statusRenderer = GetComponent<Renderer>();
            }

            RefreshVisual();
        }

        public ToolActionOption GetToolAction(PlayerToolbelt toolbelt)
        {
            return new ToolActionOption(
                targetId,
                targetName,
                isTightened ? "松开" : "紧固",
                ToolKind.ImpactWrench,
                toolbelt.EquippedTool);
        }

        public bool ApplyTool(PlayerToolbelt toolbelt)
        {
            if (toolbelt.EquippedTool != ToolKind.ImpactWrench)
            {
                return false;
            }

            isTightened = !isTightened;
            RefreshVisual();
            return true;
        }

        private void RefreshVisual()
        {
            if (statusRenderer == null)
            {
                return;
            }

            statusRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", isTightened ? new Color(0.2f, 0.7f, 0.9f) : new Color(1f, 0.55f, 0.1f));
            statusRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}

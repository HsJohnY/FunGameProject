using UnityEngine;

namespace FunGame.Tools
{
    /// <summary>
    /// 密封喷枪语义样例，只验证工具匹配、动作词和完成反馈。
    /// 正式连续覆盖与泄漏状态推进在 M1-4 实现。
    /// </summary>
    public sealed class SealantTarget : MonoBehaviour, IToolTarget
    {
        [SerializeField] private string targetId = "sealant-demo-leak";
        [SerializeField] private string targetName = "泄漏点演示";
        [SerializeField] private Renderer statusRenderer;

        private MaterialPropertyBlock _propertyBlock;
        public bool IsSealed { get; private set; }

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
                "密封",
                ToolKind.SealantGun,
                toolbelt.EquippedTool,
                !IsSealed,
                "已完成演示密封");
        }

        public bool ApplyTool(PlayerToolbelt toolbelt)
        {
            if (toolbelt.EquippedTool != ToolKind.SealantGun || IsSealed)
            {
                return false;
            }

            IsSealed = true;
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
            _propertyBlock.SetColor("_BaseColor", IsSealed ? new Color(0.15f, 0.85f, 0.45f) : new Color(0.2f, 0.55f, 1f));
            statusRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}

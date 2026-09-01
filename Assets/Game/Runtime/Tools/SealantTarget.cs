using UnityEngine;
using FunGame.Incident;

namespace FunGame.Tools
{
    /// <summary>
    /// 密封喷枪语义样例，只验证工具匹配、动作词和完成反馈。
    /// 正式连续覆盖与泄漏状态推进在 M1-4 实现。
    /// </summary>
    public sealed class SealantTarget : MonoBehaviour, IToolTarget, IIncidentResettable
    {
        [SerializeField] private string targetId = "sealant-demo-leak";
        [SerializeField] private string targetName = "泄漏点演示";
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private CoolingIncidentController incident;
        [SerializeField, Min(0.1f)] private float sealDurationSeconds = 1.5f;

        private MaterialPropertyBlock _propertyBlock;
        public bool IsSealed { get; private set; }

        public void Configure(CoolingIncidentController incidentController)
        {
            incident = incidentController;
            incident?.RegisterResettable(this);
        }

        private void Start()
        {
            // 序列化只保存控制器引用，不保存控制器内部的运行时登记列表。
            incident?.RegisterResettable(this);
        }

        public void ResetIncidentState()
        {
            IsSealed = false;
            RefreshVisual();
        }

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
            bool phaseAvailable = incident == null || incident.Phase == CoolingIncidentPhase.ContainLeak;
            bool complete = incident != null
                ? incident.Phase != CoolingIncidentPhase.ContainLeak
                : IsSealed;
            return new ToolActionOption(
                targetId,
                targetName,
                "密封",
                ToolKind.SealantGun,
                toolbelt.EquippedTool,
                phaseAvailable && !complete,
                complete ? "泄漏已受到控制" : incident?.CurrentInstruction ?? "已完成密封");
        }

        public bool ApplyTool(PlayerToolbelt toolbelt)
        {
            if (toolbelt.EquippedTool != ToolKind.SealantGun || IsSealed)
            {
                return false;
            }

            if (incident != null)
            {
                if (!incident.AddSealProgress(Time.deltaTime / sealDurationSeconds))
                {
                    return false;
                }

                IsSealed = incident.Phase != CoolingIncidentPhase.ContainLeak;
            }
            else
            {
                IsSealed = true;
            }
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

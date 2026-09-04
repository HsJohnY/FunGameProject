using FunGame.Incident;
using UnityEngine;

namespace FunGame.Tools
{
    /// <summary>
    /// 线路桥接器的单人事故目标：检测端点、连接旁路并验证回路，完成后恢复密封作业权限。
    /// </summary>
    public sealed class CircuitBridgeTarget : MonoBehaviour, IToolTarget, IIncidentResettable
    {
        private static readonly string[] ActionLabels =
        {
            "检测端点",
            "连接旁路",
            "验证回路"
        };

        [SerializeField] private string targetId = "cooling-control-interlock";
        [SerializeField] private string targetName = "冷却控制联锁";
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private CoolingIncidentController incident;
        [SerializeField, Range(0, CoolingIncidentRules.RequiredCircuitBridgeSteps)] private int completedSteps;
        private MaterialPropertyBlock _propertyBlock;

        public int CompletedSteps => completedSteps;
        public int RequiredSteps => CoolingIncidentRules.RequiredCircuitBridgeSteps;
        public bool IsBridged => completedSteps >= RequiredSteps;

        public void Configure(CoolingIncidentController incidentController)
        {
            incident = incidentController;
            incident?.RegisterResettable(this);
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

        private void Start()
        {
            incident?.RegisterResettable(this);
        }

        public ToolActionOption GetToolAction(PlayerToolbelt toolbelt)
        {
            bool correctPhase = incident == null || incident.Phase == CoolingIncidentPhase.RestoreControlPower;
            string action = IsBridged ? "检查旁路" : ActionLabels[Mathf.Min(completedSteps, ActionLabels.Length - 1)];
            string blockedReason = correctPhase
                ? IsBridged ? "临时旁路已稳定" : string.Empty
                : incident.CurrentGuidance;
            return new ToolActionOption(
                targetId,
                targetName,
                action,
                ToolKind.CircuitBridger,
                toolbelt.EquippedTool,
                correctPhase && !IsBridged,
                blockedReason);
        }

        public bool ApplyTool(PlayerToolbelt toolbelt)
        {
            if (toolbelt.EquippedTool != ToolKind.CircuitBridger || IsBridged)
            {
                return false;
            }

            if (incident != null)
            {
                if (!incident.TryAdvanceCircuitBridge())
                {
                    return false;
                }

                completedSteps = incident.CircuitBridgeProgress;
            }
            else
            {
                completedSteps++;
            }

            RefreshVisual();
            Debug.Log($"[Tool] target={targetId} action=circuit-step progress={completedSteps}/{RequiredSteps}", this);
            return true;
        }

        public void ResetIncidentState()
        {
            completedSteps = 0;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (statusRenderer == null)
            {
                return;
            }

            float progress = (float)completedSteps / RequiredSteps;
            Color color = IsBridged
                ? new Color(0.15f, 0.95f, 0.75f)
                : Color.Lerp(new Color(0.75f, 0.12f, 0.7f), new Color(0.35f, 0.75f, 1f), progress);
            statusRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", color);
            statusRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}

using FunGame.Incident;
using UnityEngine;

namespace FunGame.Tools
{
    /// <summary>
    /// 线路桥接器的最小任务：依次检测端点、连接旁路并验证回路。
    /// </summary>
    public sealed class CircuitBridgeTarget : MonoBehaviour, IToolTarget, IIncidentResettable
    {
        private static readonly string[] ActionLabels =
        {
            "检测端点",
            "连接旁路",
            "验证回路"
        };

        [SerializeField] private string targetId = "circuit-bridge-demo";
        [SerializeField] private string targetName = "断路控制节点";
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private CoolingIncidentController incident;
        [SerializeField, Range(0, 3)] private int completedSteps;

        private MaterialPropertyBlock _propertyBlock;

        public int CompletedSteps => completedSteps;
        public int RequiredSteps => ActionLabels.Length;
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
            string action = IsBridged ? "检查旁路" : ActionLabels[completedSteps];
            return new ToolActionOption(
                targetId,
                targetName,
                action,
                ToolKind.CircuitBridger,
                toolbelt.EquippedTool,
                !IsBridged,
                "临时旁路已稳定");
        }

        public bool ApplyTool(PlayerToolbelt toolbelt)
        {
            if (toolbelt.EquippedTool != ToolKind.CircuitBridger || IsBridged)
            {
                return false;
            }

            completedSteps++;
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
                : Color.Lerp(new Color(0.7f, 0.12f, 0.2f), new Color(0.2f, 0.65f, 1f), progress);
            statusRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", color);
            statusRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}

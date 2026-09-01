using UnityEngine;
using FunGame.Incident;

namespace FunGame.Tools
{
    /// <summary>
    /// 冲击扳手语义样例，在“松开”和“紧固”之间切换机械连接状态。
    /// </summary>
    public sealed class MechanicalFastenerTarget : MonoBehaviour, IToolTarget, IIncidentResettable
    {
        [SerializeField] private string targetId = "mechanical-fastener";
        [SerializeField] private string targetName = "管件机械连接";
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private bool isTightened = true;
        [SerializeField] private CoolingIncidentController incident;

        private MaterialPropertyBlock _propertyBlock;

        public bool IsTightened => isTightened;

        public void Configure(CoolingIncidentController incidentController)
        {
            incident = incidentController;
            incident?.RegisterResettable(this);
        }

        private void Start()
        {
            // 场景重新加载后 Configure 不会再次执行，因此必须在运行时重新登记。
            incident?.RegisterResettable(this);
        }

        public void ResetIncidentState()
        {
            isTightened = true;
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
            string blockedReason = string.Empty;
            bool phaseAvailable = true;
            string action = isTightened ? "松开" : "紧固";
            if (incident != null)
            {
                bool expectsLoosen = incident.Phase == CoolingIncidentPhase.LoosenConnection;
                bool expectsTighten = incident.Phase == CoolingIncidentPhase.TightenConnection;
                phaseAvailable = expectsLoosen || expectsTighten;
                action = expectsTighten ? "紧固" : "松开";
                blockedReason = phaseAvailable ? string.Empty : incident.CurrentInstruction;
            }

            return new ToolActionOption(
                targetId,
                targetName,
                action,
                ToolKind.ImpactWrench,
                toolbelt.EquippedTool,
                phaseAvailable,
                blockedReason);
        }

        public bool ApplyTool(PlayerToolbelt toolbelt)
        {
            if (toolbelt.EquippedTool != ToolKind.ImpactWrench)
            {
                return false;
            }

            if (incident != null)
            {
                bool succeeded = incident.Phase == CoolingIncidentPhase.LoosenConnection
                    ? incident.TryLoosen()
                    : incident.TryTighten();
                if (!succeeded)
                {
                    return false;
                }
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

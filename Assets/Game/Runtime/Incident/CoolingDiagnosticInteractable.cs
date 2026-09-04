using FunGame.Interaction;
using UnityEngine;

namespace FunGame.Incident
{
    /// <summary>
    /// 让玩家通过场景中的压力表与泵体完成诊断和维修后验证，而不是由 HUD 直接公布答案。
    /// </summary>
    public sealed class CoolingDiagnosticInteractable : MonoBehaviour, IContextInteractable, IIncidentResettable
    {
        public enum DiagnosticKind
        {
            PressureGauge = 0,
            PumpHousing = 1
        }

        [SerializeField] private CoolingIncidentController incident;
        [SerializeField] private DiagnosticKind kind;
        [SerializeField] private Renderer statusRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private bool _subscribed;

        public DiagnosticKind Kind => kind;

        public void Configure(CoolingIncidentController incidentController, DiagnosticKind diagnosticKind)
        {
            Unsubscribe();
            incident = incidentController;
            kind = diagnosticKind;
            incident?.RegisterResettable(this);
            Subscribe();
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

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            incident?.RegisterResettable(this);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            if (incident == null)
            {
                return CreateOption("检查", false, "事故控制器未连接");
            }

            if (incident.RunState != CoolingIncidentRunState.Active)
            {
                return CreateOption("检查", false, "本轮事故已经结束");
            }

            if (kind == DiagnosticKind.PressureGauge)
            {
                if (incident.Phase == CoolingIncidentPhase.AssessSymptoms && !incident.HasInspectedPressure)
                {
                    return CreateOption("读取压力异常", true, string.Empty);
                }

                if (incident.Phase == CoolingIncidentPhase.VerifyPressure)
                {
                    return CreateOption("验证压力回升", true, string.Empty);
                }
            }
            else if (incident.Phase == CoolingIncidentPhase.AssessSymptoms && !incident.HasInspectedPump)
            {
                return CreateOption("检查泵体振动", true, string.Empty);
            }

            return CreateOption("检查", false, incident.CurrentGuidance);
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            bool accepted = kind == DiagnosticKind.PressureGauge
                ? incident != null && incident.TryInspectPressure()
                : incident != null && incident.TryInspectPump();
            RefreshVisual();
            return accepted;
        }

        public void ResetIncidentState()
        {
            RefreshVisual();
        }

        private InteractionOption CreateOption(string action, bool available, string reason)
        {
            return new InteractionOption(
                kind == DiagnosticKind.PressureGauge ? "cooling-pressure-gauge" : "cooling-pump-housing",
                kind == DiagnosticKind.PressureGauge ? "冷却压力表" : "冷却泵检查面板",
                action,
                InteractionPriority.Device,
                available,
                reason);
        }

        private void RefreshVisual()
        {
            if (statusRenderer == null || incident == null)
            {
                return;
            }

            bool inspected = kind == DiagnosticKind.PressureGauge
                ? incident.HasInspectedPressure
                : incident.HasInspectedPump;
            bool actionable = incident.Phase == CoolingIncidentPhase.AssessSymptoms && !inspected;
            if (kind == DiagnosticKind.PressureGauge && incident.Phase == CoolingIncidentPhase.VerifyPressure)
            {
                actionable = true;
            }

            Color color = actionable
                ? new Color(1f, 0.55f, 0.08f)
                : inspected
                    ? new Color(0.15f, 0.9f, 0.75f)
                    : new Color(0.2f, 0.45f, 0.55f);
            statusRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", color);
            statusRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void Subscribe()
        {
            if (_subscribed || incident == null)
            {
                return;
            }

            incident.StateChanged += RefreshVisual;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || incident == null)
            {
                return;
            }

            incident.StateChanged -= RefreshVisual;
            _subscribed = false;
        }
    }
}

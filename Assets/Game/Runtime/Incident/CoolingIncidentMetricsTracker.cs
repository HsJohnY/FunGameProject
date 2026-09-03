using FunGame.Interaction;
using FunGame.Tools;
using UnityEngine;

namespace FunGame.Incident
{
    /// <summary>
    /// 将通用交互层的规则拒绝事件汇总到当前事故，不让工具和交互模块反向依赖事故实现。
    /// </summary>
    public sealed class CoolingIncidentMetricsTracker : MonoBehaviour
    {
        [SerializeField] private CoolingIncidentController incident;
        [SerializeField] private ContextInteractor interactor;
        [SerializeField] private ToolController toolController;
        private bool _subscribed;

        public void Configure(
            CoolingIncidentController incidentController,
            ContextInteractor configuredInteractor,
            ToolController configuredToolController)
        {
            Unsubscribe();
            incident = incidentController;
            interactor = configuredInteractor;
            toolController = configuredToolController;
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || incident == null || interactor == null || toolController == null)
            {
                return;
            }

            interactor.InteractionRejected += HandleInteractionRejected;
            toolController.ToolActionRejected += HandleToolActionRejected;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (interactor != null)
            {
                interactor.InteractionRejected -= HandleInteractionRejected;
            }

            if (toolController != null)
            {
                toolController.ToolActionRejected -= HandleToolActionRejected;
            }

            _subscribed = false;
        }

        private void HandleInteractionRejected(string targetId, string reason)
        {
            incident?.RecordRejectedAction($"interaction:{targetId}", reason);
        }

        private void HandleToolActionRejected(string targetId, string reason)
        {
            incident?.RecordRejectedAction($"tool:{targetId}", reason);
        }
    }
}

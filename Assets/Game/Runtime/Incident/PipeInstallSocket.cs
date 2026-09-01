using FunGame.Interaction;
using UnityEngine;

namespace FunGame.Incident
{
    /// <summary>
    /// 接收唯一替换管件，并在正确事故阶段将其固定到管道接口。
    /// </summary>
    public sealed class PipeInstallSocket : MonoBehaviour, IContextInteractable, IIncidentResettable
    {
        [SerializeField] private CoolingIncidentController incident;
        [SerializeField] private Transform itemAnchor;
        [SerializeField] private string requiredItemId = "replacement-pipe";
        [SerializeField] private Transform recoveryPoint;
        private CarryableInteractable _installedItem;

        public void Configure(CoolingIncidentController incidentController, Transform anchor, Transform recovery = null)
        {
            incident = incidentController;
            itemAnchor = anchor;
            recoveryPoint = recovery;
            incident?.RegisterResettable(this);
        }

        private void Start()
        {
            incident?.RegisterResettable(this);
        }

        public void ResetIncidentState()
        {
            if (_installedItem != null)
            {
                _installedItem.RecoverTo(recoveryPoint != null ? recoveryPoint.position : transform.position);
                _installedItem = null;
            }
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            bool correctPhase = incident != null && incident.Phase == CoolingIncidentPhase.InstallReplacementPipe;
            return new InteractionOption(
                "replacement-pipe-socket",
                "损坏管件接口",
                "安装替换管件",
                InteractionPriority.Device,
                correctPhase && actor.IsHoldingItem,
                !correctPhase ? incident?.CurrentInstruction ?? "事故控制器未连接" : "需要携带替换管件");
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            if (incident == null || incident.Phase != CoolingIncidentPhase.InstallReplacementPipe)
            {
                return false;
            }

            if (!actor.TryInstallHeldItem(requiredItemId, itemAnchor != null ? itemAnchor : transform))
            {
                return false;
            }

            // 安装动作会消耗手持位，但保留引用用于失败/重置时找回任务物。
            _installedItem = FindInstalledItem(itemAnchor != null ? itemAnchor : transform);
            return incident.TryInstallPipe();
        }

        private static CarryableInteractable FindInstalledItem(Transform anchor)
        {
            return anchor != null ? anchor.GetComponentInChildren<CarryableInteractable>() : null;
        }
    }
}

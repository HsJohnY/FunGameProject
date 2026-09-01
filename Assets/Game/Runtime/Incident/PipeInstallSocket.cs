using FunGame.Interaction;
using UnityEngine;

namespace FunGame.Incident
{
    /// <summary>
    /// 接收唯一替换管件，并在正确事故阶段将其固定到管道接口。
    /// </summary>
    public sealed class PipeInstallSocket : MonoBehaviour, IContextInteractable
    {
        [SerializeField] private CoolingIncidentController incident;
        [SerializeField] private Transform itemAnchor;
        [SerializeField] private string requiredItemId = "replacement-pipe";

        public void Configure(CoolingIncidentController incidentController, Transform anchor)
        {
            incident = incidentController;
            itemAnchor = anchor;
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

            return incident.TryInstallPipe();
        }
    }
}

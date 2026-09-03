using FunGame.Tools;
using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// 通过玩家已注册的网络对象提交事故交互，避免关闭场景管理时使用场景 NetworkObject。
    /// </summary>
    [RequireComponent(typeof(NetworkObject), typeof(NetworkPlayerToolbelt), typeof(NetworkPlayerCarryController))]
    public sealed class NetworkPlayerIncidentAgent : NetworkBehaviour
    {
        private const float MaximumInteractionDistance = 4f;

        public bool RequestAction(NetworkIncidentAction action)
        {
            if (!IsSpawned || !IsOwner)
            {
                return false;
            }

            RequestActionRpc(action);
            return true;
        }

        [Rpc(SendTo.Server)]
        private void RequestActionRpc(NetworkIncidentAction action)
        {
            Vector3 stationPosition = NetworkIncidentLayout.GetStationPosition(action);
            if (Vector3.Distance(transform.position, stationPosition) > MaximumInteractionDistance)
            {
                return;
            }

            NetworkCoolingIncidentController incident = FindFirstObjectByType<NetworkCoolingIncidentController>();
            NetworkPlayerToolbelt toolbelt = GetComponent<NetworkPlayerToolbelt>();
            NetworkPlayerCarryController carryController = GetComponent<NetworkPlayerCarryController>();
            ToolKind equippedTool = toolbelt != null ? toolbelt.EquippedTool : ToolKind.None;
            bool hasReplacementPipe = carryController != null && carryController.IsHoldingItem("m3-shared-task-part");
            if (incident == null || !incident.IsActionAvailable(action, equippedTool, hasReplacementPipe))
            {
                return;
            }

            if (action == NetworkIncidentAction.InstallPipe
                && (carryController == null
                    || !carryController.TryInstallHeldItemServer(
                        "m3-shared-task-part",
                        stationPosition + new Vector3(0f, 0f, -0.8f))))
            {
                return;
            }

            incident.TryExecuteServer(action, equippedTool, hasReplacementPipe);
        }
    }
}

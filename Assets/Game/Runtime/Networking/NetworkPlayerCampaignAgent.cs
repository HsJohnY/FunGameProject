using FunGame.Tools;
using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>把本地工具命中转换为经过距离和工具验证的服务器请求。</summary>
    [RequireComponent(typeof(NetworkObject), typeof(NetworkPlayerToolbelt))]
    public sealed class NetworkPlayerCampaignAgent : NetworkBehaviour
    {
        private const float MaximumDistance = 4.5f;
        private float _nextCombatAction;

        public bool RequestRelay(int index)
        {
            if (!IsSpawned || !IsOwner) return false;
            RelayRpc(index);
            return true;
        }

        public bool RequestCalibration()
        {
            if (!IsSpawned || !IsOwner) return false;
            CalibrationRpc();
            return true;
        }

        public bool RequestEnemyHit(NetworkObjectReference enemy)
        {
            if (!IsSpawned || !IsOwner) return false;
            EnemyHitRpc(enemy);
            return true;
        }

        [Rpc(SendTo.Server)]
        private void RelayRpc(int index)
        {
            NetworkCampaignStation station = FindNearestStation(index, false);
            NetworkPlayerToolbelt belt = GetComponent<NetworkPlayerToolbelt>();
            if (station == null || Vector3.Distance(transform.position, station.transform.position) > MaximumDistance ||
                belt.EquippedTool != ToolKind.CircuitBridger) return;
            FindFirstObjectByType<NetworkCampaignController>()?.TryOperateRelayServer(index);
        }

        [Rpc(SendTo.Server)]
        private void CalibrationRpc()
        {
            NetworkCampaignStation station = FindNearestStation(0, true);
            if (station == null || Vector3.Distance(transform.position, station.transform.position) > MaximumDistance) return;
            FindFirstObjectByType<NetworkCampaignController>()?.ConfirmStormWaveServer();
        }

        [Rpc(SendTo.Server)]
        private void EnemyHitRpc(NetworkObjectReference reference)
        {
            if (!reference.TryGet(out NetworkObject target) || !target.TryGetComponent(out NetworkCombatEnemy enemy) ||
                Vector3.Distance(transform.position, enemy.transform.position) > MaximumDistance) return;
            ToolKind tool = GetComponent<NetworkPlayerToolbelt>().EquippedTool;
            if (!NetworkPlayerToolbelt.IsSupportedTool(tool) || Time.time < _nextCombatAction) return;
            _nextCombatAction = Time.time + (tool == ToolKind.ImpactWrench ? 0.38f : tool == ToolKind.CircuitBridger ? 0.8f : 0.15f);
            enemy.ApplyToolServer(tool, transform.position);
        }

        private static NetworkCampaignStation FindNearestStation(int index, bool console)
        {
            NetworkCampaignStation best = null;
            float distance = float.MaxValue;
            foreach (NetworkCampaignStation station in FindObjectsByType<NetworkCampaignStation>(FindObjectsSortMode.None))
            {
                if (station.IsCalibrationConsole != console || (!console && station.StationIndex != index)) continue;
                float candidate = Vector3.SqrMagnitude(station.transform.position);
                if (candidate < distance) { best = station; distance = candidate; }
            }
            return best;
        }
    }
}

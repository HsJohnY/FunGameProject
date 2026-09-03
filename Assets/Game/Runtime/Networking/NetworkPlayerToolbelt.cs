using FunGame.Tools;
using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// 同步单个玩家的主工具位。客户端拥有者只提交请求，最终状态由服务器写入。
    /// </summary>
    [RequireComponent(typeof(NetworkObject), typeof(PlayerToolbelt))]
    public sealed class NetworkPlayerToolbelt : NetworkBehaviour
    {
        private readonly NetworkVariable<ToolKind> equippedTool = new NetworkVariable<ToolKind>(
            ToolKind.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private PlayerToolbelt _localToolbelt;

        public ToolKind EquippedTool => equippedTool.Value;

        private void Awake()
        {
            _localToolbelt = GetComponent<PlayerToolbelt>();
        }

        public override void OnNetworkSpawn()
        {
            equippedTool.OnValueChanged += HandleToolChanged;
            ApplyTool(equippedTool.Value);
        }

        public override void OnNetworkDespawn()
        {
            equippedTool.OnValueChanged -= HandleToolChanged;
            ApplyTool(ToolKind.None);
        }

        public bool RequestToggleTool(ToolKind requestedTool)
        {
            if (!IsSpawned || !IsOwner || !IsSupportedTool(requestedTool))
            {
                return false;
            }

            ToggleToolRpc(requestedTool);
            return true;
        }

        public static bool IsSupportedTool(ToolKind tool)
        {
            return tool == ToolKind.ImpactWrench || tool == ToolKind.SealantGun;
        }

        [Rpc(SendTo.Server)]
        private void ToggleToolRpc(ToolKind requestedTool)
        {
            if (!IsSupportedTool(requestedTool))
            {
                return;
            }

            equippedTool.Value = equippedTool.Value == requestedTool
                ? ToolKind.None
                : requestedTool;
        }

        private void HandleToolChanged(ToolKind previousTool, ToolKind currentTool)
        {
            ApplyTool(currentTool);
        }

        private void ApplyTool(ToolKind tool)
        {
            if (_localToolbelt == null)
            {
                return;
            }

            if (tool == ToolKind.None)
            {
                _localToolbelt.Unequip();
                return;
            }

            if (_localToolbelt.EquippedTool != tool)
            {
                _localToolbelt.Equip(tool);
            }
        }
    }
}

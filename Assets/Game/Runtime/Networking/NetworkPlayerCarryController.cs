using FunGame.Interaction;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FunGame.Networking
{
    /// <summary>
    /// 玩家侧共享任务物入口。拥有者读取 Q，服务器验证并执行拾取或物理抛放。
    /// </summary>
    [RequireComponent(typeof(NetworkObject), typeof(ContextInteractor))]
    public sealed class NetworkPlayerCarryController : NetworkBehaviour
    {
        private const float MaxPickupDistance = 4f;
        private const float ThrowForwardImpulse = 4.5f;
        private const float ThrowUpwardImpulse = 1f;

        [SerializeField] private Camera viewCamera;

        private readonly NetworkVariable<NetworkObjectReference> heldItem = new NetworkVariable<NetworkObjectReference>(
            new NetworkObjectReference((NetworkObject)null),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private InputAction _dropAction;

        public bool HasHeldItem => heldItem.Value.TryGet(out _, NetworkManager);

        private void Awake()
        {
            if (viewCamera == null)
            {
                viewCamera = GetComponentInChildren<Camera>(true);
            }

            _dropAction = new InputAction("抛放共享物品", InputActionType.Button, "<Keyboard>/q");
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _dropAction.Enable();
            }
        }

        public override void OnNetworkDespawn()
        {
            _dropAction.Disable();
            if (IsServer)
            {
                DropHeldItemServer(transform.forward);
            }
        }

        private void OnDestroy()
        {
            _dropAction?.Dispose();
        }

        private void Update()
        {
            if (IsOwner && _dropAction.WasPressedThisFrame())
            {
                RequestDrop();
            }
        }

        public bool RequestPickup(NetworkCarryableItem item)
        {
            if (!IsSpawned || !IsOwner || HasHeldItem || item == null || !item.IsSpawned)
            {
                return false;
            }

            RequestPickupRpc(new NetworkObjectReference(item.NetworkObject));
            return true;
        }

        public bool RequestDrop()
        {
            if (!IsSpawned || !IsOwner || !HasHeldItem || viewCamera == null)
            {
                return false;
            }

            RequestDropRpc(viewCamera.transform.forward);
            return true;
        }

        public bool IsHoldingItem(string requiredTargetId)
        {
            return heldItem.Value.TryGet(out NetworkObject itemObject, NetworkManager)
                && itemObject.TryGetComponent(out NetworkCarryableItem item)
                && item.TargetId == requiredTargetId;
        }

        public bool TryInstallHeldItemServer(string requiredTargetId, Vector3 installPosition)
        {
            if (!IsServer
                || !heldItem.Value.TryGet(out NetworkObject itemObject, NetworkManager)
                || !itemObject.TryGetComponent(out NetworkCarryableItem item)
                || item.TargetId != requiredTargetId)
            {
                return false;
            }

            if (!item.InstallServer(installPosition))
            {
                return false;
            }

            heldItem.Value = new NetworkObjectReference((NetworkObject)null);
            return true;
        }

        public void ClearHeldItemServer(NetworkObject expectedItem)
        {
            if (!IsServer || !heldItem.Value.TryGet(out NetworkObject currentItem, NetworkManager))
            {
                return;
            }

            if (expectedItem == null || currentItem == expectedItem)
            {
                heldItem.Value = new NetworkObjectReference((NetworkObject)null);
            }
        }

        public static bool IsValidThrowDirection(Vector3 direction)
        {
            return float.IsFinite(direction.x)
                && float.IsFinite(direction.y)
                && float.IsFinite(direction.z)
                && direction.sqrMagnitude > 0.25f;
        }

        [Rpc(SendTo.Server)]
        private void RequestPickupRpc(NetworkObjectReference itemReference)
        {
            if (HasHeldItem || !itemReference.TryGet(out NetworkObject itemObject, NetworkManager))
            {
                return;
            }

            NetworkCarryableItem item = itemObject.GetComponent<NetworkCarryableItem>();
            if (item == null || item.IsHeld || Vector3.Distance(transform.position, item.transform.position) > MaxPickupDistance)
            {
                return;
            }

            if (item.TryHoldServer(NetworkObject))
            {
                heldItem.Value = itemReference;
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestDropRpc(Vector3 requestedDirection)
        {
            DropHeldItemServer(requestedDirection);
        }

        private void DropHeldItemServer(Vector3 requestedDirection)
        {
            if (!IsServer || !heldItem.Value.TryGet(out NetworkObject itemObject, NetworkManager))
            {
                heldItem.Value = new NetworkObjectReference((NetworkObject)null);
                return;
            }

            NetworkCarryableItem item = itemObject.GetComponent<NetworkCarryableItem>();
            Vector3 direction = IsValidThrowDirection(requestedDirection)
                ? requestedDirection.normalized
                : transform.forward;
            Vector3 dropPosition = transform.position + Vector3.up * 0.5f + direction * 1.2f;
            Vector3 impulse = CarryThrowMath.CalculateImpulse(direction, ThrowForwardImpulse, ThrowUpwardImpulse);
            heldItem.Value = new NetworkObjectReference((NetworkObject)null);
            item?.DropServer(dropPosition, impulse);
        }
    }
}

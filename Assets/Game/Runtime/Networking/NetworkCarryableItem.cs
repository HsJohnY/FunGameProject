using FunGame.Interaction;
using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// 由服务器拥有的共享任务物。服务器统一处理占用、父子关系和物理抛放。
    /// </summary>
    [RequireComponent(typeof(NetworkObject), typeof(Rigidbody), typeof(Collider))]
    public sealed class NetworkCarryableItem : NetworkBehaviour, IContextInteractable
    {
        [SerializeField] private string targetId = "network-task-part";
        [SerializeField] private string targetName = "共享替换管件";
        [SerializeField] private Vector3 heldLocalPosition = new Vector3(0.35f, 0.35f, 0.8f);
        [SerializeField] private Vector3 heldLocalEuler = new Vector3(12f, -18f, 0f);
        [SerializeField, Range(0.1f, 1f)] private float heldScaleMultiplier = 0.45f;

        private readonly NetworkVariable<NetworkObjectReference> holder = new NetworkVariable<NetworkObjectReference>(
            new NetworkObjectReference((NetworkObject)null),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private Rigidbody _body;
        private Collider _interactionCollider;
        private Vector3 _worldScale;
        private Vector3 _spawnPosition;

        public string TargetId => targetId;
        public bool IsHeld => holder.Value.TryGet(out _, NetworkManager);

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _interactionCollider = GetComponent<Collider>();
            _worldScale = transform.lossyScale;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _spawnPosition = transform.position;
            }

            holder.OnValueChanged += HandleHolderChanged;
            ApplyHeldPresentation(IsHeld);
        }

        public override void OnNetworkDespawn()
        {
            holder.OnValueChanged -= HandleHolderChanged;
        }

        private void Update()
        {
            if (IsServer && !IsHeld && transform.position.y < NetworkPlayerSpawnLayout.FallResetHeight)
            {
                RecoverServer();
            }
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            NetworkPlayerCarryController carryController = actor.GetComponent<NetworkPlayerCarryController>();
            bool available = carryController != null && carryController.IsOwner && !carryController.HasHeldItem && !IsHeld;
            string reason = IsHeld ? "物品正由其他玩家携带" : "手中已有物品";
            return new InteractionOption(
                targetId,
                targetName,
                "拾取",
                InteractionPriority.Pickup,
                available,
                available ? string.Empty : reason);
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            NetworkPlayerCarryController carryController = actor.GetComponent<NetworkPlayerCarryController>();
            return carryController != null && carryController.RequestPickup(this);
        }

        public void ConfigureIdentity(string id, string displayName)
        {
            targetId = id;
            targetName = displayName;
        }

        public bool TryHoldServer(NetworkObject playerObject)
        {
            if (!IsServer || IsHeld || playerObject == null || !playerObject.IsSpawned)
            {
                return false;
            }

            _body.isKinematic = true;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _interactionCollider.enabled = false;
            if (!NetworkObject.TrySetParent(playerObject, false))
            {
                _interactionCollider.enabled = true;
                _body.isKinematic = false;
                return false;
            }

            transform.localPosition = heldLocalPosition;
            transform.localRotation = Quaternion.Euler(heldLocalEuler);
            transform.localScale = _worldScale * heldScaleMultiplier;
            holder.Value = new NetworkObjectReference(playerObject);
            return true;
        }

        public void DropServer(Vector3 worldPosition, Vector3 impulse)
        {
            if (!IsServer)
            {
                return;
            }

            holder.Value = new NetworkObjectReference((NetworkObject)null);
            NetworkObject.TryRemoveParent(true);
            transform.SetPositionAndRotation(worldPosition, Quaternion.identity);
            transform.localScale = _worldScale;
            _interactionCollider.enabled = true;
            _body.isKinematic = false;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.AddForce(impulse, ForceMode.Impulse);
        }

        private void RecoverServer()
        {
            transform.SetPositionAndRotation(_spawnPosition, Quaternion.identity);
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
        }

        private void HandleHolderChanged(NetworkObjectReference previous, NetworkObjectReference current)
        {
            ApplyHeldPresentation(current.TryGet(out _, NetworkManager));
        }

        private void ApplyHeldPresentation(bool held)
        {
            if (_interactionCollider != null)
            {
                _interactionCollider.enabled = !held;
            }
        }
    }
}

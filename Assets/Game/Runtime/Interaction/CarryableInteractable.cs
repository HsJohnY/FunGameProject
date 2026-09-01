using UnityEngine;

namespace FunGame.Interaction
{
    /// <summary>
    /// M1-2 的轻型可携带占位物，用于验证拾取、单一手持位和丢下语义。
    /// </summary>
    public sealed class CarryableInteractable : MonoBehaviour, IContextInteractable
    {
        [SerializeField] private string targetId = "sample-part";
        [SerializeField] private string targetName = "测试管件";
        [SerializeField] private Collider interactionCollider;
        [SerializeField] private Rigidbody itemBody;
        [SerializeField, Range(0.1f, 1f)] private float heldScaleMultiplier = 0.45f;

        private Vector3 _worldScaleBeforePickup;

        public string TargetId => targetId;
        public bool IsHeld { get; private set; }

        public void ConfigureIdentity(string id, string displayName)
        {
            targetId = id;
            targetName = displayName;
        }

        private void Awake()
        {
            if (interactionCollider == null)
            {
                interactionCollider = GetComponent<Collider>();
            }

            if (itemBody == null)
            {
                itemBody = GetComponent<Rigidbody>();
            }
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            bool available = !actor.IsHoldingItem;
            return new InteractionOption(
                targetId,
                targetName,
                "拾取",
                InteractionPriority.Pickup,
                available,
                available ? string.Empty : "手中已有物品");
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            return actor.TryPickup(this);
        }

        /// <summary>
        /// 切换为手持表现；关闭碰撞，避免物品持续推挤玩家控制器。
        /// </summary>
        public void SetHeld(Transform anchor)
        {
            IsHeld = true;
            _worldScaleBeforePickup = transform.lossyScale;
            if (itemBody != null)
            {
                itemBody.isKinematic = true;
                itemBody.linearVelocity = Vector3.zero;
                itemBody.angularVelocity = Vector3.zero;
            }

            interactionCollider.enabled = false;
            transform.SetParent(anchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(12f, -18f, 0f);
            // 携带物使用缩小后的第一人称表现，避免与左侧主工具模型穿插并遮挡视野。
            transform.localScale = _worldScaleBeforePickup * heldScaleMultiplier;
        }

        /// <summary>
        /// 恢复为场景物体并重新启用物理与交互碰撞。
        /// </summary>
        public void SetDropped(Vector3 worldPosition, Vector3 impulse = default)
        {
            IsHeld = false;
            transform.SetParent(null, true);
            transform.position = worldPosition;
            transform.localScale = _worldScaleBeforePickup == Vector3.zero
                ? transform.localScale
                : _worldScaleBeforePickup;
            interactionCollider.enabled = true;
            if (itemBody != null)
            {
                itemBody.isKinematic = false;
                itemBody.AddForce(impulse, ForceMode.Impulse);
            }
        }

        /// <summary>
        /// 将任务物安全恢复到指定位置，并清除掉落前的速度。
        /// </summary>
        public void RecoverTo(Vector3 worldPosition)
        {
            SetDropped(worldPosition);
            if (itemBody != null)
            {
                itemBody.linearVelocity = Vector3.zero;
                itemBody.angularVelocity = Vector3.zero;
            }
        }
    }
}

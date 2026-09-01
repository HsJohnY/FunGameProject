using UnityEngine;

namespace FunGame.Interaction
{
    /// <summary>
    /// 当关键管件跌出灰盒有效区域时，将其送回可见恢复点，防止永久卡关。
    /// </summary>
    [RequireComponent(typeof(CarryableInteractable))]
    public sealed class TaskItemRecovery : MonoBehaviour
    {
        [SerializeField] private Transform recoveryPoint;
        [SerializeField] private float minimumWorldHeight = -3f;

        private CarryableInteractable _item;

        public int RecoveryCount { get; private set; }

        public void Configure(Transform point, float minimumHeight)
        {
            recoveryPoint = point;
            minimumWorldHeight = minimumHeight;
        }

        private void Awake()
        {
            _item = GetComponent<CarryableInteractable>();
        }

        private void Update()
        {
            if (_item.IsHeld || recoveryPoint == null || transform.position.y >= minimumWorldHeight)
            {
                return;
            }

            _item.RecoverTo(recoveryPoint.position);
            RecoveryCount++;
            Debug.Log($"[Recovery] target={_item.TargetId} result=recovered count={RecoveryCount}", this);
        }
    }
}

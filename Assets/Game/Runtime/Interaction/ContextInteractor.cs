using System.Collections.Generic;
using FunGame.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FunGame.Interaction
{
    /// <summary>
    /// 从第一人称摄像机发射准星射线，选择动作并适配交互与丢下输入。
    /// </summary>
    public sealed class ContextInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform holdAnchor;
        [SerializeField] private FirstPersonController playerController;
        [SerializeField, Min(0.5f)] private float interactionRange = 3f;
        [SerializeField, Range(0f, 0.3f)] private float aimAssistRadius = 0.16f;
        [SerializeField, Range(0f, 0.5f)] private float targetRetentionSeconds = 0.12f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private readonly List<MonoBehaviour> _componentBuffer = new List<MonoBehaviour>(8);
        private InputAction _interactAction;
        private InputAction _dropAction;
        private IContextInteractable _currentInteractable;
        private InteractionOption? _currentOption;
        private CarryableInteractable _heldItem;
        private float _lastTargetSeenAt = float.NegativeInfinity;

        public InteractionOption? CurrentOption => _currentOption;
        public bool IsHoldingItem => _heldItem != null;

        private void Awake()
        {
            if (viewCamera == null)
            {
                viewCamera = GetComponentInChildren<Camera>(true);
            }

            if (viewCamera == null)
            {
                Debug.LogError("[Interaction] 上下文交互器缺少第一人称摄像机。", this);
                enabled = false;
                return;
            }

            if (playerController == null)
            {
                playerController = GetComponent<FirstPersonController>();
            }

            if (holdAnchor == null)
            {
                var anchorObject = new GameObject("Held Item Anchor");
                holdAnchor = anchorObject.transform;
                holdAnchor.SetParent(viewCamera.transform, false);
                holdAnchor.localPosition = new Vector3(0.35f, -0.28f, 0.8f);
            }

            _interactAction = new InputAction("上下文交互", InputActionType.Button, "<Keyboard>/e");
            _dropAction = new InputAction("丢下或取消", InputActionType.Button, "<Keyboard>/q");
        }

        private void OnEnable()
        {
            _interactAction?.Enable();
            _dropAction?.Enable();
        }

        private void OnDisable()
        {
            _interactAction?.Disable();
            _dropAction?.Disable();
        }

        private void OnDestroy()
        {
            _interactAction?.Dispose();
            _dropAction?.Dispose();
        }

        private void Update()
        {
            if (playerController != null && !playerController.IsCursorLocked)
            {
                ClearTarget();
                return;
            }

            RefreshTarget();

            if (_interactAction.WasPressedThisFrame())
            {
                ExecuteCurrentInteraction();
            }

            if (_dropAction.WasPressedThisFrame())
            {
                DropHeldItem();
            }
        }

        /// <summary>
        /// 重新执行一次准星检测，供自动化测试和调试工具显式调用。
        /// </summary>
        public void RefreshTarget()
        {
            Ray ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
            bool hasHit = Physics.SphereCast(
                ray,
                aimAssistRadius,
                out RaycastHit hit,
                interactionRange,
                interactionMask,
                QueryTriggerInteraction.Ignore);

            if (!hasHit)
            {
                // 只在准星短暂落空时保持目标；若命中墙壁等其他表面，会在下方立即清除。
                if (_currentInteractable == null || Time.unscaledTime - _lastTargetSeenAt > targetRetentionSeconds)
                {
                    ClearTarget();
                }

                return;
            }

            // 只读取最先命中表面上的组件，防止跨越遮挡物选择远处目标。
            _componentBuffer.Clear();
            hit.collider.GetComponentsInParent(true, _componentBuffer);
            IContextInteractable bestInteractable = null;
            InteractionOption? bestOption = null;
            foreach (MonoBehaviour component in _componentBuffer)
            {
                if (!(component is IContextInteractable interactable))
                {
                    continue;
                }

                InteractionOption option = interactable.GetInteractionOption(this);
                if (!bestOption.HasValue || InteractionSelection.IsBetter(option, bestOption.Value))
                {
                    bestInteractable = interactable;
                    bestOption = option;
                }
            }

            if (bestInteractable == null)
            {
                // 已明确命中非交互表面时不使用保持时间，防止隔着遮挡物误操作。
                ClearTarget();
                return;
            }

            _currentInteractable = bestInteractable;
            _currentOption = bestOption;
            _lastTargetSeenAt = Time.unscaledTime;
        }

        /// <summary>
        /// 执行当前已显示的动作；条件不足时只记录原因，不尝试低优先级动作。
        /// </summary>
        public bool ExecuteCurrentInteraction()
        {
            if (_currentInteractable == null || !_currentOption.HasValue)
            {
                return false;
            }

            InteractionOption option = _currentOption.Value;
            if (!option.IsAvailable)
            {
                Debug.Log($"[Interaction] target={option.TargetId} blocked={option.UnavailableReason}", this);
                return false;
            }

            bool succeeded = _currentInteractable.ExecuteInteraction(this);
            Debug.Log($"[Interaction] target={option.TargetId} action={option.ActionLabel} success={succeeded}", this);
            RefreshTarget();
            return succeeded;
        }

        /// <summary>
        /// 尝试把一个轻型物体放入唯一手持位。
        /// </summary>
        public bool TryPickup(CarryableInteractable item)
        {
            if (_heldItem != null || item == null)
            {
                return false;
            }

            _heldItem = item;
            item.SetHeld(holdAnchor);
            return true;
        }

        /// <summary>
        /// 将当前手持物放到玩家前方；没有手持物时不产生副作用。
        /// </summary>
        public bool DropHeldItem()
        {
            if (_heldItem == null)
            {
                return false;
            }

            CarryableInteractable item = _heldItem;
            _heldItem = null;
            Vector3 dropPosition = viewCamera.transform.position + viewCamera.transform.forward * 1.2f;
            item.SetDropped(dropPosition);
            Debug.Log($"[Interaction] target={item.TargetId} action=drop success=True", this);
            return true;
        }

        private void ClearTarget()
        {
            _currentInteractable = null;
            _currentOption = null;
            _lastTargetSeenAt = float.NegativeInfinity;
        }
    }
}

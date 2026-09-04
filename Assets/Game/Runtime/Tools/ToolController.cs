using System;
using System.Collections.Generic;
using FunGame.Interaction;
using UnityEngine;
using FunGame.Content;
using UnityEngine.InputSystem;

namespace FunGame.Tools
{
    /// <summary>
    /// 将鼠标左键适配为当前工具的主要功能，并报告具体动作或错误工具原因。
    /// </summary>
    [RequireComponent(typeof(ContextInteractor), typeof(PlayerToolbelt))]
    public sealed class ToolController : MonoBehaviour
    {
        private readonly List<MonoBehaviour> _componentBuffer = new List<MonoBehaviour>(8);
        private ContextInteractor _interactor;
        private PlayerToolbelt _toolbelt;
        private InputAction _primaryAction;
        private IToolTarget _currentTarget;
        [SerializeField, Min(0.05f)] private float impactWrenchCooldownSeconds = 0.38f;
        [SerializeField, Min(0.05f)] private float circuitBridgerCooldownSeconds = 0.8f;
        [SerializeField] private ToolDefinition wrenchDefinition;
        [SerializeField] private ToolDefinition bridgerDefinition;
        public void ConfigureDefinitions(ToolDefinition wrench, ToolDefinition bridger)
        {
            wrenchDefinition = wrench; bridgerDefinition = bridger;
        }
        private float _nextImpactWrenchActionTime;
        private float _nextCircuitBridgerActionTime;

        public ToolActionOption? CurrentOption { get; private set; }
        public event Action<ToolActionFeedback> ToolActionExecuted;
        public event Action<string, string> ToolActionRejected;
        public float ImpactWrenchCooldownRemaining => Mathf.Max(0f, _nextImpactWrenchActionTime - Time.unscaledTime);
        public float CircuitBridgerCooldownRemaining => Mathf.Max(0f, _nextCircuitBridgerActionTime - Time.unscaledTime);

        private void Awake()
        {
            _interactor = GetComponent<ContextInteractor>();
            _toolbelt = GetComponent<PlayerToolbelt>();
            _primaryAction = new InputAction("工具主要功能", InputActionType.Button, "<Mouse>/leftButton");
        }

        private void OnEnable()
        {
            _primaryAction?.Enable();
        }

        private void OnDisable()
        {
            _primaryAction?.Disable();
        }

        private void OnDestroy()
        {
            _primaryAction?.Dispose();
        }

        private void Update()
        {
            RefreshTarget();
            // 密封喷枪需要持续按住形成覆盖；其他工具仍保持单次点击语义。
            bool continuousSeal = _toolbelt.EquippedTool == ToolKind.SealantGun && _primaryAction.IsPressed();
            if (_primaryAction.WasPressedThisFrame() || continuousSeal)
            {
                ExecuteCurrentToolAction();
            }
        }

        public void RefreshTarget()
        {
            _currentTarget = null;
            CurrentOption = null;
            if (!_interactor.TryGetAimHit(out RaycastHit hit))
            {
                return;
            }

            _componentBuffer.Clear();
            hit.collider.GetComponentsInParent(true, _componentBuffer);
            foreach (MonoBehaviour component in _componentBuffer)
            {
                if (component.isActiveAndEnabled && component is IToolTarget target)
                {
                    _currentTarget = target;
                    CurrentOption = target.GetToolAction(_toolbelt);
                    return;
                }
            }
        }

        public bool ExecuteCurrentToolAction()
        {
            if (_currentTarget == null || !CurrentOption.HasValue)
            {
                return false;
            }

            ToolActionOption option = CurrentOption.Value;
            if (option.EquippedTool == ToolKind.ImpactWrench && ImpactWrenchCooldownRemaining > 0f)
            {
                return false;
            }

            if (option.EquippedTool == ToolKind.CircuitBridger && CircuitBridgerCooldownRemaining > 0f)
            {
                return false;
            }

            if (!option.IsAvailable)
            {
                ToolActionRejected?.Invoke(option.TargetId, option.BlockedReason);
                Debug.Log($"[Tool] target={option.TargetId} blocked={option.BlockedReason}", this);
                return false;
            }

            bool succeeded = _currentTarget.ApplyTool(_toolbelt);
            if (option.EquippedTool == ToolKind.ImpactWrench)
            {
                _nextImpactWrenchActionTime = Time.unscaledTime + (wrenchDefinition != null ? wrenchDefinition.CooldownSeconds : impactWrenchCooldownSeconds);
            }
            else if (option.EquippedTool == ToolKind.CircuitBridger)
            {
                _nextCircuitBridgerActionTime = Time.unscaledTime + (bridgerDefinition != null ? bridgerDefinition.CooldownSeconds : circuitBridgerCooldownSeconds);
            }

            ToolActionExecuted?.Invoke(new ToolActionFeedback(
                option.EquippedTool,
                _currentTarget as MonoBehaviour,
                succeeded));
            Debug.Log($"[Tool] target={option.TargetId} action={option.ActionLabel} tool={option.EquippedTool} success={succeeded}", this);
            RefreshTarget();
            return succeeded;
        }

        public void ConfigureImpactWrenchCooldown(float seconds)
        {
            impactWrenchCooldownSeconds = Mathf.Max(0.05f, seconds);
        }

        public void ConfigureCircuitBridgerCooldown(float seconds)
        {
            circuitBridgerCooldownSeconds = Mathf.Max(0.05f, seconds);
        }
    }
}

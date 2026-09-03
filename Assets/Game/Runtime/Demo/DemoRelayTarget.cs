using System;
using FunGame.Tools;
using UnityEngine;

namespace FunGame.Demo
{
    /// <summary>
    /// 第二章复用线路桥接器的继电器变式；三次离散动作分别代表扫描、旁路与锁定。
    /// </summary>
    public sealed class DemoRelayTarget : MonoBehaviour, IToolTarget
    {
        private static readonly string[] Actions = { "扫描继电器", "建立旁路", "锁定相位" };

        [SerializeField] private string targetId = "storm-relay";
        [SerializeField] private string targetName = "风暴继电器";
        [SerializeField] private Renderer statusRenderer;
        [SerializeField, Range(0, 3)] private int completedSteps;
        [SerializeField] private bool chapterActive;
        private MaterialPropertyBlock _propertyBlock;

        public event Action<DemoRelayTarget> Stabilized;
        public bool IsStabilized => completedSteps >= Actions.Length;
        public int CompletedSteps => completedSteps;

        public void Configure(string id, string displayName)
        {
            targetId = id;
            targetName = displayName;
        }

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            if (statusRenderer == null)
            {
                statusRenderer = GetComponent<Renderer>();
            }

            RefreshVisual();
        }

        public void SetChapterActive(bool active, bool reset)
        {
            chapterActive = active;
            if (reset)
            {
                completedSteps = 0;
            }

            RefreshVisual();
        }

        public ToolActionOption GetToolAction(PlayerToolbelt toolbelt)
        {
            string action = IsStabilized ? "检查稳定状态" : Actions[Mathf.Min(completedSteps, Actions.Length - 1)];
            return new ToolActionOption(
                targetId,
                targetName,
                action,
                ToolKind.CircuitBridger,
                toolbelt.EquippedTool,
                chapterActive && !IsStabilized,
                !chapterActive ? "当前章节尚未启用该继电器" : "继电器已经稳定");
        }

        public bool ApplyTool(PlayerToolbelt toolbelt)
        {
            if (!GetToolAction(toolbelt).IsAvailable)
            {
                return false;
            }

            completedSteps++;
            RefreshVisual();
            if (IsStabilized)
            {
                Stabilized?.Invoke(this);
            }

            Debug.Log($"[Demo] relay={targetId} progress={completedSteps}/{Actions.Length}", this);
            return true;
        }

        private void RefreshVisual()
        {
            if (statusRenderer == null)
            {
                return;
            }

            float ratio = (float)completedSteps / Actions.Length;
            Color color = !chapterActive
                ? new Color(0.16f, 0.18f, 0.22f)
                : IsStabilized
                    ? new Color(0.1f, 0.95f, 0.65f)
                    : Color.Lerp(new Color(0.75f, 0.12f, 0.7f), new Color(0.25f, 0.75f, 1f), ratio);
            statusRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", color);
            statusRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}

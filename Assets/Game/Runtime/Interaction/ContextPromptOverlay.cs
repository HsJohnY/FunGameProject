using UnityEngine;
using FunGame.Tools;

namespace FunGame.Interaction
{
    /// <summary>
    /// 使用最小 IMGUI 显示准星、目标名称、动作和阻塞原因。
    /// 正式 UI 系统确定后替换本组件，不改变交互规则接口。
    /// </summary>
    [RequireComponent(typeof(ContextInteractor))]
    public sealed class ContextPromptOverlay : MonoBehaviour
    {
        private ContextInteractor _interactor;
        private ToolController _toolController;
        private GUIStyle _crosshairStyle;
        private GUIStyle _promptStyle;

        private void Awake()
        {
            _interactor = GetComponent<ContextInteractor>();
            _toolController = GetComponent<ToolController>();
        }

        private void OnGUI()
        {
            EnsureStyles();

            var crosshairRect = new Rect(Screen.width * 0.5f - 15f, Screen.height * 0.5f - 18f, 30f, 36f);
            GUI.Label(crosshairRect, "+", _crosshairStyle);

            if (_interactor.CurrentOption.HasValue)
            {
                InteractionOption option = _interactor.CurrentOption.Value;
                string prompt = option.IsAvailable
                    ? $"[E] {option.ActionLabel} · {option.TargetName}"
                    : $"{option.TargetName} · {option.UnavailableReason}";
                DrawPrompt(prompt, option.IsAvailable, Screen.height * 0.5f + 28f);
            }

            if (_toolController != null && _toolController.CurrentOption.HasValue)
            {
                ToolActionOption option = _toolController.CurrentOption.Value;
                string prompt = option.IsAvailable
                    ? $"[左键] {option.ActionLabel} · {option.TargetName}"
                    : $"{option.TargetName} · {option.BlockedReason}";
                DrawPrompt(prompt, option.IsAvailable, Screen.height * 0.5f + 60f);
            }
        }

        private void EnsureStyles()
        {
            if (_crosshairStyle != null)
            {
                return;
            }

            _crosshairStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24
            };
            _promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
        }

        private void DrawPrompt(string prompt, bool isAvailable, float verticalPosition)
        {
            _promptStyle.normal.textColor = isAvailable ? Color.white : new Color(1f, 0.55f, 0.25f);
            var promptRect = new Rect(0f, verticalPosition, Screen.width, 40f);
            GUI.Label(promptRect, prompt, _promptStyle);
        }
    }
}

using UnityEngine;

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
        private GUIStyle _crosshairStyle;
        private GUIStyle _promptStyle;

        private void Awake()
        {
            _interactor = GetComponent<ContextInteractor>();
        }

        private void OnGUI()
        {
            EnsureStyles();

            var crosshairRect = new Rect(Screen.width * 0.5f - 15f, Screen.height * 0.5f - 18f, 30f, 36f);
            GUI.Label(crosshairRect, "+", _crosshairStyle);

            if (!_interactor.CurrentOption.HasValue)
            {
                return;
            }

            InteractionOption option = _interactor.CurrentOption.Value;
            string prompt = option.IsAvailable
                ? $"[E] {option.ActionLabel} · {option.TargetName}"
                : $"{option.TargetName} · {option.UnavailableReason}";
            _promptStyle.normal.textColor = option.IsAvailable ? Color.white : new Color(1f, 0.55f, 0.25f);

            var promptRect = new Rect(0f, Screen.height * 0.5f + 28f, Screen.width, 40f);
            GUI.Label(promptRect, prompt, _promptStyle);
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
    }
}

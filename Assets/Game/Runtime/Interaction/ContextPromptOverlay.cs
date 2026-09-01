using UnityEngine;
using FunGame.Tools;
using FunGame.Incident;

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
        [SerializeField] private CoolingIncidentController incident;
        private GUIStyle _crosshairStyle;
        private GUIStyle _promptStyle;

        private void Awake()
        {
            _interactor = GetComponent<ContextInteractor>();
            _toolController = GetComponent<ToolController>();
        }

        public void Configure(CoolingIncidentController incidentController)
        {
            incident = incidentController;
        }

        private void OnGUI()
        {
            EnsureStyles();

            var crosshairRect = new Rect(Screen.width * 0.5f - 15f, Screen.height * 0.5f - 18f, 30f, 36f);
            GUI.Label(crosshairRect, "+", _crosshairStyle);

            if (incident != null)
            {
                string objective;
                bool available = incident.RunState == CoolingIncidentRunState.Active;
                if (incident.RunState == CoolingIncidentRunState.Failed)
                {
                    objective = $"事故失败 · 未完成：{incident.CurrentInstruction} · 用时 {CoolingIncidentController.FormatDuration(incident.LastRunDurationSeconds)} · 前往控制台重新开始";
                }
                else if (incident.RunState == CoolingIncidentRunState.Succeeded)
                {
                    objective = $"冷却系统已恢复 · 用时 {CoolingIncidentController.FormatDuration(incident.LastRunDurationSeconds)} · 前往控制台重新开始";
                }
                else
                {
                    objective = incident.Phase == CoolingIncidentPhase.ContainLeak
                        ? $"当前目标：{incident.CurrentInstruction} ({incident.SealProgress:P0})"
                        : $"当前目标：{incident.CurrentInstruction}";
                }

                DrawPrompt(objective, available, 22f);
                DrawPrompt(
                    $"用时：{CoolingIncidentController.FormatDuration(incident.ElapsedSeconds)} · 舱内温度：{incident.Temperature:F1}°C / {incident.FailureTemperature:F0}°C",
                    available,
                    48f);
            }

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

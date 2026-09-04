using FunGame.Interaction;
using FunGame.Tools;
using UnityEngine;

namespace FunGame.UI
{
    /// <summary>
    /// 显示唯一主工具位与唯一手持物品位；工具仍需在场景工具架上更换。
    /// </summary>
    [RequireComponent(typeof(PlayerToolbelt), typeof(ContextInteractor))]
    public sealed class ToolbeltStatusOverlay : MonoBehaviour
    {
        [SerializeField] private bool networkMode;
        [SerializeField] private bool rightAligned;
        public void ConfigureGuidanceLayout() => rightAligned = true;
        private FunGame.Networking.NetworkPlayerCarryController _networkCarry;
        public void ConfigureNetworkMode() => networkMode = true;
        private PlayerToolbelt _toolbelt;
        private ContextInteractor _interactor;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _activeStyle;

        private void Awake()
        {
            _toolbelt = GetComponent<PlayerToolbelt>();
            _interactor = GetComponent<ContextInteractor>();
            _networkCarry = GetComponent<FunGame.Networking.NetworkPlayerCarryController>();
        }

        private void OnGUI()
        {
            if (GameMenuController.IsAnyMenuOpen || _toolbelt == null || _interactor == null)
            {
                return;
            }

            EnsureStyles();
            bool alignRight = networkMode || rightAligned;
            float panelWidth = Mathf.Min(390f, alignRight ? Screen.width * 0.38f : Screen.width - 24f);
            var panel = new Rect(alignRight ? Screen.width - panelWidth - 16f : 12f, Screen.height - 174f, panelWidth, 162f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, 24f), "工具腰带（工具架换取）", _titleStyle);

            string heldItem = networkMode ? (_networkCarry != null && _networkCarry.HasHeldItem ? "共享替换管件" : "空")
                : _interactor.HeldItem != null ? _interactor.HeldItem.DisplayName : "空";
            GUI.Label(new Rect(panel.x + 12f, panel.y + 34f, panel.width - 24f, 24f),
                $"主工具：{_toolbelt.EquippedTool.GetDisplayName()}    手持物：[Q] {heldItem}", _activeStyle);

            GUI.Label(new Rect(panel.x + 12f, panel.y + 62f, panel.width - 24f, 22f),
                FormatTool(ToolKind.ImpactWrench, "紧固 / 高单体重击"), GetStyle(ToolKind.ImpactWrench));
            GUI.Label(new Rect(panel.x + 12f, panel.y + 86f, panel.width - 24f, 22f),
                FormatTool(ToolKind.SealantGun, "密封 / 范围喷覆清群"), GetStyle(ToolKind.SealantGun));
            GUI.Label(new Rect(panel.x + 12f, panel.y + 110f, panel.width - 24f, 22f),
                FormatTool(ToolKind.CircuitBridger, "桥接 / 瘫痪破盾"), GetStyle(ToolKind.CircuitBridger));
            GUI.Label(new Rect(panel.x + 12f, panel.y + 136f, panel.width - 24f, 20f),
                "[E] 取用/放回 · [左键] 使用 · [Q] 抛下", _bodyStyle);
        }

        private string FormatTool(ToolKind tool, string purpose)
        {
            string marker = _toolbelt.EquippedTool == tool ? "▶" : " ";
            return $"{marker} {tool.GetDisplayName()}：{purpose}";
        }

        private GUIStyle GetStyle(ToolKind tool)
        {
            return _toolbelt.EquippedTool == tool ? _activeStyle : _bodyStyle;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.78f, 0.82f, 0.86f) }
            };
            _activeStyle = new GUIStyle(_bodyStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.25f, 1f, 0.72f) }
            };
        }
    }
}

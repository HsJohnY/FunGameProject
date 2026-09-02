using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 战斗切片临时 HUD：只显示防卫目标、设备完整度和敌人状态。
    /// 正式 UI 建立后移除，不把显示逻辑写入战斗规则。
    /// </summary>
    public sealed class CombatStatusOverlay : MonoBehaviour
    {
        [SerializeField] private CombatEncounterController encounter;
        private GUIStyle _statusStyle;

        public void Configure(CombatEncounterController configuredEncounter)
        {
            encounter = configuredEncounter;
        }

        private void OnGUI()
        {
            if (encounter == null || encounter.DefenseTarget == null || encounter.Enemy == null)
            {
                return;
            }

            EnsureStyle();
            _statusStyle.normal.textColor = encounter.State == CombatEncounterState.Failed
                ? new Color(1f, 0.35f, 0.2f)
                : Color.white;

            GUI.Label(new Rect(0f, 20f, Screen.width, 34f), encounter.CurrentInstruction, _statusStyle);
            GUI.Label(
                new Rect(0f, 52f, Screen.width, 34f),
                $"设备完整度：{encounter.DefenseTarget.Integrity}/{encounter.DefenseTarget.MaxIntegrity} · 剩余干扰体：{encounter.RemainingEnemyCount}",
                _statusStyle);
        }

        private void EnsureStyle()
        {
            if (_statusStyle != null)
            {
                return;
            }

            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
        }
    }
}

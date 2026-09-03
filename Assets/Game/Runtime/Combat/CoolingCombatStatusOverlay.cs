using UnityEngine;
using FunGame.UI;
using FunGame.Demo;

namespace FunGame.Combat
{
    /// <summary>
    /// 维修场景中的紧凑战斗提示；不覆盖原维修目标，仅在干扰触发后补充状态。
    /// </summary>
    public sealed class CoolingCombatStatusOverlay : MonoBehaviour
    {
        [SerializeField] private CoolingCombatIntegrationController integration;
        [SerializeField] private SinglePlayerDemoController demoCampaign;
        private GUIStyle _style;

        public void Configure(CoolingCombatIntegrationController configuredIntegration)
        {
            integration = configuredIntegration;
        }

        public void ConfigureDemoCampaign(SinglePlayerDemoController configuredCampaign)
        {
            demoCampaign = configuredCampaign;
        }

        private void OnGUI()
        {
            if (GameMenuController.IsAnyMenuOpen ||
                (demoCampaign != null && demoCampaign.Chapter != SinglePlayerDemoChapter.CoolingEmergency) ||
                integration == null ||
                !integration.HasTriggered || integration.Encounter == null)
            {
                return;
            }

            CombatEncounterController encounter = integration.Encounter;
            if (encounter.State == CombatEncounterState.Dormant)
            {
                return;
            }

            EnsureStyle();
            _style.normal.textColor = encounter.State == CombatEncounterState.Failed
                ? new Color(1f, 0.3f, 0.15f)
                : encounter.State == CombatEncounterState.Succeeded
                    ? new Color(0.25f, 1f, 0.55f)
                    : new Color(1f, 0.8f, 0.2f);
            GUI.Label(new Rect(0f, 82f, Screen.width, 32f), encounter.CurrentInstruction, _style);
            GUI.Label(
                new Rect(0f, 112f, Screen.width, 32f),
                $"辅助设备：{encounter.DefenseTarget.Integrity}/{encounter.DefenseTarget.MaxIntegrity} · 剩余干扰体：{encounter.RemainingEnemyCount}",
                _style);
        }

        private void EnsureStyle()
        {
            if (_style != null)
            {
                return;
            }

            _style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
        }
    }
}

using FunGame.Incident;
using FunGame.UI;
using UnityEngine;

namespace FunGame.Demo
{
    /// <summary>
    /// 显示章节、当前组合目标和全程用时；具体交互动作仍由准星提示承担。
    /// </summary>
    public sealed class SinglePlayerDemoOverlay : MonoBehaviour
    {
        [SerializeField] private SinglePlayerDemoController campaign;
        private GUIStyle _titleStyle;
        private GUIStyle _objectiveStyle;

        public void Configure(SinglePlayerDemoController configuredCampaign)
        {
            campaign = configuredCampaign;
        }

        private void OnGUI()
        {
            if (campaign == null || GameMenuController.IsAnyMenuOpen)
            {
                return;
            }

            EnsureStyles();
            _titleStyle.normal.textColor = campaign.IsCompleted
                ? new Color(0.25f, 1f, 0.65f)
                : new Color(0.35f, 0.9f, 1f);
            GUI.Label(new Rect(24f, 20f, 520f, 34f), campaign.ChapterTitle, _titleStyle);
            GUI.Label(
                new Rect(24f, 54f, Mathf.Min(Screen.width - 48f, 760f), 32f),
                campaign.CurrentObjective,
                _objectiveStyle);
            GUI.Label(
                new Rect(Screen.width - 220f, 20f, 196f, 30f),
                $"演示用时 {CoolingIncidentController.FormatDuration(campaign.ElapsedSeconds)}",
                _objectiveStyle);
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _objectiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
        }
    }
}

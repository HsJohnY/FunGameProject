using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>显示所有客户端一致的章节目标。</summary>
    public sealed class NetworkCampaignOverlay : MonoBehaviour
    {
        private NetworkCampaignController _campaign;
        private NetworkCoolingIncidentController _incident;
        private GUIStyle _title, _body, _hint;
        private void OnGUI()
        {
            if (_campaign == null) _campaign = GetComponent<NetworkCampaignController>();
            if (_campaign == null || !_campaign.IsSpawned || FunGame.UI.GameMenuController.IsAnyMenuOpen) return;
            if (_incident == null) _incident = FindFirstObjectByType<NetworkCoolingIncidentController>();
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 0.67f, 0.24f) } };
                _body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true,
                    normal = { textColor = new Color(0.88f, 0.95f, 0.97f) } };
                _hint = new GUIStyle(GUI.skin.label) { fontSize = 12,
                    normal = { textColor = new Color(0.43f, 0.78f, 0.82f) } };
            }
            float width = Mathf.Min(680f, Screen.width - 32f);
            Fill(new Rect(16, 16, width, 100), new Color(0.025f, 0.045f, 0.065f, 0.94f));
            Fill(new Rect(16, 16, 4, 100), new Color(1f, 0.6f, 0.16f));
            GUI.Label(new Rect(34, 24, width - 36, 24), "维修队  /  协作远征", _title);
            GUI.Label(new Rect(34, 51, width - 36, 27), _campaign.CurrentObjective, _body);
            string status = _campaign.Chapter == NetworkCampaignChapter.CoolingRepair && _incident != null
                ? $"冷却温度 {_incident.Temperature:0.0}°C" : $"核心完整度 {_campaign.CoreIntegrity}";
            GUI.Label(new Rect(34, 83, width - 36, 20), status + "   ·   F1 会话   F2 诊断   F3 聊天   Esc 菜单", _hint);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}

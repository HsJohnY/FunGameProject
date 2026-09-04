using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>显示所有客户端一致的章节目标。</summary>
    public sealed class NetworkCampaignOverlay : MonoBehaviour
    {
        private GUIStyle _style;
        private void OnGUI()
        {
            NetworkCampaignController campaign = GetComponent<NetworkCampaignController>();
            if (campaign == null) return;
            _style ??= new GUIStyle(GUI.skin.box) { fontSize = 17, alignment = TextAnchor.MiddleLeft };
            GUI.Box(new Rect(16f, 92f, 620f, 42f), campaign.CurrentObjective, _style);
        }
    }
}

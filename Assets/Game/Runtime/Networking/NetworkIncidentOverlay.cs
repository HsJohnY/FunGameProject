using FunGame.Incident;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// M3-4 技术验证 HUD，直接展示客户端收到的同步事故状态。
    /// </summary>
    public sealed class NetworkIncidentOverlay : MonoBehaviour
    {
        [SerializeField] private NetworkCoolingIncidentController incident;

        public void Configure(NetworkCoolingIncidentController controller) => incident = controller;

        private void OnGUI()
        {
            if (incident == null || !incident.IsSpawned)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(Screen.width - 440f, 20f, 420f, 145f), GUI.skin.box);
            GUILayout.Label("主机权威冷却事故状态");
            GUILayout.Label($"阶段：{incident.Phase}　状态：{incident.RunState}");
            GUILayout.Label($"目标：{incident.CurrentInstruction}");
            if (incident.Phase == CoolingIncidentPhase.ContainLeak)
            {
                GUILayout.Label($"密封进度：{incident.SealProgress:P0}");
            }
            if (incident.Phase == CoolingIncidentPhase.RestoreControlPower)
            {
                GUILayout.Label($"线路桥接：{incident.CircuitBridgeProgress}/{CoolingIncidentRules.RequiredCircuitBridgeSteps}");
            }
            GUILayout.Label($"温度：{incident.Temperature:F1} / {incident.FailureTemperature:F0} °C");
            GUILayout.EndArea();
        }
    }
}

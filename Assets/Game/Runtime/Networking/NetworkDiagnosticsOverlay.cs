using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FunGame.Networking
{
    /// <summary>
    /// M3-6 验证面板：显示会话角色、对象数量和客户端到主机 RTT，便于双进程记录。
    /// </summary>
    public sealed class NetworkDiagnosticsOverlay : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport transport;
        private bool _visible;

        public void Configure(NetworkManager manager, UnityTransport configuredTransport)
        {
            networkManager = manager;
            transport = configuredTransport;
        }

        private void Update()
        {
            if (!NetworkChatController.IsChatOpen && Keyboard.current?.f2Key.wasPressedThisFrame == true)
            {
                _visible = !_visible;
            }
        }

        private void OnGUI()
        {
            if (!_visible || networkManager == null || FunGame.UI.GameMenuController.IsAnyMenuOpen)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(Screen.width - 330f, 130f, 310f, 160f), GUI.skin.box);
            GUILayout.Label("M3-6 网络诊断（F2 关闭）");
            GUILayout.Label($"模式：{GetModeLabel()}");
            GUILayout.Label($"本地客户端：{networkManager.LocalClientId}");
            GUILayout.Label($"已连接玩家：{networkManager.ConnectedClientsIds.Count}");
            GUILayout.Label($"已生成对象：{networkManager.SpawnManager?.SpawnedObjects.Count ?? 0}");
            if (networkManager.IsClient && !networkManager.IsHost && transport != null)
            {
                ulong rtt = transport.GetCurrentRtt(NetworkManager.ServerClientId);
                GUILayout.Label($"往返延迟：{rtt} ms（{NetworkQualityRules.GetLabel(rtt)}）");
            }
            GUILayout.EndArea();
        }

        private string GetModeLabel()
        {
            if (networkManager.IsHost) return "主机";
            if (networkManager.IsClient) return "客户端";
            return networkManager.ShutdownInProgress ? "正在清理" : "未连接";
        }
    }
}

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// M3-1 的最小会话入口：配置直连地址、启动监听主机或客户端，并安全停止会话。
    /// 当前使用即时 GUI 仅用于双进程技术验证，正式大厅界面将在会话规则稳定后替换它。
    /// </summary>
    public sealed class NetworkSessionController : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport transport;

        private string addressText = NetworkEndpointRules.DefaultAddress;
        private string portText = NetworkEndpointRules.DefaultPort.ToString();
        private string statusText = "尚未启动会话";

        public string StatusText => statusText;

        public void Configure(NetworkManager manager, UnityTransport networkTransport)
        {
            networkManager = manager;
            transport = networkTransport;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }
        }

        private void OnGUI()
        {
            const float width = 420f;
            GUILayout.BeginArea(new Rect(20f, 20f, width, 270f), GUI.skin.box);
            GUILayout.Label("M3-1 双人会话验证");
            GUILayout.Space(8f);

            bool idle = networkManager != null && !networkManager.IsListening;
            GUI.enabled = idle;
            GUILayout.Label("主机 IPv4 地址");
            addressText = GUILayout.TextField(addressText);
            GUILayout.Label("端口");
            portText = GUILayout.TextField(portText);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("启动主机", GUILayout.Height(32f)))
            {
                StartHost();
            }

            if (GUILayout.Button("加入主机", GUILayout.Height(32f)))
            {
                StartClient();
            }
            GUILayout.EndHorizontal();

            GUI.enabled = networkManager != null && networkManager.IsListening;
            if (GUILayout.Button("停止 / 断开", GUILayout.Height(28f)))
            {
                StopSession();
            }

            GUI.enabled = true;
            GUILayout.Space(8f);
            GUILayout.Label($"状态：{statusText}");
            if (networkManager != null && networkManager.IsHost)
            {
                GUILayout.Label($"已连接玩家：{networkManager.ConnectedClientsIds.Count}");
            }

            GUILayout.EndArea();
        }

        public bool StartHost()
        {
            if (!TryApplyEndpoint("0.0.0.0"))
            {
                return false;
            }

            statusText = networkManager.StartHost() ? "主机已启动，等待客户端" : "主机启动失败，请查看 Console";
            return networkManager.IsHost;
        }

        public bool StartClient()
        {
            if (!TryApplyEndpoint(null))
            {
                return false;
            }

            statusText = networkManager.StartClient() ? "正在连接主机…" : "客户端启动失败，请查看 Console";
            return networkManager.IsClient;
        }

        public void StopSession()
        {
            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            statusText = "会话已停止";
        }

        private bool TryApplyEndpoint(string listenAddress)
        {
            if (networkManager == null || transport == null)
            {
                statusText = "场景缺少 NetworkManager 或 UnityTransport";
                return false;
            }

            if (networkManager.IsListening)
            {
                statusText = "请先停止当前会话";
                return false;
            }

            if (!NetworkEndpointRules.TryNormalize(addressText, portText, out string address, out ushort port, out string error))
            {
                statusText = error;
                return false;
            }

            transport.SetConnectionData(address, port, listenAddress);
            return true;
        }

        private void Subscribe()
        {
            if (networkManager == null)
            {
                return;
            }

            networkManager.OnClientConnectedCallback -= HandleClientConnected;
            networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            networkManager.OnClientConnectedCallback += HandleClientConnected;
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        private void Unsubscribe()
        {
            if (networkManager == null)
            {
                return;
            }

            networkManager.OnClientConnectedCallback -= HandleClientConnected;
            networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        private void HandleClientConnected(ulong clientId)
        {
            statusText = networkManager.IsHost
                ? $"玩家 {clientId} 已连接"
                : "已连接到主机";
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (networkManager != null && !networkManager.IsHost && clientId == networkManager.LocalClientId)
            {
                statusText = string.IsNullOrWhiteSpace(networkManager.DisconnectReason)
                    ? "已与主机断开"
                    : $"连接已断开：{networkManager.DisconnectReason}";
                return;
            }

            statusText = $"玩家 {clientId} 已断开";
        }
    }
}

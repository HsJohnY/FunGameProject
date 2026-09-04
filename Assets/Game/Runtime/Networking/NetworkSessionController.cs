using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FunGame.Networking
{
    /// <summary>
    /// M3-1 的最小会话入口：配置直连地址、启动监听主机或客户端，并安全停止会话。
    /// 当前使用即时 GUI 仅用于双进程技术验证，正式大厅界面将在会话规则稳定后替换它。
    /// </summary>
    public sealed class NetworkSessionController : MonoBehaviour
    {
        private enum SessionState
        {
            Idle,
            Connecting,
            ClientConnected,
            HostRunning,
            Stopping
        }

        private const int ConnectTimeoutMilliseconds = 500;
        private const int MaxConnectAttempts = 6;

        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport transport;
        [SerializeField] private string panelTitle = "M3-2 双人会话验证";

        private string addressText = NetworkEndpointRules.DefaultAddress;
        private string portText = NetworkEndpointRules.DefaultPort.ToString();
        private string statusText = "尚未启动会话";
        private SessionState sessionState = SessionState.Idle;
        private bool shutdownRequested;
        private bool transportFailedDuringStart;
        private bool panelVisible = true;

        public string StatusText => statusText;
        public bool IsEndpointEditable => sessionState == SessionState.Idle;

        /// <summary>
        /// 为后续正式大厅界面和自动测试提供统一的地址输入入口。
        /// </summary>
        public bool TrySetEndpointInput(string address, string port)
        {
            if (!IsEndpointEditable)
            {
                statusText = "请先停止当前会话";
                return false;
            }

            if (!NetworkEndpointRules.TryNormalize(address, port, out string normalizedAddress, out ushort normalizedPort, out string error))
            {
                statusText = error;
                return false;
            }

            addressText = normalizedAddress;
            portText = normalizedPort.ToString();
            return true;
        }

        public void Configure(NetworkManager manager, UnityTransport networkTransport, string configuredPanelTitle = null)
        {
            Unsubscribe();
            networkManager = manager;
            transport = networkTransport;
            if (!string.IsNullOrWhiteSpace(configuredPanelTitle))
            {
                panelTitle = configuredPanelTitle;
            }
            Subscribe();
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

        private void Update()
        {
            if (Keyboard.current?.f1Key.wasPressedThisFrame == true)
            {
                panelVisible = !panelVisible;
            }

            // 网络玩家生成后鼠标会被第一人称视角锁定；Esc 始终保留为技术验证场景的安全退出键。
            if (sessionState != SessionState.Idle && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                StopSession();
            }

            // NGO 的断开回调可能发生在其内部事件分发期间。延迟到下一帧清理，
            // 避免在回调栈中再次关闭传输层，同时确保失败客户端回到可编辑状态。
            if (!shutdownRequested)
            {
                return;
            }

            shutdownRequested = false;
            if (networkManager != null && networkManager.IsListening)
            {
                // 保留一帧消息队列，让房主退出原因和最后的 Despawn 有机会送达客户端。
                networkManager.Shutdown(false);
                return;
            }

            CompleteShutdown();
        }

        private void OnGUI()
        {
            if (!panelVisible)
            {
                GUILayout.BeginArea(new Rect(20f, 20f, 300f, 58f), GUI.skin.box);
                GUILayout.Label($"F1：显示网络面板　状态：{statusText}");
                GUILayout.EndArea();
                return;
            }

            const float width = 420f;
            GUILayout.BeginArea(new Rect(20f, 20f, width, 300f), GUI.skin.box);
            GUILayout.Label($"{panelTitle}（F1 隐藏）");
            GUILayout.Space(8f);

            bool idle = networkManager != null && sessionState == SessionState.Idle;
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

            // 停止操作由我们的会话状态控制，不能依赖 NGO 正在异步变化的 IsListening。
            GUI.enabled = networkManager != null && sessionState != SessionState.Idle;
            if (GUILayout.Button("停止 / 断开", GUILayout.Height(28f)))
            {
                StopSession();
            }

            GUI.enabled = true;
            GUILayout.Space(8f);
            GUILayout.Label($"状态：{statusText}");
            GUILayout.Label("会话中按 Esc 可停止 / 断开");
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

            // 已知端口冲突在进入 Unity Transport 前转换为正常的界面提示，
            // 避免底层按严重错误写入 Console，并让玩家可以立即修改端口。
            ushort hostPort = transport.ConnectionData.Port;
            if (!NetworkPortAvailability.CanBindUdp(hostPort))
            {
                statusText = $"无法启动主机：端口 {hostPort} 已被占用，请更换端口";
                return false;
            }

            transportFailedDuringStart = false;
            sessionState = SessionState.HostRunning;
            if (networkManager.StartHost())
            {
                // 端口绑定错误既可能同步返回，也可能在后续传输更新中报告。
                // 同步失败事件已经提供了更明确的提示时，不要用泛化消息覆盖它。
                if (transportFailedDuringStart)
                {
                    return false;
                }

                statusText = "主机已启动，等待客户端";
                return true;
            }

            sessionState = SessionState.Idle;
            if (!transportFailedDuringStart)
            {
                statusText = "主机启动失败，请检查端口后重试";
            }
            return false;
        }

        public bool StartClient()
        {
            if (!TryApplyEndpoint(null))
            {
                return false;
            }

            // 默认传输层可能持续重试约一分钟。技术验证阶段缩短到约三秒，
            // 使错误地址或未启动的主机能快速失败并允许玩家重新输入。
            transport.ConnectTimeoutMS = ConnectTimeoutMilliseconds;
            transport.MaxConnectAttempts = MaxConnectAttempts;
            sessionState = SessionState.Connecting;
            if (networkManager.StartClient())
            {
                statusText = "正在连接主机…（可随时停止）";
                return true;
            }

            sessionState = SessionState.Idle;
            statusText = "客户端启动失败，请检查地址后重试";
            return false;
        }

        public void StopSession()
        {
            if (networkManager == null || sessionState == SessionState.Idle)
            {
                return;
            }

            sessionState = SessionState.Stopping;
            statusText = "会话已停止";
            shutdownRequested = true;
        }

        private bool TryApplyEndpoint(string listenAddress)
        {
            if (networkManager == null || transport == null)
            {
                statusText = "场景缺少 NetworkManager 或 UnityTransport";
                return false;
            }

            if (sessionState != SessionState.Idle || networkManager.IsListening)
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
            networkManager.OnTransportFailure -= HandleTransportFailure;
            networkManager.OnClientStopped -= HandleClientStopped;
            networkManager.OnServerStopped -= HandleServerStopped;
            networkManager.ConnectionApprovalCallback -= HandleConnectionApproval;
            networkManager.OnClientConnectedCallback += HandleClientConnected;
            networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            networkManager.OnTransportFailure += HandleTransportFailure;
            networkManager.OnClientStopped += HandleClientStopped;
            networkManager.OnServerStopped += HandleServerStopped;
            networkManager.ConnectionApprovalCallback += HandleConnectionApproval;
        }

        private void Unsubscribe()
        {
            if (networkManager == null)
            {
                return;
            }

            networkManager.OnClientConnectedCallback -= HandleClientConnected;
            networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            networkManager.OnTransportFailure -= HandleTransportFailure;
            networkManager.OnClientStopped -= HandleClientStopped;
            networkManager.OnServerStopped -= HandleServerStopped;
            networkManager.ConnectionApprovalCallback -= HandleConnectionApproval;
        }

        private static void HandleConnectionApproval(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            // 在 NetworkObject 创建前由服务器写入出生变换，避免先在预制体原点坠落再被拥有者拉回。
            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Position = NetworkPlayerSpawnLayout.GetSpawnPosition(request.ClientNetworkId);
            response.Rotation = Quaternion.identity;
            response.Pending = false;
        }

        private void HandleClientConnected(ulong clientId)
        {
            sessionState = networkManager.IsHost ? SessionState.HostRunning : SessionState.ClientConnected;
            panelVisible = false;
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
                sessionState = SessionState.Stopping;
                shutdownRequested = true;
                return;
            }

            statusText = $"玩家 {clientId} 已断开";
        }

        private void HandleTransportFailure()
        {
            transportFailedDuringStart = true;
            statusText = sessionState == SessionState.HostRunning
                ? "主机启动失败：端口可能已被占用，请更换端口后重试"
                : "网络传输失败，请检查地址和端口后重试";
            sessionState = SessionState.Stopping;
            shutdownRequested = true;
        }

        private void HandleClientStopped(bool wasServer)
        {
            CompleteShutdown();
        }

        private void HandleServerStopped(bool wasClient)
        {
            CompleteShutdown();
        }

        private void CompleteShutdown()
        {
            shutdownRequested = false;
            sessionState = SessionState.Idle;
            panelVisible = true;
            SetCursorAvailable();
        }

        private static void SetCursorAvailable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}

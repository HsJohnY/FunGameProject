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

        private const int ConnectTimeoutMilliseconds = 1000;
        private const int MaxConnectAttempts = 10;
        private const float ConnectionRecoveryTimeoutSeconds = 12f;
        private const float ShutdownRecoveryTimeoutSeconds = 2f;

        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport transport;
        [SerializeField] private string panelTitle = "M3-2 双人会话验证";
        [SerializeField] private bool escapeStopsSession = true;

        private string addressText = NetworkEndpointRules.DefaultAddress;
        private string portText = NetworkEndpointRules.DefaultPort.ToString();
        private string statusText = "尚未启动会话";
        private SessionState sessionState = SessionState.Idle;
        private bool shutdownRequested;
        private bool transportFailedDuringStart;
        private bool panelVisible = true;
        private float connectionDeadline;
        private float shutdownDeadline;

        public string StatusText => statusText;
        public bool IsEndpointEditable => sessionState == SessionState.Idle;
        public bool EscapeStopsSession => escapeStopsSession;
        public string Address => addressText;
        public string Port => portText;
        public bool HasLocalPlayer => networkManager != null && networkManager.IsConnectedClient
            && networkManager.LocalClient?.PlayerObject != null;
        public bool IsHost => networkManager != null && networkManager.IsHost;
        public int ConnectedPlayerCount => networkManager != null && networkManager.IsHost
            ? networkManager.ConnectedClientsIds.Count : 0;

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

        /// <summary>
        /// 创建房间只需要端口。主机监听地址由程序固定为全部 IPv4 网卡，
        /// 不接受玩家填写的“本机 IP”，避免把远端连接地址和本地监听地址混为一谈。
        /// </summary>
        public bool TrySetHostPortInput(string port)
        {
            if (!IsEndpointEditable)
            {
                statusText = "请先停止当前会话";
                return false;
            }

            if (!NetworkEndpointRules.TryNormalizePort(port, out ushort normalizedPort, out string error))
            {
                statusText = error;
                return false;
            }

            portText = normalizedPort.ToString();
            return true;
        }

        public void Configure(
            NetworkManager manager,
            UnityTransport networkTransport,
            string configuredPanelTitle = null,
            bool configuredEscapeStopsSession = true)
        {
            Unsubscribe();
            networkManager = manager;
            transport = networkTransport;
            if (!string.IsNullOrWhiteSpace(configuredPanelTitle))
            {
                panelTitle = configuredPanelTitle;
            }
            escapeStopsSession = configuredEscapeStopsSession;
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
            if (escapeStopsSession && Keyboard.current?.f1Key.wasPressedThisFrame == true)
            {
                panelVisible = !panelVisible;
            }

            // 网络玩家生成后鼠标会被第一人称视角锁定；Esc 始终保留为技术验证场景的安全退出键。
            if (escapeStopsSession
                && sessionState != SessionState.Idle
                && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                StopSession();
            }

            // 某些虚拟网卡失败后不会及时产生断开回调。不能让界面无限停留在 Connecting。
            if (sessionState == SessionState.Connecting
                && connectionDeadline > 0f
                && Time.realtimeSinceStartup >= connectionDeadline)
            {
                BeginShutdown("连接超时，请确认房主地址、端口和防火墙设置后重试");
            }

            // NGO 的断开回调可能发生在其内部事件分发期间。延迟到下一帧清理，
            // 避免在回调栈中再次关闭传输层，同时确保失败客户端回到可编辑状态。
            if (shutdownRequested)
            {
                shutdownRequested = false;
                shutdownDeadline = Time.realtimeSinceStartup + ShutdownRecoveryTimeoutSeconds;
                if (networkManager != null && networkManager.IsListening)
                {
                    // 先尝试正常停止，让最后的 Despawn 和断开消息有机会完成。
                    networkManager.Shutdown(false);
                }
                else
                {
                    CompleteShutdown();
                }
            }

            // 即使底层停止回调丢失，也必须在短时间后解除界面输入锁定。
            if (sessionState == SessionState.Stopping
                && shutdownDeadline > 0f
                && Time.realtimeSinceStartup >= shutdownDeadline)
            {
                if (networkManager != null && networkManager.IsListening)
                {
                    networkManager.Shutdown(true);
                }
                CompleteShutdown();
            }
        }

        private void OnGUI()
        {
            // The shared campaign uses GameMenuController for cursor-safe room controls.
            if (!escapeStopsSession) return;
            if (!panelVisible)
            {
                GUILayout.BeginArea(new Rect(20f, 20f, 300f, 58f), GUI.skin.box);
                GUILayout.Label($"F1：显示网络面板　状态：{statusText}");
                GUILayout.EndArea();
                return;
            }

            const float width = 420f;
            GUILayout.BeginArea(new Rect(20f, escapeStopsSession ? 20f : 126f, width, 300f), GUI.skin.box);
            GUILayout.Label($"{panelTitle}（F1 隐藏）");
            GUILayout.Space(8f);

            bool idle = networkManager != null && sessionState == SessionState.Idle;
            GUI.enabled = idle;
            GUILayout.Label("房主 IPv4 地址（仅加入房间时使用）");
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
            GUILayout.Label(escapeStopsSession
                ? "会话中按 Esc 可停止 / 断开"
                : "会话中按 F1 打开面板并选择停止 / 断开");
            if (networkManager != null && networkManager.IsHost)
            {
                GUILayout.Label($"已连接玩家：{networkManager.ConnectedClientsIds.Count}");
            }

            GUILayout.EndArea();
        }

        public bool StartHost()
        {
            if (!TryApplyHostEndpoint())
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

            // 虚拟网卡首次握手可能比真实局域网更慢；保留约十秒连接窗口，
            // 同时仍让错误地址在合理时间内恢复为可编辑状态。
            transport.ConnectTimeoutMS = ConnectTimeoutMilliseconds;
            transport.MaxConnectAttempts = MaxConnectAttempts;
            sessionState = SessionState.Connecting;
            if (networkManager.StartClient())
            {
                connectionDeadline = Time.realtimeSinceStartup + ConnectionRecoveryTimeoutSeconds;
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

            BeginShutdown("会话已停止");
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

        private bool TryApplyHostEndpoint()
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

            if (!NetworkEndpointRules.TryNormalizePort(portText, out ushort port, out string error))
            {
                statusText = error;
                return false;
            }

            // Address 是主机进程内的本地客户端目标；ServerListenAddress 才是服务器监听范围。
            // 监听 0.0.0.0 后，真实局域网与虚拟局域网网卡都可接收外部客户端连接。
            transport.SetConnectionData(
                NetworkEndpointRules.DefaultAddress,
                port,
                NetworkEndpointRules.AnyIpv4Address);
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
            connectionDeadline = 0f;
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
                statusText = sessionState == SessionState.Stopping
                    ? "已离开房间，可以重新加入或创建房间"
                    : sessionState == SessionState.Connecting
                        ? "连接失败，请确认房主已开房且地址、端口正确后重试"
                        : "与房主的连接已断开，请重新加入房间";
                BeginShutdown(statusText);
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
            BeginShutdown(statusText);
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
            connectionDeadline = 0f;
            shutdownDeadline = 0f;
            sessionState = SessionState.Idle;
            panelVisible = true;
            SetCursorAvailable();
        }

        private void BeginShutdown(string message)
        {
            statusText = message;
            connectionDeadline = 0f;
            sessionState = SessionState.Stopping;
            shutdownRequested = true;
        }

        private static void SetCursorAvailable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}

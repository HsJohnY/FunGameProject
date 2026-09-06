using FunGame.Networking;
using UnityEngine;

namespace FunGame.UI
{
    public sealed partial class GameMenuController
    {
        private enum LobbySetupMode
        {
            ChooseAction,
            CreateRoom,
            JoinRoom
        }

        private NetworkSessionController _session;
        private string _roomAddress = NetworkEndpointRules.DefaultAddress;
        private string _roomPort = NetworkEndpointRules.DefaultPort.ToString();
        private bool _enterWhenConnected;
        private bool _hadConnectedSession;
        private GUIStyle _roomInputStyle;
        private GUIStyle _roomBodyStyle;
        private LobbySetupMode _lobbySetupMode;

        public bool IsNetworkLobbyOpen => _menuOpen && _page == MenuPage.Lobby;
        private bool HasConnectedSession => _session != null && _session.HasLocalPlayer;

        public void OpenNetworkLobby()
        {
            if (!networkSessionFlow) return;
            if (_session == null) _session = Object.FindFirstObjectByType<NetworkSessionController>();
            if (_session != null)
            {
                _roomAddress = _session.Address;
                _roomPort = _session.Port;
            }
            if (!HasConnectedSession) _lobbySetupMode = LobbySetupMode.ChooseAction;
            OpenMenu(MenuPage.Lobby);
        }

        public bool StartRoom(bool host, string address, string port)
        {
            if (!IsNetworkLobbyOpen || _session == null) return false;
            _roomPort = port;
            if (!host) _roomAddress = address;

            // 房主只决定监听端口；监听哪些本机网卡由会话层统一处理。
            // 只有加入方需要填写并校验房主的可达 IPv4 地址。
            bool endpointValid = host
                ? _session.TrySetHostPortInput(port)
                : _session.TrySetEndpointInput(address, port);
            if (!endpointValid) return false;
            _enterWhenConnected = host ? _session.StartHost() : _session.StartClient();
            return _enterWhenConnected;
        }

        public void DisconnectRoom()
        {
            CancelHostPreparation();
            _enterWhenConnected = false;
            _lobbySetupMode = LobbySetupMode.ChooseAction;
            if (_session != null) _session.StopSession();
            OpenMenu(MenuPage.Lobby);
        }

        private void LeaveNetworkLobby()
        {
            CancelHostPreparation();
            if (HasConnectedSession) CloseMenu();
            else if (_session != null && !_session.IsEndpointEditable) DisconnectRoom();
            else OpenMenu(MenuPage.Main);
        }

        private void UpdateNetworkLobby()
        {
            if (!networkSessionFlow || _changingScene) return;
            UpdateHostPreparation();
            if (_session == null) _session = Object.FindFirstObjectByType<NetworkSessionController>();
            bool connected = HasConnectedSession;
            if (_enterWhenConnected && connected)
            {
                _enterWhenConnected = false;
                TryBindSpawnedPlayer();
                // 无论创建还是加入，连接成功后直接进入游戏。
                // 房主地址由玩家从实际使用的局域网或虚拟局域网软件中发送给好友，
                // 避免程序枚举到 Docker、WSL、代理等不可达的虚拟网卡并造成误导。
                CloseMenu();
            }
            else if (_enterWhenConnected && _session != null && _session.IsEndpointEditable)
                _enterWhenConnected = false;

            if (_hadConnectedSession && !connected) OpenNetworkLobby();
            _hadConnectedSession = connected;
        }

        private void HandleMenuKeyboard()
        {
            Event input = Event.current;
            if (input.type != EventType.KeyDown || _changingScene) return;
            if (input.keyCode == KeyCode.F1 && networkSessionFlow)
            {
                input.Use();
                if (IsNetworkLobbyOpen) LeaveNetworkLobby();
                else OpenNetworkLobby();
            }
            else if (input.keyCode == KeyCode.Escape)
            {
                input.Use();
                if (!_menuOpen)
                {
                    if (networkSessionFlow && !HasConnectedSession) OpenNetworkLobby();
                    else OpenMenu(MenuPage.Pause);
                }
                else if (_page == MenuPage.Settings) _page = _settingsReturnPage;
                else if (_page == MenuPage.Lobby) LeaveNetworkLobby();
                else if (_page == MenuPage.Pause) CloseMenu();
            }
        }

        // Keep cursor ownership after NGO spawns a player or another input component updates.
        private void LateUpdate()
        {
            if (!_menuOpen) return;
            if (player != null && player.IsInputEnabled) SetGameplayEnabled(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void DrawNetworkLobby()
        {
            if (_roomInputStyle == null)
            {
                _roomInputStyle = new GUIStyle(GUI.skin.textField)
                {
                    fontSize = 21, alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(16, 16, 8, 8),
                    normal = { background = _contentTexture, textColor = Color.white },
                    focused = { background = _buttonHoverTexture, textColor = Color.white }
                };
                _roomBodyStyle = new GUIStyle(_valueStyle) { wordWrap = true, fontSize = 17 };
            }

            Matrix4x4 previousMatrix = GUI.matrix;
            bool previousEnabled = GUI.enabled;
            float scale = Mathf.Min(1f, Mathf.Min((Screen.width - 32f) / 1080f, (Screen.height - 32f) / 650f));
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1080f * scale) / 2f,
                (Screen.height - 650f * scale) / 2f, 0f), Quaternion.identity, Vector3.one * scale);
            try
            {
                DrawPanelFrame(new Rect(0f, 0f, 1080f, 650f));
                GUI.Label(new Rect(42f, 24f, 900f, 24f), "COOPERATIVE EXPEDITION  //  CREW CONNECTION", _eyebrowStyle);
                GUI.Label(new Rect(42f, 58f, 800f, 60f), "联机协作 · 维修队集结", _titleStyle);
                DrawRect(new Rect(42f, 132f, 996f, 2f), Cyan);

                bool idle = _session != null && _session.IsEndpointEditable;
                bool connected = HasConnectedSession;
                string state = _hostFirewall != null && !string.IsNullOrEmpty(_hostFirewall.Message)
                    ? _hostFirewall.Message
                    : _session == null ? "联机服务尚未就绪，请返回主菜单重试。" : _session.StatusText;

                GUI.Box(new Rect(42f, 158f, 996f, 390f), GUIContent.none, _contentStyle);
                if (connected)
                {
                    GUI.Label(new Rect(70f, 184f, 940f, 36f),
                        _session.IsHost ? "房间创建成功" : "已加入好友房间", _sectionStyle);
                    if (_session.IsHost)
                    {
                        GUI.Label(new Rect(70f, 238f, 890f, 60f),
                            $"房间端口：{_session.Port}\n请从正在使用的虚拟局域网软件中复制房主地址。", _roomBodyStyle);
                        state = $"房间已开启 · 已连接 {_session.ConnectedPlayerCount} 人";
                    }
                    else state = $"已连接房主 {_session.Address}:{_session.Port}";

                    GUI.Label(new Rect(70f, 424f, 900f, 48f), state, _roomBodyStyle);
                    if (GUI.Button(new Rect(70f, 486f, 280f, 44f), "断开连接", _secondaryButtonStyle)) DisconnectRoom();
                    if (GUI.Button(new Rect(730f, 486f, 280f, 44f), "进入 / 返回游戏", _buttonStyle)) CloseMenu();
                }
                else if (_lobbySetupMode == LobbySetupMode.ChooseAction)
                {
                    GUI.Label(new Rect(70f, 184f, 940f, 36f), "选择联机方式", _sectionStyle);
                    GUI.Label(new Rect(70f, 236f, 940f, 48f),
                        "房主创建房间后，把所用局域网或虚拟局域网的 IP 发给队友。", _roomBodyStyle);
                    GUI.enabled = idle;
                    if (GUI.Button(new Rect(130f, 320f, 350f, 76f), "创建房间", _buttonStyle))
                        _lobbySetupMode = LobbySetupMode.CreateRoom;
                    if (GUI.Button(new Rect(600f, 320f, 350f, 76f), "加入房间", _buttonStyle))
                        _lobbySetupMode = LobbySetupMode.JoinRoom;
                    GUI.enabled = true;
                    if (GUI.Button(new Rect(400f, 462f, 280f, 44f), "返回主菜单", _secondaryButtonStyle))
                        OpenMenu(MenuPage.Main);
                }
                else
                {
                    bool creating = _lobbySetupMode == LobbySetupMode.CreateRoom;
                    GUI.Label(new Rect(70f, 184f, 940f, 36f), creating ? "创建房间" : "加入房间", _sectionStyle);
                    GUI.enabled = idle && !IsPreparingHost;
                    if (!creating)
                    {
                        GUI.Label(new Rect(70f, 238f, 430f, 24f), "好友发送的房主 IPv4", _subtitleStyle);
                        GUI.SetNextControlName("RoomAddress");
                        _roomAddress = GUI.TextField(new Rect(70f, 272f, 430f, 48f), _roomAddress, 64, _roomInputStyle);
                    }
                    else
                    {
                        GUI.Label(new Rect(70f, 248f, 430f, 70f),
                            "无需填写本机 IP。联机授权仅在本次游戏运行期间有效，退出后自动清理。", _roomBodyStyle);
                    }

                    GUI.Label(new Rect(560f, 238f, 400f, 24f), "房间端口", _subtitleStyle);
                    GUI.SetNextControlName("RoomPort");
                    _roomPort = GUI.TextField(new Rect(560f, 272f, 400f, 48f), _roomPort, 5, _roomInputStyle);
                    bool needsConsent = creating && FirewallState == HostFirewallState.NeedsConsent;
                    GUI.enabled = idle && (!IsPreparingHost || needsConsent);
                    if (GUI.Button(new Rect(560f, 348f, 400f, 58f), needsConsent ? "授权本次联机并创建" : creating ? "确认创建" : "确认加入", _buttonStyle))
                    {
                        if (needsConsent) AuthorizeHostFirewall();
                        else RequestRoomFromMenu(creating, _roomAddress, _roomPort);
                    }
                    GUI.enabled = true;
                    GUI.Label(new Rect(70f, 422f, 890f, 44f), state, _roomBodyStyle);
                    GUI.enabled = IsPreparingHost || (_session != null && !idle);
                    if (GUI.Button(new Rect(70f, 486f, 280f, 44f), "取消连接", _secondaryButtonStyle)) DisconnectRoom();
                    GUI.enabled = true;
                    if (GUI.Button(new Rect(730f, 486f, 280f, 44f), "返回", _secondaryButtonStyle))
                    {
                        CancelHostPreparation();
                        _lobbySetupMode = LobbySetupMode.ChooseAction;
                    }
                }
                GUI.Label(new Rect(502f, 612f, 536f, 24f), "[ F1 ] 联机房间    [ ESC ] 返回", _eyebrowStyle);
            }
            finally { GUI.matrix = previousMatrix; GUI.enabled = previousEnabled; }
        }

    }
}

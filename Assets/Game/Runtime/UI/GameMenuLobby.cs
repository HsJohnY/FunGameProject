using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        private string _localAddresses;
        private bool _enterWhenConnected;
        private bool _hadConnectedSession;
        private GUIStyle _roomInputStyle;
        private GUIStyle _roomBodyStyle;
        private int _localAddressIndex;
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
            if (_localAddresses == null) _localAddresses = FindLocalAddresses();
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
            _enterWhenConnected = false;
            _lobbySetupMode = LobbySetupMode.ChooseAction;
            if (_session != null) _session.StopSession();
            OpenMenu(MenuPage.Lobby);
        }

        private void LeaveNetworkLobby()
        {
            if (HasConnectedSession) CloseMenu();
            else if (_session != null && !_session.IsEndpointEditable) DisconnectRoom();
            else OpenMenu(MenuPage.Main);
        }

        private void UpdateNetworkLobby()
        {
            if (!networkSessionFlow || _changingScene) return;
            if (_session == null) _session = Object.FindFirstObjectByType<NetworkSessionController>();
            bool connected = HasConnectedSession;
            if (_enterWhenConnected && connected)
            {
                _enterWhenConnected = false;
                TryBindSpawnedPlayer();
                // 房主先停留在房间信息页，以便复制自动检测到的局域网地址给好友。
                // 加入方连接成功后直接进入游戏，仍可用 F1 再次打开房间页。
                if (_session.IsHost) OpenMenu(MenuPage.Lobby);
                else CloseMenu();
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
                string state = _session == null ? "联机服务尚未就绪，请返回主菜单重试。" : _session.StatusText;
                string[] addresses = (_localAddresses ?? "未检测到局域网地址").Split('\n');

                GUI.Box(new Rect(42f, 158f, 996f, 390f), GUIContent.none, _contentStyle);
                if (connected)
                {
                    GUI.Label(new Rect(70f, 184f, 940f, 36f),
                        _session.IsHost ? "房间创建成功" : "已加入好友房间", _sectionStyle);
                    if (_session.IsHost)
                    {
                        GUI.Label(new Rect(70f, 238f, 430f, 28f), "将下面的 IP 与端口发送给好友", _subtitleStyle);
                        _localAddressIndex = GUI.SelectionGrid(new Rect(70f, 278f, 430f, addresses.Length * 42f),
                            Mathf.Clamp(_localAddressIndex, 0, addresses.Length - 1), addresses, 1, _choiceStyle);
                        GUI.Label(new Rect(560f, 278f, 400f, 36f), $"端口：{_session.Port}", _valueStyle);
                        if (GUI.Button(new Rect(560f, 334f, 250f, 44f), "复制所选 IP", _secondaryButtonStyle))
                            GUIUtility.systemCopyBuffer = addresses[_localAddressIndex];
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
                        "房主创建房间后发送自动检测到的 IP；队友使用该 IP 加入。", _roomBodyStyle);
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
                    GUI.enabled = idle;
                    if (!creating)
                    {
                        GUI.Label(new Rect(70f, 238f, 430f, 24f), "好友发送的房主 IPv4", _subtitleStyle);
                        GUI.SetNextControlName("RoomAddress");
                        _roomAddress = GUI.TextField(new Rect(70f, 272f, 430f, 48f), _roomAddress, 64, _roomInputStyle);
                    }
                    else
                    {
                        GUI.Label(new Rect(70f, 248f, 430f, 70f),
                            "无需填写本机 IP。创建后会自动显示可发送给好友的地址。", _roomBodyStyle);
                    }

                    GUI.Label(new Rect(560f, 238f, 400f, 24f), "房间端口", _subtitleStyle);
                    GUI.SetNextControlName("RoomPort");
                    _roomPort = GUI.TextField(new Rect(560f, 272f, 400f, 48f), _roomPort, 5, _roomInputStyle);
                    if (GUI.Button(new Rect(560f, 348f, 400f, 58f), creating ? "确认创建" : "确认加入", _buttonStyle))
                        StartRoom(creating, _roomAddress, _roomPort);
                    GUI.enabled = true;
                    GUI.Label(new Rect(70f, 422f, 890f, 44f), state, _roomBodyStyle);
                    GUI.enabled = _session != null && !idle;
                    if (GUI.Button(new Rect(70f, 486f, 280f, 44f), "取消连接", _secondaryButtonStyle)) DisconnectRoom();
                    GUI.enabled = true;
                    if (GUI.Button(new Rect(730f, 486f, 280f, 44f), "返回", _secondaryButtonStyle))
                        _lobbySetupMode = LobbySetupMode.ChooseAction;
                }
                GUI.Label(new Rect(502f, 612f, 536f, 24f), "[ F1 ] 联机房间    [ ESC ] 返回", _eyebrowStyle);
            }
            finally { GUI.matrix = previousMatrix; GUI.enabled = previousEnabled; }
        }

        private static string FindLocalAddresses()
        {
            try
            {
                var addresses = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.Address.ToString()).Distinct().Take(3).ToArray();
                return addresses.Length == 0 ? "未检测到局域网地址" : string.Join("\n", addresses);
            }
            catch (NetworkInformationException) { return "无法读取本机地址"; }
        }
    }
}

using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FunGame.Networking;
using UnityEngine;

namespace FunGame.UI
{
    public sealed partial class GameMenuController
    {
        private NetworkSessionController _session;
        private string _roomAddress = NetworkEndpointRules.DefaultAddress;
        private string _roomPort = NetworkEndpointRules.DefaultPort.ToString();
        private string _localAddresses;
        private bool _enterWhenConnected;
        private bool _hadConnectedSession;
        private GUIStyle _roomInputStyle;
        private GUIStyle _roomBodyStyle;
        private int _localAddressIndex;

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

                GUI.Box(new Rect(42f, 158f, 420f, 368f), GUIContent.none, _contentStyle);
                GUI.Label(new Rect(64f, 178f, 370f, 32f), "同一艘巨构，一起完成三章任务", _sectionStyle);
                GUI.Label(new Rect(64f, 230f, 368f, 100f),
                    "房主：点击创建房间，进入地图等待队友。\n\n队友：填写房主的 IPv4 地址和相同端口，点击加入房间。", _roomBodyStyle);
                GUI.Label(new Rect(64f, 350f, 368f, 24f), "本机局域网 IPv4", _subtitleStyle);
                string[] addresses = (_localAddresses ?? "未检测到局域网地址").Split('\n');
                _localAddressIndex = GUI.SelectionGrid(new Rect(64f, 380f, 368f, addresses.Length * 34f),
                    Mathf.Clamp(_localAddressIndex, 0, addresses.Length - 1), addresses, 1, _choiceStyle);
                if (GUI.Button(new Rect(64f, 486f, 190f, 34f), "复制所选地址", _secondaryButtonStyle))
                    GUIUtility.systemCopyBuffer = addresses[_localAddressIndex];
                GUI.Label(new Rect(42f, 545f, 420f, 66f),
                    "适用于同一局域网或已互通的虚拟局域网。\n同机测试可填 127.0.0.1。", _roomBodyStyle);

                bool idle = _session != null && _session.IsEndpointEditable;
                bool connected = HasConnectedSession;
                GUI.Label(new Rect(502f, 158f, 536f, 28f), "连接设置", _sectionStyle);
                GUI.Label(new Rect(502f, 203f, 536f, 24f), "房主 IPv4 地址（仅加入房间时填写）", _subtitleStyle);
                GUI.enabled = idle;
                GUI.SetNextControlName("RoomAddress");
                _roomAddress = GUI.TextField(new Rect(502f, 235f, 536f, 48f), _roomAddress, 64, _roomInputStyle);
                GUI.Label(new Rect(502f, 296f, 536f, 24f), "房间端口", _subtitleStyle);
                GUI.SetNextControlName("RoomPort");
                _roomPort = GUI.TextField(new Rect(502f, 328f, 536f, 48f), _roomPort, 5, _roomInputStyle);
                if (GUI.Button(new Rect(502f, 400f, 258f, 58f), "创建房间  →", _buttonStyle)) StartRoom(true, _roomAddress, _roomPort);
                if (GUI.Button(new Rect(780f, 400f, 258f, 58f), "加入房间  →", _buttonStyle)) StartRoom(false, _roomAddress, _roomPort);
                GUI.enabled = true;
                string state = _session == null ? "联机服务尚未就绪，请返回主菜单重试。" : _session.StatusText;
                if (connected) state = _session.IsHost
                    ? $"房间已开启 · 已连接 {_session.ConnectedPlayerCount} 人 · 端口 {_session.Port}"
                    : $"已连接房主 {_session.Address}:{_session.Port}";
                GUI.Label(new Rect(502f, 475f, 536f, 64f), state, _roomBodyStyle);
                GUI.enabled = _session != null && !idle;
                if (GUI.Button(new Rect(502f, 552f, 258f, 48f), connected ? "断开连接" : "取消连接", _secondaryButtonStyle)) DisconnectRoom();
                GUI.enabled = true;
                if (GUI.Button(new Rect(780f, 552f, 258f, 48f), connected ? "返回游戏" : "返回主菜单", _secondaryButtonStyle))
                {
                    if (connected) CloseMenu();
                    else { DisconnectRoom(); OpenMenu(MenuPage.Main); }
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

using FunGame.Networking;
using UnityEngine;

namespace FunGame.UI
{
    public sealed partial class GameMenuController
    {
        private HostFirewallPreparation _hostFirewall;
        public HostFirewallState FirewallState => _hostFirewall?.State ?? HostFirewallState.Idle;
        private bool IsPreparingHost => _hostFirewall != null &&
            (_hostFirewall.IsBusy || FirewallState == HostFirewallState.NeedsConsent);

        public void ConfigureHostFirewallAccess(IHostFirewallAccess access)
        {
            CancelHostPreparation();
            _hostFirewall = new HostFirewallPreparation(access);
        }

        // The interactive entry checks Windows access. StartRoom remains the transport entry
        // used by headless diagnostics, which must never display a UAC dialog.
        public void RequestRoomFromMenu(bool host, string address, string port)
        {
            if (!IsNetworkLobbyOpen || _session == null || IsPreparingHost) return;
            CancelHostPreparation();
            if (!host) { StartRoom(false, address, port); return; }
            _lobbySetupMode = LobbySetupMode.CreateRoom;
            _roomPort = port;
            if (!_session.TrySetHostPortInput(port)) return;
            if (_hostFirewall == null && Application.platform == RuntimePlatform.WindowsPlayer)
            {
                using (var process = System.Diagnostics.Process.GetCurrentProcess())
                    ConfigureHostFirewallAccess(new WindowsHostFirewall(process.MainModule.FileName));
            }
            if (_hostFirewall == null) { StartRoom(true, address, port); return; }
            NetworkEndpointRules.TryNormalizePort(port, out ushort normalizedPort, out _);
            _hostFirewall.Begin(normalizedPort);
        }

        public void AuthorizeHostFirewall() => _hostFirewall?.Authorize();
        private void CancelHostPreparation() => _hostFirewall?.Cancel();

        private void UpdateHostPreparation()
        {
            if (_hostFirewall == null || FirewallState != HostFirewallState.Ready) return;
            ushort port = _hostFirewall.Port;
            CancelHostPreparation();
            if (IsNetworkLobbyOpen && !_changingScene)
                StartRoom(true, _roomAddress, port.ToString());
        }
    }
}

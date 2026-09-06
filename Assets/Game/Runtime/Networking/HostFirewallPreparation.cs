using System;
using System.Threading.Tasks;

namespace FunGame.Networking
{
    public enum HostFirewallState { Idle, Checking, NeedsConsent, Configuring, Ready, Failed }

    /// <summary>Menu-owned preparation; stale async completions cannot start a room after leaving.</summary>
    public sealed class HostFirewallPreparation
    {
        private readonly IHostFirewallAccess access;
        private int generation;
        public HostFirewallState State { get; private set; }
        public ushort Port { get; private set; }
        public string Message { get; private set; } = string.Empty;
        public bool IsBusy => State == HostFirewallState.Checking || State == HostFirewallState.Configuring;
        public Task PendingOperation { get; private set; } = Task.CompletedTask;

        public HostFirewallPreparation(IHostFirewallAccess access) => this.access = access;

        public void Begin(ushort port)
        {
            if (IsBusy) return;
            if (port == 0) throw new ArgumentOutOfRangeException(nameof(port));
            Port = port;
            State = HostFirewallState.Checking;
            Message = "正在检查联机访问权限…";
            PendingOperation = CompleteAsync(++generation, false);
        }

        public void Authorize()
        {
            if (State != HostFirewallState.NeedsConsent) return;
            State = HostFirewallState.Configuring;
            Message = "请在 Windows 授权窗口中确认，完成后会自动创建房间。";
            PendingOperation = CompleteAsync(generation, true);
        }

        public void Cancel()
        {
            generation++;
            State = HostFirewallState.Idle;
            Message = string.Empty;
        }

        private async Task CompleteAsync(int request, bool configure)
        {
            HostFirewallResult result;
            try { result = await (configure ? access.ConfigureAsync(Port) : access.CheckAsync(Port)); }
            catch (Exception) { result = HostFirewallResult.Failed; }
            if (request != generation) return;
            if (result == HostFirewallResult.Allowed)
            {
                State = HostFirewallState.Ready;
                Message = string.Empty;
            }
            else if (result == HostFirewallResult.Missing && !configure)
            {
                State = HostFirewallState.NeedsConsent;
                Message = $"需要允许好友连接端口 {Port}（含公用网络）。仅本次运行有效，退出后自动清理。";
            }
            else
            {
                State = HostFirewallState.Failed;
                Message = result == HostFirewallResult.Cancelled
                    ? "已取消 Windows 授权，房间尚未创建。可以重新点击确认创建。"
                    : "联机权限检查或配置失败，房间尚未创建。请检查系统策略后重试。";
            }
        }
    }
}

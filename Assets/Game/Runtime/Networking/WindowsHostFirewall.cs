using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FunGame.Networking
{
    public enum HostFirewallResult { Allowed, Missing, Cancelled, Failed }

    public interface IHostFirewallAccess
    {
        Task<HostFirewallResult> CheckAsync(ushort port);
        Task<HostFirewallResult> ConfigureAsync(ushort port);
    }

    /// <summary>Uses the bundled Windows helper directly; no shell or script interpreter.</summary>
    public sealed class WindowsHostFirewall : IHostFirewallAccess
    {
        private readonly string helperPath;
        private readonly int playerId;
        private readonly long playerStartTicks;

        public WindowsHostFirewall(string executable)
        {
            string path = Path.GetFullPath(executable);
            if (!string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A Windows player executable is required.", nameof(executable));
            helperPath = Path.Combine(Path.GetDirectoryName(path), HostFirewallIdentity.HelperFileName);
            using (Process player = Process.GetCurrentProcess())
            {
                playerId = player.Id;
                playerStartTicks = player.StartTime.ToUniversalTime().Ticks;
            }
        }

        public Task<HostFirewallResult> CheckAsync(ushort port) => Task.Run(() => Check(port));

        public Task<HostFirewallResult> ConfigureAsync(ushort port) => Task.Run(() =>
        {
            try
            {
                if (!File.Exists(helperPath)) return HostFirewallResult.Failed;
                using (Process helper = Process.Start(StartInfo(port, true)))
                {
                    if (helper == null) return HostFirewallResult.Failed;
                    // The helper owns the dynamic rule until the original player process exits.
                    var timer = Stopwatch.StartNew();
                    while (timer.Elapsed.TotalSeconds < 45)
                    {
                        HostFirewallResult result = Check(port);
                        if (result == HostFirewallResult.Allowed) return result;
                        if (helper.HasExited) return HostFirewallResult.Failed;
                        Thread.Sleep(250);
                    }
                    return HostFirewallResult.Failed;
                }
            }
            catch (Win32Exception exception)
            {
                return exception.NativeErrorCode == 1223 ? HostFirewallResult.Cancelled : HostFirewallResult.Failed;
            }
            catch (Exception) { return HostFirewallResult.Failed; }
        });

        public static string RuleName(string executable, ushort port, int playerId, long playerStartTicks) =>
            HostFirewallIdentity.RuleName(executable, port, playerId, playerStartTicks);

        private HostFirewallResult Check(ushort port)
        {
            try
            {
                if (!File.Exists(helperPath)) return HostFirewallResult.Failed;
                using (Process process = Process.Start(StartInfo(port, false)))
                {
                    if (process == null) return HostFirewallResult.Failed;
                    if (!process.WaitForExit(20000))
                    {
                        process.Kill();
                        return HostFirewallResult.Failed;
                    }
                    return process.ExitCode == 0 ? HostFirewallResult.Allowed
                        : process.ExitCode == 10 ? HostFirewallResult.Missing : HostFirewallResult.Failed;
                }
            }
            catch (Exception) { return HostFirewallResult.Failed; }
        }

        private ProcessStartInfo StartInfo(ushort port, bool elevated) => new ProcessStartInfo
        {
            FileName = helperPath,
            Arguments = HostFirewallIdentity.Arguments(elevated, port, playerId, playerStartTicks),
            UseShellExecute = elevated,
            CreateNoWindow = !elevated,
            WindowStyle = ProcessWindowStyle.Hidden,
            Verb = elevated ? "runas" : string.Empty
        };
    }
}

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

    /// <summary>
    /// An elevated helper owns an ActiveStore rule and watches this exact player process.
    /// Normal exit and player crashes trigger cleanup; reboot also discards the dynamic rule.
    /// </summary>
    public sealed class WindowsHostFirewall : IHostFirewallAccess
    {
        private readonly string executable;
        private readonly int playerId;
        private readonly long playerStartTicks;

        public WindowsHostFirewall(string executable)
        {
            this.executable = Path.GetFullPath(executable);
            if (!string.Equals(Path.GetExtension(this.executable), ".exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A Windows player executable is required.", nameof(executable));
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
                using (Process helper = Process.Start(StartInfo(Script(port, true), true)))
                {
                    if (helper == null) return HostFirewallResult.Failed;
                    // The helper lives until the game exits; read the real rule for readiness.
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

        private string Script(ushort port, bool configure) =>
            BuildScript(executable, port, playerId, playerStartTicks, configure);

        public static string RuleName(string executable, ushort port, int playerId, long playerStartTicks)
        {
            if (port == 0) throw new ArgumentOutOfRangeException(nameof(port));
            if (playerId <= 0 || playerStartTicks <= 0) throw new ArgumentOutOfRangeException(nameof(playerId));
            using (SHA256 hash = SHA256.Create())
                return "FunGame-Session-" + BitConverter.ToString(hash.ComputeHash(
                        Encoding.UTF8.GetBytes(Path.GetFullPath(executable).ToUpperInvariant())))
                    .Replace("-", "").Substring(0, 16) + "-" + playerId + "-" + playerStartTicks + "-UDP-" + port;
        }

        public static string BuildScript(string executable, ushort port, int playerId, long playerStartTicks, bool configure)
        {
            string path = Path.GetFullPath(executable);
            string name = RuleName(path, port, playerId, playerStartTicks);
            // Single-quoted literals + UTF-16 EncodedCommand preserve Unicode and metacharacters.
            // Port and process identifiers are numeric, never player-supplied command text.
            return "$ErrorActionPreference = 'Stop'\n$ProgressPreference = 'SilentlyContinue'\n$created = $false\ntry {\n" +
                "$exe = '" + path.Replace("'", "''") + "'\n" +
                "$name = '" + name + "'\n$port = '" + port + "'\n" +
                "$playerId = " + playerId + "\n$playerTicks = " + playerStartTicks + "\n" +
                @"$game = Get-Process -Id $playerId -ErrorAction Stop
if ($game.StartTime.ToUniversalTime().Ticks -ne $playerTicks -or $game.Path -ine $exe) { exit 20 }
function Test-GameRule {
    $r = Get-NetFirewallRule -PolicyStore ActiveStore -Name $name -ErrorAction SilentlyContinue
    if ($null -eq $r -or $r.PolicyStoreSourceType -ne 'Dynamic' -or
        $r.Enabled -ne 'True' -or $r.Direction -ne 'Inbound' -or $r.Action -ne 'Allow' -or
        ($r.Profile -ne 'Any' -and (([int]$r.Profile -band 7) -ne 7))) { return $false }
    $app = $r | Get-NetFirewallApplicationFilter
    $ports = $r | Get-NetFirewallPortFilter
    $addresses = $r | Get-NetFirewallAddressFilter
    $interfaces = $r | Get-NetFirewallInterfaceFilter
    return ($app.Program -ieq $exe -and $ports.Protocol -eq 'UDP' -and
        $ports.LocalPort -eq $port -and $ports.RemotePort -eq 'Any' -and
        $addresses.LocalAddress -eq 'Any' -and $addresses.RemoteAddress -eq 'Any' -and
        $interfaces.InterfaceAlias -eq 'Any')
}
if (Test-GameRule) { exit 0 }
" + (configure ? @"
New-NetFirewallRule -PolicyStore ActiveStore -Name $name -DisplayName $name `
    -Description 'FunGame: this player session only; removed when the game exits.' `
    -Group 'FunGame Session Hosting' -Program $exe -Protocol UDP -LocalPort $port `
    -Direction Inbound -Action Allow -Profile Any -Enabled True | Out-Null
$created = $true
if (-not (Test-GameRule)) { exit 20 }
# This process handle avoids confusing a reused PID with the original game.
$game.WaitForExit()
" : "exit 10\n") + @"
} catch { exit 20 }
finally {
    if ($created) {
        Remove-NetFirewallRule -PolicyStore ActiveStore -Name $name -ErrorAction SilentlyContinue
    }
}
";
        }

        private HostFirewallResult Check(ushort port)
        {
            try
            {
                using (Process process = Process.Start(StartInfo(Script(port, false), false)))
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

        private static ProcessStartInfo StartInfo(string script, bool elevated) => new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"WindowsPowerShell\v1.0\powershell.exe"),
            Arguments = "-NoLogo -NoProfile -NonInteractive -EncodedCommand " +
                Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
            UseShellExecute = elevated,
            CreateNoWindow = !elevated,
            WindowStyle = ProcessWindowStyle.Hidden,
            Verb = elevated ? "runas" : string.Empty
        };
    }
}

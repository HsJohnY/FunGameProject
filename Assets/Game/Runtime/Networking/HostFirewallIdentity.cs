using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FunGame.Networking
{
    // Shared with the standalone helper; keep compatible with the Windows .NET Framework compiler.
    public static class HostFirewallIdentity
    {
        public const string HelperFileName = "FunGame.Firewall.exe";

        public static string RuleName(string executable, ushort port, int playerId, long playerStartTicks)
        {
            Validate(port, playerId, playerStartTicks);
            using (SHA256 hash = SHA256.Create())
                return "FunGame-Session-" + BitConverter.ToString(hash.ComputeHash(
                        Encoding.UTF8.GetBytes(Path.GetFullPath(executable).ToUpperInvariant())))
                    .Replace("-", "").Substring(0, 16) + "-" + playerId.ToString(CultureInfo.InvariantCulture) +
                    "-" + playerStartTicks.ToString(CultureInfo.InvariantCulture) + "-UDP-" + port.ToString(CultureInfo.InvariantCulture);
        }

        public static string Arguments(bool configure, ushort port, int playerId, long playerStartTicks)
        {
            Validate(port, playerId, playerStartTicks);
            return (configure ? "--grant " : "--check ") + playerId.ToString(CultureInfo.InvariantCulture) +
                " " + playerStartTicks.ToString(CultureInfo.InvariantCulture) + " " + port.ToString(CultureInfo.InvariantCulture);
        }

        private static void Validate(ushort port, int playerId, long playerStartTicks)
        {
            if (port == 0) throw new ArgumentOutOfRangeException("port");
            if (playerId <= 0 || playerStartTicks <= 0) throw new ArgumentOutOfRangeException("playerId");
        }
    }
}

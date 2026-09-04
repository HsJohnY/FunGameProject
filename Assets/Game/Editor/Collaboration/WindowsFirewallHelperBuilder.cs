using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FunGame.Networking;
using UnityEditor.Build;

namespace FunGame.Editor
{
    public static class WindowsFirewallHelperBuilder
    {
        public static void Build(string playerExecutable)
        {
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string compiler = Path.Combine(windows, @"Microsoft.NET\Framework64\v4.0.30319\csc.exe");
            string infrastructure = Path.Combine(windows, @"Microsoft.NET\assembly\GAC_MSIL\Microsoft.Management.Infrastructure");
            if (!File.Exists(compiler) || !Directory.Exists(infrastructure))
                throw new BuildFailedException("Windows .NET Framework and Management Infrastructure are required to build the firewall helper.");
            string reference = Directory.GetFiles(infrastructure, "Microsoft.Management.Infrastructure.dll", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.Ordinal).FirstOrDefault();
            if (reference == null) throw new BuildFailedException("Windows Management Infrastructure reference is missing.");
            string output = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(playerExecutable)), HostFirewallIdentity.HelperFileName);
            string source = Path.GetFullPath("Tools/WindowsFirewallHelper/Program.cs");
            string identity = Path.GetFullPath("Assets/Game/Runtime/Networking/HostFirewallIdentity.cs");
            string manifest = Path.GetFullPath("Tools/WindowsFirewallHelper/app.manifest");
            var start = new ProcessStartInfo
            {
                FileName = compiler,
                Arguments = "/nologo /target:winexe /platform:x64 /optimize+ /out:" + Quote(output) +
                    " /win32manifest:" + Quote(manifest) + " /reference:" + Quote(reference) +
                    " /reference:System.Core.dll " + Quote(source) + " " + Quote(identity),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true
            };
            using (Process process = Process.Start(start))
            {
                if (process == null) throw new BuildFailedException("Could not start the firewall helper compiler.");
                string diagnostics = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0) throw new BuildFailedException("Firewall helper compilation failed: " + diagnostics);
            }
            UnityEngine.Debug.Log("[Firewall] Bundled native Windows helper: " + output);
        }

        private static string Quote(string value) => "\"" + value + "\"";
    }
}

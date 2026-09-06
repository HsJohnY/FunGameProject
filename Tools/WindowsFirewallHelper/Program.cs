using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using FunGame.Networking;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using Microsoft.Win32.SafeHandles;

[assembly: AssemblyTitle("FunGame Firewall Access")]
[assembly: AssemblyDescription("Temporary network access for a running FunGame player")]
[assembly: AssemblyProduct("FunGame")]
[assembly: AssemblyVersion("1.0.0.0")]

internal static class Program
{
    private const string Namespace = "root/StandardCimv2";

    private static int Main(string[] args)
    {
        try
        {
            int id;
            long ticks;
            ushort port;
            if (args.Length != 4 || (args[0] != "--check" && args[0] != "--grant") ||
                !int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out id) || id <= 0 ||
                !long.TryParse(args[2], NumberStyles.None, CultureInfo.InvariantCulture, out ticks) || ticks <= 0 ||
                !ushort.TryParse(args[3], NumberStyles.None, CultureInfo.InvariantCulture, out port) || port == 0)
                return 20;

            using (Process game = Process.GetProcessById(id))
            {
                if (game.StartTime.ToUniversalTime().Ticks != ticks || game.HasExited) return 20;
                string executable = game.MainModule.FileName;
                string directory = Path.GetDirectoryName(executable);
                // Accept only a live Unity player beside this helper, never a caller-supplied path.
                if (!string.Equals(directory, AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar),
                        StringComparison.OrdinalIgnoreCase) ||
                    !Directory.Exists(Path.Combine(directory, Path.GetFileNameWithoutExtension(executable) + "_Data")))
                    return 20;
                string name = HostFirewallIdentity.RuleName(executable, port, id, ticks);
                if (args[0] == "--check") return CheckOwner(name);
                using (var ownership = new SessionOwnership(name))
                using (var dcom = new DComSessionOptions())
                using (CimSession session = CimSession.Create(null, dcom))
                using (CimOperationOptions options = StoreOptions())
                {
                    if (!ownership.Acquired) return CheckOwner(name);
                    bool created = false;
                    try
                    {
                        if (IsReady(session, options, name, executable, port))
                        {
                            // Adopt a leftover dynamic rule only for this exact still-live player.
                            created = true;
                        }
                        else
                        using (var instance = new CimInstance("MSFT_NetFirewallRule", Namespace))
                        using (CimOperationOptions createOptions = StoreOptions())
                        {
                            Add(instance, "InstanceID", name);
                            Add(instance, "ElementName", name);
                            Add(instance, "Description", "FunGame session only; removed when its player exits.");
                            Add(instance, "RuleGroup", "FunGame Session Hosting");
                            Add(instance, "Enabled", (ushort)1);
                            Add(instance, "Profiles", (ushort)0); // Any profile, including virtual LANs marked Public.
                            Add(instance, "Direction", (ushort)1);
                            Add(instance, "Action", (ushort)2);
                            Add(instance, "EdgeTraversalPolicy", (ushort)0);
                            createOptions.SetCustomOption("Program", executable, false);
                            createOptions.SetCustomOption("Protocol", "UDP", false);
                            createOptions.SetCustomOption("LocalPort", new[] { port.ToString(CultureInfo.InvariantCulture) }, CimType.StringArray, false);
                            using (CimInstance added = session.CreateInstance(Namespace, instance, createOptions)) { }
                            created = true;
                        }
                        if (!IsReady(session, options, name, executable, port)) return 22;
                        ServeChecks(game, name, () => IsReady(session, options, name, executable, port));
                    }
                    finally
                    {
                        if (created) Remove(session, options, name);
                    }
                }
            }
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 21;
        }
    }

    private static CimOperationOptions StoreOptions()
    {
        var options = new CimOperationOptions { Timeout = TimeSpan.FromSeconds(10) };
        options.SetCustomOption("PolicyStore", "ActiveStore", false);
        return options;
    }

    private static void Add(CimInstance instance, string name, object value)
    {
        instance.CimInstanceProperties.Add(CimProperty.Create(name, value, CimFlags.None));
    }

    private static CimInstance Find(CimSession session, CimOperationOptions options, string type, string name)
    {
        // name is generated exclusively from a hex digest and numeric process/port values.
        return session.QueryInstances(Namespace, "WQL", "SELECT * FROM " + type + " WHERE InstanceID='" + name + "'", options)
            .FirstOrDefault();
    }

    private static object Value(CimInstance instance, string property)
    {
        return instance == null ? null : instance.CimInstanceProperties[property].Value;
    }

    private static bool IsAny(object value)
    {
        var values = value as string[];
        return value == null || (values != null && (values.Length == 0 ||
            (values.Length == 1 && (values[0] == "Any" || values[0] == "*"))));
    }

    private static bool IsReady(CimSession session, CimOperationOptions options, string name, string executable, ushort port)
    {
        using (CimInstance rule = Find(session, options, "MSFT_NetFirewallRule", name))
        {
            if (rule == null || Convert.ToInt32(Value(rule, "PolicyStoreSourceType")) != 3 ||
                Convert.ToInt32(Value(rule, "Enabled")) != 1 || Convert.ToInt32(Value(rule, "Action")) != 2 ||
                Convert.ToInt32(Value(rule, "Direction")) != 1 || Convert.ToInt32(Value(rule, "Profiles")) != 0)
                return false;
        }
        using (CimInstance app = Find(session, options, "MSFT_NetApplicationFilter", name))
        using (CimInstance ports = Find(session, options, "MSFT_NetProtocolPortFilter", name))
        using (CimInstance addresses = Find(session, options, "MSFT_NetAddressFilter", name))
        using (CimInstance interfaces = Find(session, options, "MSFT_NetInterfaceFilter", name))
        using (CimInstance service = Find(session, options, "MSFT_NetServiceFilter", name))
        {
            var localPorts = Value(ports, "LocalPort") as string[];
            return app != null && ports != null && addresses != null && interfaces != null && service != null &&
                string.Equals(Convert.ToString(Value(app, "AppPath")), executable, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrEmpty(Convert.ToString(Value(app, "Package"))) &&
                Convert.ToString(Value(ports, "Protocol")) == "UDP" && localPorts != null && localPorts.Length == 1 &&
                localPorts[0] == port.ToString(CultureInfo.InvariantCulture) && IsAny(Value(ports, "RemotePort")) &&
                IsAny(Value(addresses, "LocalAddress")) && IsAny(Value(addresses, "RemoteAddress")) &&
                IsAny(Value(interfaces, "InterfaceAlias")) && string.IsNullOrEmpty(Convert.ToString(Value(service, "ServiceName")));
        }
    }

    private static int CheckOwner(string name)
    {
        // The privileged rule owner answers a read-only status query. No commands, paths,
        // ports or other mutable data are accepted over this channel.
        try
        {
            using (var pipe = new NamedPipeClientStream(".", name, PipeDirection.In, PipeOptions.Asynchronous))
            {
                pipe.Connect(1500);
                byte[] status = new byte[1];
                IAsyncResult read = pipe.BeginRead(status, 0, 1, null, null);
                using (WaitHandle pending = read.AsyncWaitHandle)
                {
                    if (!pending.WaitOne(10000)) return 21;
                    return pipe.EndRead(read) == 1 ? status[0] : 21;
                }
            }
        }
        catch (TimeoutException) { return 10; }
        catch (IOException) { return 10; }
    }

    private static void ServeChecks(Process game, string name, Func<bool> check)
    {
        IntPtr token;
        if (!OpenProcessToken(game.Handle, 8, out token)) throw new System.ComponentModel.Win32Exception();
        SecurityIdentifier owner;
        try { using (var identity = new WindowsIdentity(token)) owner = identity.User; }
        finally { CloseHandle(token); }
        var security = new PipeSecurity();
        security.SetAccessRuleProtection(true, false);
        security.AddAccessRule(new PipeAccessRule(owner, PipeAccessRights.Read, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User, PipeAccessRights.FullControl, AccessControlType.Allow));
        using (var stopped = new PlayerExitSignal(game))
        {
            while (!game.HasExited)
            {
                using (var pipe = new NamedPipeServerStream(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous, 128, 128, security))
                {
                    IAsyncResult connection = pipe.BeginWaitForConnection(null, null);
                    using (WaitHandle pending = connection.AsyncWaitHandle)
                    {
                        if (WaitHandle.WaitAny(new WaitHandle[] { pending, stopped }) == 1) break;
                        pipe.EndWaitForConnection(connection);
                    }
                    try { pipe.WriteByte(check() ? (byte)0 : (byte)10); }
                    catch (IOException) { }
                }
            }
        }
    }

    private sealed class PlayerExitSignal : WaitHandle
    {
        public PlayerExitSignal(Process game)
        {
            // The Process owns this handle; the wait wrapper must not close it.
            SafeWaitHandle = new SafeWaitHandle(game.Handle, false);
        }
    }

    private sealed class SessionOwnership : IDisposable
    {
        private readonly Mutex mutex;
        public readonly bool Acquired;

        public SessionOwnership(string name)
        {
            mutex = new Mutex(false, "Local\\" + name + "-Owner");
            try { Acquired = mutex.WaitOne(0); }
            catch (AbandonedMutexException) { Acquired = true; }
        }

        public void Dispose()
        {
            if (Acquired) mutex.ReleaseMutex();
            mutex.Dispose();
        }
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    private static void Remove(CimSession session, CimOperationOptions options, string name)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using (CimInstance rule = Find(session, options, "MSFT_NetFirewallRule", name))
                {
                    if (rule != null) session.DeleteInstance(Namespace, rule, options);
                    return;
                }
            }
            catch (CimException) { if (attempt == 2) throw; Thread.Sleep(500); }
        }
    }
}

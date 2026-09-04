using System;
using System.Threading.Tasks;
using FunGame.Networking;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class HostFirewallPreparationTests
    {
        private sealed class Access : IHostFirewallAccess
        {
            public Task<HostFirewallResult> Check = Task.FromResult(HostFirewallResult.Missing);
            public Task<HostFirewallResult> Configure = Task.FromResult(HostFirewallResult.Allowed);
            public int Checks, Configurations;
            public ushort LastPort;
            public Task<HostFirewallResult> CheckAsync(ushort port) { Checks++; LastPort = port; return Check; }
            public Task<HostFirewallResult> ConfigureAsync(ushort port) { Configurations++; LastPort = port; return Configure; }
        }

        [Test]
        public void MissingRule_RequiresExplicitConsent_AndDoesNotElevateDuringCheck()
        {
            var access = new Access();
            var flow = new HostFirewallPreparation(access);
            flow.Begin(4848);
            Assert.That(flow.State, Is.EqualTo(HostFirewallState.NeedsConsent));
            Assert.That(access.Configurations, Is.Zero);
            flow.Authorize();
            Assert.That(flow.State, Is.EqualTo(HostFirewallState.Ready));
            Assert.That(access.Configurations, Is.EqualTo(1));
            Assert.That(access.LastPort, Is.EqualTo(4848));
        }

        [Test]
        public void ExistingSessionRule_DoesNotRequestElevation()
        {
            var access = new Access { Check = Task.FromResult(HostFirewallResult.Allowed) };
            var flow = new HostFirewallPreparation(access);
            flow.Begin(4848);
            flow.Authorize();
            Assert.That(flow.State, Is.EqualTo(HostFirewallState.Ready));
            Assert.That(access.Configurations, Is.Zero);
        }

        [TestCase(HostFirewallResult.Cancelled)]
        [TestCase(HostFirewallResult.Failed)]
        [TestCase(HostFirewallResult.Missing)]
        public void UnsuccessfulConfiguration_DoesNotBecomeReady_AndCanRetry(HostFirewallResult result)
        {
            var access = new Access { Configure = Task.FromResult(result) };
            var flow = new HostFirewallPreparation(access);
            flow.Begin(4848);
            flow.Authorize();
            Assert.That(flow.State, Is.EqualTo(HostFirewallState.Failed));
            Assert.That(flow.Message, Is.Not.Empty);
            flow.Begin(7777);
            Assert.That(flow.State, Is.EqualTo(HostFirewallState.NeedsConsent));
            Assert.That(flow.Port, Is.EqualTo(7777));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task LeavingDuringAsyncOperation_IgnoresLateSuccess(bool duringConfiguration)
        {
            var pending = new TaskCompletionSource<HostFirewallResult>();
            var access = new Access();
            if (duringConfiguration) access.Configure = pending.Task;
            else access.Check = pending.Task;
            var flow = new HostFirewallPreparation(access);
            flow.Begin(4848);
            if (duringConfiguration) flow.Authorize();
            Task operation = flow.PendingOperation;
            flow.Cancel();
            pending.SetResult(HostFirewallResult.Allowed);
            await operation;
            Assert.That(flow.State, Is.EqualTo(HostFirewallState.Idle));
        }

        [Test]
        public async Task RepeatedClicks_DoNotDuplicatePendingOperation()
        {
            var pending = new TaskCompletionSource<HostFirewallResult>();
            var access = new Access { Check = pending.Task };
            var flow = new HostFirewallPreparation(access);
            flow.Begin(4848);
            flow.Begin(7777);
            Assert.That(access.Checks, Is.EqualTo(1));
            pending.SetResult(HostFirewallResult.Missing);
            await flow.PendingOperation;
            access.Configure = new TaskCompletionSource<HostFirewallResult>().Task;
            flow.Authorize();
            flow.Authorize();
            Assert.That(access.Configurations, Is.EqualTo(1));
            Assert.That(flow.Port, Is.EqualTo(4848));
            flow.Cancel();
        }

        [Test]
        public void RuleIdentity_IsScopedToExecutablePortAndProcessStart()
        {
            const string path = @"C:\Games\FunGame.exe";
            string name = WindowsHostFirewall.RuleName(path, 4848, 100, 500);
            Assert.That(WindowsHostFirewall.RuleName(path.ToLowerInvariant(), 4848, 100, 500), Is.EqualTo(name));
            Assert.That(WindowsHostFirewall.RuleName(path, 7777, 100, 500), Is.Not.EqualTo(name));
            Assert.That(WindowsHostFirewall.RuleName(path, 4848, 100, 501), Is.Not.EqualTo(name));
            Assert.That(WindowsHostFirewall.RuleName(@"C:\Other\FunGame.exe", 4848, 100, 500), Is.Not.EqualTo(name));
            Assert.Throws<ArgumentOutOfRangeException>(() => WindowsHostFirewall.RuleName(path, 0, 100, 500));
        }
    }
}

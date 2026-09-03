using FunGame.Incident;
using FunGame.Networking;
using FunGame.Tools;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class NetworkIncidentAuthorityTests
    {
        [TestCase(NetworkIncidentAction.SealLeak, CoolingIncidentPhase.ContainLeak, ToolKind.SealantGun)]
        [TestCase(NetworkIncidentAction.OperateFastener, CoolingIncidentPhase.LoosenConnection, ToolKind.ImpactWrench)]
        [TestCase(NetworkIncidentAction.OperateFastener, CoolingIncidentPhase.TightenConnection, ToolKind.ImpactWrench)]
        [TestCase(NetworkIncidentAction.OperatePump, CoolingIncidentPhase.ResetPump, ToolKind.None)]
        public void GetRequiredTool_返回阶段对应工具(
            NetworkIncidentAction action,
            CoolingIncidentPhase phase,
            ToolKind expected)
        {
            Assert.That(NetworkCoolingIncidentController.GetRequiredTool(action, phase), Is.EqualTo(expected));
        }

        [Test]
        public void RequiresReplacementPipe_只要求安装动作消耗任务物()
        {
            Assert.That(NetworkCoolingIncidentController.RequiresReplacementPipe(NetworkIncidentAction.InstallPipe), Is.True);
            Assert.That(NetworkCoolingIncidentController.RequiresReplacementPipe(NetworkIncidentAction.SealLeak), Is.False);
            Assert.That(NetworkCoolingIncidentController.RequiresReplacementPipe(NetworkIncidentAction.OperateFastener), Is.False);
            Assert.That(NetworkCoolingIncidentController.RequiresReplacementPipe(NetworkIncidentAction.OperatePump), Is.False);
        }
    }
}

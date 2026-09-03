using FunGame.Demo;
using FunGame.Incident;
using FunGame.Tools;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class DemoGuidanceRulesTests
    {
        [Test]
        public void 冷却诊断先标记压力表再标记泵检查面板()
        {
            DemoGuidanceInstruction first = DemoGuidanceRules.ResolveCooling(
                CoolingIncidentRunState.Active,
                CoolingIncidentPhase.AssessSymptoms,
                false,
                false,
                ToolKind.None,
                false,
                false);
            DemoGuidanceInstruction second = DemoGuidanceRules.ResolveCooling(
                CoolingIncidentRunState.Active,
                CoolingIncidentPhase.AssessSymptoms,
                true,
                false,
                ToolKind.None,
                false,
                false);

            Assert.That(first.PrimaryTarget, Is.EqualTo(DemoGuidanceTargetKind.PressureGauge));
            Assert.That(first.ActionText, Does.Contain("按 E"));
            Assert.That(second.PrimaryTarget, Is.EqualTo(DemoGuidanceTargetKind.PumpInspection));
        }

        [Test]
        public void 维修阶段缺工具时先标记对应工具架()
        {
            DemoGuidanceInstruction withoutTool = DemoGuidanceRules.ResolveCooling(
                CoolingIncidentRunState.Active,
                CoolingIncidentPhase.RestoreControlPower,
                true,
                true,
                ToolKind.None,
                false,
                false);
            DemoGuidanceInstruction withTool = DemoGuidanceRules.ResolveCooling(
                CoolingIncidentRunState.Active,
                CoolingIncidentPhase.RestoreControlPower,
                true,
                true,
                ToolKind.CircuitBridger,
                false,
                false);

            Assert.That(withoutTool.PrimaryTarget, Is.EqualTo(DemoGuidanceTargetKind.CircuitBridgerRack));
            Assert.That(withTool.PrimaryTarget, Is.EqualTo(DemoGuidanceTargetKind.CircuitInterlock));
            Assert.That(withTool.ActionText, Does.Contain("左键三次"));
        }

        [Test]
        public void 安装阶段根据是否手持管件切换目标()
        {
            DemoGuidanceInstruction pickup = DemoGuidanceRules.ResolveCooling(
                CoolingIncidentRunState.Active,
                CoolingIncidentPhase.InstallReplacementPipe,
                true,
                true,
                ToolKind.None,
                false,
                false);
            DemoGuidanceInstruction install = DemoGuidanceRules.ResolveCooling(
                CoolingIncidentRunState.Active,
                CoolingIncidentPhase.InstallReplacementPipe,
                true,
                true,
                ToolKind.None,
                true,
                false);

            Assert.That(pickup.PrimaryTarget, Is.EqualTo(DemoGuidanceTargetKind.ReplacementPipe));
            Assert.That(install.PrimaryTarget, Is.EqualTo(DemoGuidanceTargetKind.Fastener));
        }

        [Test]
        public void 防卫插曲同时提示武器架与威胁方向()
        {
            DemoGuidanceInstruction instruction = DemoGuidanceRules.ResolveCooling(
                CoolingIncidentRunState.Active,
                CoolingIncidentPhase.LoosenConnection,
                true,
                true,
                ToolKind.SealantGun,
                false,
                true);

            Assert.That(instruction.PrimaryTarget, Is.EqualTo(DemoGuidanceTargetKind.ImpactWrenchRack));
            Assert.That(instruction.SecondaryTarget, Is.EqualTo(DemoGuidanceTargetKind.Enemy));
        }

        [Test]
        public void 第二章和第三章的门禁都有明确操作目标()
        {
            DemoGuidanceInstruction relay = DemoGuidanceRules.ResolveRelay(false, 3, 2, ToolKind.CircuitBridger);
            DemoGuidanceInstruction calibration = DemoGuidanceRules.ResolveStorm(false, true, 0, ToolKind.ImpactWrench, false);
            DemoGuidanceInstruction completed = DemoGuidanceRules.ResolveStorm(false, false, 0, ToolKind.ImpactWrench, true);

            Assert.That(relay.PrimaryTarget, Is.EqualTo(DemoGuidanceTargetKind.Relay));
            Assert.That(relay.SecondaryTarget, Is.EqualTo(DemoGuidanceTargetKind.Enemy));
            Assert.That(calibration.PrimaryTarget, Is.EqualTo(DemoGuidanceTargetKind.CampaignConsole));
            Assert.That(completed.PrimaryTarget, Is.EqualTo(DemoGuidanceTargetKind.SecretPlate));
        }
    }
}

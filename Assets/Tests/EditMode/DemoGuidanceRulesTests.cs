using FunGame.Demo;
using FunGame.Incident;
using FunGame.Tools;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class DemoGuidanceRulesTests
    {
        [Test]
        public void 正后方目标在死区内保持原边避免左右乱跳()
        {
            var safeRect = new UnityEngine.Rect(100f, 50f, 1000f, 500f);
            DemoMarkerPlacement first = DemoMarkerLayout.Calculate(
                new UnityEngine.Vector3(2.8f, 0f, -5f),
                UnityEngine.Vector2.zero,
                safeRect,
                -1f);
            DemoMarkerPlacement jitter = DemoMarkerLayout.Calculate(
                new UnityEngine.Vector3(0.02f, 0f, -5f),
                UnityEngine.Vector2.zero,
                safeRect,
                first.BehindSide);

            Assert.That(first.IsEdge, Is.True);
            Assert.That(first.BehindSide, Is.EqualTo(1f));
            Assert.That(jitter.BehindSide, Is.EqualTo(1f));
            Assert.That(jitter.Position.x, Is.EqualTo(safeRect.xMax).Within(0.01f));
        }

        [Test]
        public void 背后目标忽略垂直偏差并稳定贴在左右边缘()
        {
            var safeRect = new UnityEngine.Rect(100f, 50f, 1000f, 500f);
            DemoMarkerPlacement placement = DemoMarkerLayout.Calculate(
                new UnityEngine.Vector3(0.1f, -4f, -8f),
                UnityEngine.Vector2.zero,
                safeRect,
                -1f);

            Assert.That(placement.IsEdge, Is.True);
            Assert.That(placement.BehindSide, Is.EqualTo(-1f));
            Assert.That(placement.Position.x, Is.EqualTo(safeRect.xMin).Within(0.01f));
            Assert.That(placement.Position.y, Is.EqualTo(safeRect.center.y).Within(0.01f));
        }

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

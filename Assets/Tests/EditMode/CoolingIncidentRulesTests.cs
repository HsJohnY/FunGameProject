using FunGame.Incident;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class CoolingIncidentRulesTests
    {
        [Test]
        public void 完整事故链只能按固定顺序推进()
        {
            var rules = new CoolingIncidentRules();

            Assert.That(rules.TryLoosen(), Is.False);
            Assert.That(rules.AddSealProgress(0.5f), Is.True);
            Assert.That(rules.Phase, Is.EqualTo(CoolingIncidentPhase.ContainLeak));
            Assert.That(rules.AddSealProgress(0.5f), Is.True);
            Assert.That(rules.Phase, Is.EqualTo(CoolingIncidentPhase.LoosenConnection));
            Assert.That(rules.TryLoosen(), Is.True);
            Assert.That(rules.TryInstallPipe(), Is.True);
            Assert.That(rules.TryTighten(), Is.True);
            Assert.That(rules.TryResetPump(), Is.True);
            Assert.That(rules.Phase, Is.EqualTo(CoolingIncidentPhase.Stabilized));
        }

        [Test]
        public void 密封进度会限制在完整范围内()
        {
            var rules = new CoolingIncidentRules();

            Assert.That(rules.AddSealProgress(2f), Is.True);
            Assert.That(rules.SealProgress, Is.EqualTo(1f));
            Assert.That(rules.AddSealProgress(0.1f), Is.False);
        }

        [Test]
        public void 每个阶段提供明确的当前目标()
        {
            var rules = new CoolingIncidentRules();

            Assert.That(rules.CurrentInstruction, Does.Contain("密封"));
            rules.AddSealProgress(1f);
            Assert.That(rules.CurrentInstruction, Does.Contain("松开"));
            rules.TryLoosen();
            Assert.That(rules.CurrentInstruction, Does.Contain("安装"));
        }

        [Test]
        public void 扩展事故必须收集两条诊断线索后才能开始密封()
        {
            var rules = new CoolingIncidentRules(diagnosticChecksEnabled: true);

            Assert.That(rules.Phase, Is.EqualTo(CoolingIncidentPhase.AssessSymptoms));
            Assert.That(rules.TryInspectPump(), Is.True);
            Assert.That(rules.Phase, Is.EqualTo(CoolingIncidentPhase.AssessSymptoms));
            Assert.That(rules.HasInspectedPump, Is.True);
            Assert.That(rules.TryInspectPressure(), Is.True);
            Assert.That(rules.Phase, Is.EqualTo(CoolingIncidentPhase.RestoreControlPower));
            Assert.That(rules.TryAdvanceCircuitBridge(), Is.True);
            Assert.That(rules.TryAdvanceCircuitBridge(), Is.True);
            Assert.That(rules.TryAdvanceCircuitBridge(), Is.True);
            Assert.That(rules.Phase, Is.EqualTo(CoolingIncidentPhase.ContainLeak));
        }

        [Test]
        public void 扩展事故在紧固后必须验证压力才能复位()
        {
            var rules = new CoolingIncidentRules(diagnosticChecksEnabled: true);

            rules.TryInspectPressure();
            rules.TryInspectPump();
            rules.TryAdvanceCircuitBridge();
            rules.TryAdvanceCircuitBridge();
            rules.TryAdvanceCircuitBridge();
            rules.AddSealProgress(1f);
            rules.TryLoosen();
            rules.TryInstallPipe();
            rules.TryTighten();

            Assert.That(rules.Phase, Is.EqualTo(CoolingIncidentPhase.VerifyPressure));
            Assert.That(rules.TryResetPump(), Is.False);
            Assert.That(rules.TryInspectPressure(), Is.True);
            Assert.That(rules.Phase, Is.EqualTo(CoolingIncidentPhase.ResetPump));
            Assert.That(rules.TryResetPump(), Is.True);
            Assert.That(rules.Phase, Is.EqualTo(CoolingIncidentPhase.Stabilized));
        }

        [Test]
        public void 分层提示会先描述现象再逐步给出明确动作()
        {
            var rules = new CoolingIncidentRules(diagnosticChecksEnabled: true);

            Assert.That(rules.GetGuidance(0f), Does.Contain("压力持续下降"));
            Assert.That(rules.GetGuidance(15f), Does.Contain("压力读数"));
            Assert.That(rules.GetGuidance(35f), Is.EqualTo(rules.CurrentInstruction));
        }

        [Test]
        public void 扩展事故重置会清除诊断状态并回到观察阶段()
        {
            var rules = new CoolingIncidentRules(diagnosticChecksEnabled: true);
            rules.TryInspectPressure();
            rules.TryInspectPump();

            rules.Reset();

            Assert.That(rules.Phase, Is.EqualTo(CoolingIncidentPhase.AssessSymptoms));
            Assert.That(rules.HasInspectedPressure, Is.False);
            Assert.That(rules.HasInspectedPump, Is.False);
            Assert.That(rules.CircuitBridgeProgress, Is.Zero);
        }

        [Test]
        public void 线路联锁需要三个离散步骤且不能越序推进()
        {
            var rules = new CoolingIncidentRules(diagnosticChecksEnabled: true);

            Assert.That(rules.TryAdvanceCircuitBridge(), Is.False);
            rules.TryInspectPressure();
            rules.TryInspectPump();

            Assert.That(rules.TryAdvanceCircuitBridge(), Is.True);
            Assert.That(rules.CircuitBridgeProgress, Is.EqualTo(1));
            Assert.That(rules.TryAdvanceCircuitBridge(), Is.True);
            Assert.That(rules.TryAdvanceCircuitBridge(), Is.True);
            Assert.That(rules.CircuitBridgeProgress, Is.EqualTo(CoolingIncidentRules.RequiredCircuitBridgeSteps));
            Assert.That(rules.Phase, Is.EqualTo(CoolingIncidentPhase.ContainLeak));
            Assert.That(rules.TryAdvanceCircuitBridge(), Is.False);
        }

        [TestCase(0f, "00:00")]
        [TestCase(65.9f, "01:05")]
        [TestCase(605f, "10:05")]
        public void 运行时长使用稳定的分秒格式(float seconds, string expected)
        {
            Assert.That(CoolingIncidentController.FormatDuration(seconds), Is.EqualTo(expected));
        }
    }
}

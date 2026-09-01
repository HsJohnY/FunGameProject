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
    }
}

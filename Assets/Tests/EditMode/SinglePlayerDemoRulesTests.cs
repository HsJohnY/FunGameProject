using FunGame.Demo;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class SinglePlayerDemoRulesTests
    {
        [Test]
        public void 第二章必须同时完成三处继电器与防卫()
        {
            var rules = new SinglePlayerDemoRules(3, 3);

            Assert.That(rules.CompleteCoolingChapter(), Is.True);
            rules.RegisterRelayStabilized();
            rules.RegisterRelayStabilized();
            rules.RegisterRelayStabilized();
            Assert.That(rules.Chapter, Is.EqualTo(SinglePlayerDemoChapter.RelaySurge));

            Assert.That(rules.CompleteRelayDefense(), Is.True);
            Assert.That(rules.Chapter, Is.EqualTo(SinglePlayerDemoChapter.StormCalibration));
        }

        [Test]
        public void 第三章每波完成后必须写入一次校准()
        {
            var rules = EnterStormChapter();

            for (int wave = 0; wave < 3; wave++)
            {
                Assert.That(rules.CurrentStormWave, Is.EqualTo(wave));
                Assert.That(rules.ConfirmCalibration(), Is.False);
                Assert.That(rules.CompleteCurrentStormWave(), Is.True);
                Assert.That(rules.IsAwaitingCalibration, Is.True);
                Assert.That(rules.ConfirmCalibration(), Is.True);
            }

            Assert.That(rules.Chapter, Is.EqualTo(SinglePlayerDemoChapter.Completed));
        }

        [Test]
        public void 重启第三章会回到第一波并清除等待校准状态()
        {
            var rules = EnterStormChapter();
            rules.CompleteCurrentStormWave();
            rules.ConfirmCalibration();
            rules.CompleteCurrentStormWave();

            rules.RestartCurrentChapter();

            Assert.That(rules.CurrentStormWave, Is.Zero);
            Assert.That(rules.IsAwaitingCalibration, Is.False);
            Assert.That(rules.Chapter, Is.EqualTo(SinglePlayerDemoChapter.StormCalibration));
        }

        private static SinglePlayerDemoRules EnterStormChapter()
        {
            var rules = new SinglePlayerDemoRules(3, 3);
            rules.CompleteCoolingChapter();
            rules.CompleteRelayDefense();
            rules.RegisterRelayStabilized();
            rules.RegisterRelayStabilized();
            rules.RegisterRelayStabilized();
            return rules;
        }
    }
}

using FunGame.Demo;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class SinglePlayerDemoRulesTests
    {
        [Test]
        public void 第一章完成两轮冷却支路后才进入第二章()
        {
            var rules = new SinglePlayerDemoRules(2, 5, 5);

            Assert.That(rules.CompleteCoolingChapter(), Is.True);
            Assert.That(rules.CoolingRunsCompleted, Is.EqualTo(1));
            Assert.That(rules.Chapter, Is.EqualTo(SinglePlayerDemoChapter.CoolingEmergency));

            Assert.That(rules.CompleteCoolingChapter(), Is.True);
            Assert.That(rules.CoolingRunsCompleted, Is.EqualTo(2));
            Assert.That(rules.Chapter, Is.EqualTo(SinglePlayerDemoChapter.RelaySurge));
            Assert.That(rules.CompleteCoolingChapter(), Is.False);
        }

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
            var rules = EnterStormChapter(5, 5);

            for (int wave = 0; wave < 5; wave++)
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

        private static SinglePlayerDemoRules EnterStormChapter(int relayCount = 3, int waveCount = 3)
        {
            var rules = new SinglePlayerDemoRules(relayCount, waveCount);
            rules.CompleteCoolingChapter();
            rules.CompleteRelayDefense();
            for (int index = 0; index < relayCount; index++)
            {
                rules.RegisterRelayStabilized();
            }

            return rules;
        }
    }
}

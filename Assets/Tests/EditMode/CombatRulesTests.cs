using FunGame.Combat;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class CombatRulesTests
    {
        [Test]
        public void InterferenceEnemy_三次扳手命中后被击败()
        {
            var rules = new InterferenceEnemyRules(3, 1f);

            Assert.That(rules.ReceiveHit(1), Is.False);
            Assert.That(rules.Health, Is.EqualTo(2));
            Assert.That(rules.ReceiveHit(1), Is.False);
            Assert.That(rules.ReceiveHit(1), Is.True);
            Assert.That(rules.Health, Is.Zero);
        }

        [Test]
        public void InterferenceEnemy_只在目标范围内按间隔攻击()
        {
            var rules = new InterferenceEnemyRules(3, 1f);

            Assert.That(rules.Advance(0f, false), Is.False);
            Assert.That(rules.Advance(0f, true), Is.True);
            Assert.That(rules.Advance(0.5f, true), Is.False);
            Assert.That(rules.Advance(0.5f, true), Is.True);
        }

        [Test]
        public void InterferenceEnemy_重置后恢复生命和即时攻击资格()
        {
            var rules = new InterferenceEnemyRules(2, 1f);
            rules.ReceiveHit(2);

            rules.Reset();

            Assert.That(rules.Health, Is.EqualTo(2));
            Assert.That(rules.IsDefeated, Is.False);
            Assert.That(rules.Advance(0f, true), Is.True);
        }

        [Test]
        public void DefendableSystem_完整度耗尽后离线且可重置()
        {
            var rules = new DefendableSystemRules(20);

            Assert.That(rules.ApplyInterference(10), Is.False);
            Assert.That(rules.ApplyInterference(10), Is.True);
            Assert.That(rules.IsOffline, Is.True);

            rules.Reset();

            Assert.That(rules.Integrity, Is.EqualTo(20));
            Assert.That(rules.IsOffline, Is.False);
        }
    }
}

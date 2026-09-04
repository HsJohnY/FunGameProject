using FunGame.Interaction;
using FunGame.Tools;
using NUnit.Framework;
using UnityEngine;

namespace FunGame.Tests.EditMode
{
    public sealed class ToolAndThrowRuleTests
    {
        [Test]
        public void ToolActionOption_错误工具明确阻止动作()
        {
            var option = new ToolActionOption(
                "leak",
                "泄漏点",
                "密封",
                ToolKind.SealantGun,
                ToolKind.ImpactWrench);

            Assert.That(option.IsAvailable, Is.False);
            Assert.That(option.BlockedReason, Is.EqualTo("需要密封喷枪"));
        }

        [Test]
        public void ToolActionOption_正确工具允许具体动作()
        {
            var option = new ToolActionOption(
                "fastener",
                "机械连接",
                "松开",
                ToolKind.ImpactWrench,
                ToolKind.ImpactWrench);

            Assert.That(option.IsAvailable, Is.True);
            Assert.That(option.ActionLabel, Is.EqualTo("松开"));
        }

        [Test]
        public void 第三工具具有独立名称且错误工具反馈明确()
        {
            var option = new ToolActionOption(
                "circuit",
                "控制联锁",
                "桥接",
                ToolKind.CircuitBridger,
                ToolKind.SealantGun);

            Assert.That(ToolKind.CircuitBridger.GetDisplayName(), Is.EqualTo("线路桥接器"));
            Assert.That(option.IsAvailable, Is.False);
            Assert.That(option.BlockedReason, Is.EqualTo("需要线路桥接器"));
        }

        [Test]
        public void CalculateImpulse_向上观察产生更大的垂直冲量()
        {
            Vector3 horizontal = CarryThrowMath.CalculateImpulse(Vector3.forward, 4.5f, 1f);
            Vector3 upward = CarryThrowMath.CalculateImpulse(new Vector3(0f, 1f, 1f), 4.5f, 1f);

            Assert.That(upward.y, Is.GreaterThan(horizontal.y));
            Assert.That(horizontal.z, Is.GreaterThan(0f));
        }
    }
}

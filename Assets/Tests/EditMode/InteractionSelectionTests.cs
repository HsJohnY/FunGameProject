using FunGame.Interaction;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class InteractionSelectionTests
    {
        [Test]
        public void IsBetter_设备动作优先于普通拾取()
        {
            var pickup = new InteractionOption("item", "物品", "拾取", InteractionPriority.Pickup);
            var device = new InteractionOption("console", "控制台", "启动", InteractionPriority.Device);

            Assert.That(InteractionSelection.IsBetter(device, pickup), Is.True);
        }

        [Test]
        public void IsBetter_条件不足不会让高优先级动作降级()
        {
            var pickup = new InteractionOption("item", "物品", "拾取", InteractionPriority.Pickup);
            var blockedPlacement = new InteractionOption(
                "socket",
                "安装位",
                "安装",
                InteractionPriority.TaskItemPlacement,
                false,
                "零件不匹配");

            Assert.That(InteractionSelection.IsBetter(blockedPlacement, pickup), Is.True);
        }

        [Test]
        public void IsBetter_同优先级按TargetId稳定排序()
        {
            var current = new InteractionOption("target-b", "B", "操作", InteractionPriority.Device);
            var candidate = new InteractionOption("target-a", "A", "操作", InteractionPriority.Device);

            Assert.That(InteractionSelection.IsBetter(candidate, current), Is.True);
        }
    }
}

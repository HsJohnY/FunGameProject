using FunGame.Networking;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class NetworkCommunicationRulesTests
    {
        [TestCase(0ul, NetworkQualityLevel.Good)]
        [TestCase(100ul, NetworkQualityLevel.Good)]
        [TestCase(101ul, NetworkQualityLevel.Playable)]
        [TestCase(200ul, NetworkQualityLevel.Playable)]
        [TestCase(201ul, NetworkQualityLevel.Degraded)]
        public void Evaluate_按往返延迟划分诊断等级(ulong rtt, NetworkQualityLevel expected)
        {
            Assert.That(NetworkQualityRules.Evaluate(rtt), Is.EqualTo(expected));
        }

        [Test]
        public void NormalizeMessage_移除换行限制长度并拒绝空白()
        {
            Assert.That(NetworkChatController.NormalizeMessage(" 你好\n维修员 "), Is.EqualTo("你好 维修员"));
            Assert.That(NetworkChatController.NormalizeMessage("   "), Is.Empty);
            Assert.That(NetworkChatController.NormalizeMessage(new string('a', 100)).Length, Is.EqualTo(80));
        }
    }
}

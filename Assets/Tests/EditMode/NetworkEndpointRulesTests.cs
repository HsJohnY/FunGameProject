using FunGame.Networking;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class NetworkEndpointRulesTests
    {
        [TestCase("127.0.0.1", "7777", "127.0.0.1", 7777)]
        [TestCase(" 192.168.1.8 ", "24567", "192.168.1.8", 24567)]
        public void TryNormalize_有效地址返回标准连接参数(string inputAddress, string inputPort, string address, int port)
        {
            bool valid = NetworkEndpointRules.TryNormalize(inputAddress, inputPort, out string actualAddress, out ushort actualPort, out string error);

            Assert.That(valid, Is.True, error);
            Assert.That(actualAddress, Is.EqualTo(address));
            Assert.That((int)actualPort, Is.EqualTo(port));
        }

        [TestCase("", "7777")]
        [TestCase("localhost", "7777")]
        [TestCase("127.0.0.1", "0")]
        [TestCase("127.0.0.1", "70000")]
        public void TryNormalize_无效输入提供可读错误(string address, string port)
        {
            bool valid = NetworkEndpointRules.TryNormalize(address, port, out _, out _, out string error);

            Assert.That(valid, Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [TestCase("7777", 7777)]
        [TestCase(" 24567 ", 24567)]
        public void TryNormalizePort_创建房间只校验端口(string input, int expected)
        {
            bool valid = NetworkEndpointRules.TryNormalizePort(input, out ushort port, out string error);

            Assert.That(valid, Is.True, error);
            Assert.That((int)port, Is.EqualTo(expected));
        }
    }
}

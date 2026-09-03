using FunGame.Networking;
using FunGame.Tools;
using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class NetworkPlayerToolbeltTests
    {
        [TestCase(ToolKind.ImpactWrench, true)]
        [TestCase(ToolKind.SealantGun, true)]
        [TestCase(ToolKind.None, false)]
        public void IsSupportedTool_只允许MVP核心工具(ToolKind tool, bool expected)
        {
            Assert.That(NetworkPlayerToolbelt.IsSupportedTool(tool), Is.EqualTo(expected));
        }
    }
}

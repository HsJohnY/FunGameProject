using NUnit.Framework;

namespace FunGame.Tests.EditMode
{
    public sealed class ProjectBootstrapMarkerTests
    {
        [Test]
        public void BaselineId_IsStableAndNonEmpty()
        {
            Assert.That(ProjectBootstrapMarker.BaselineId, Is.EqualTo("m0-technical-baseline"));
        }

        [TestCase(new[] { "FunGame-M0.exe", "--m0-smoke" }, true)]
        [TestCase(new[] { "FunGame-M0.exe" }, false)]
        public void ContainsSmokeRunFlag_DetectsOnlyExplicitFlag(string[] arguments, bool expected)
        {
            Assert.That(ProjectBootstrapMarker.ContainsSmokeRunFlag(arguments), Is.EqualTo(expected));
        }
    }
}

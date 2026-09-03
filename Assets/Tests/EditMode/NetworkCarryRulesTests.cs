using FunGame.Networking;
using NUnit.Framework;
using UnityEngine;

namespace FunGame.Tests.EditMode
{
    public sealed class NetworkCarryRulesTests
    {
        [Test]
        public void IsValidThrowDirection_拒绝零向量和非有限数值()
        {
            Assert.That(NetworkPlayerCarryController.IsValidThrowDirection(Vector3.zero), Is.False);
            Assert.That(NetworkPlayerCarryController.IsValidThrowDirection(new Vector3(float.NaN, 0f, 1f)), Is.False);
            Assert.That(NetworkPlayerCarryController.IsValidThrowDirection(Vector3.forward), Is.True);
        }
    }
}

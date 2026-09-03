using FunGame.Networking;
using NUnit.Framework;
using UnityEngine;

namespace FunGame.Tests.EditMode
{
    public sealed class NetworkPlayerSpawnLayoutTests
    {
        [Test]
        public void GetSpawnPosition_不同客户端不会重叠()
        {
            Vector3 hostPosition = NetworkPlayerSpawnLayout.GetSpawnPosition(0);
            Vector3 clientPosition = NetworkPlayerSpawnLayout.GetSpawnPosition(1);

            Assert.That(hostPosition, Is.Not.EqualTo(clientPosition));
            Assert.That(Vector3.Distance(hostPosition, clientPosition), Is.GreaterThanOrEqualTo(2f));
            Assert.That(hostPosition.y, Is.EqualTo(1f));
            Assert.That(clientPosition.y, Is.EqualTo(1f));
        }
    }
}

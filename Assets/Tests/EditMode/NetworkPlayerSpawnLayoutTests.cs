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
            Assert.That(hostPosition.y, Is.EqualTo(1.1f));
            Assert.That(clientPosition.y, Is.EqualTo(1.1f));
        }

        [Test]
        public void GetSpawnPosition_前四名玩家均位于测试场内且互不重叠()
        {
            var positions = new Vector3[4];
            for (ulong clientId = 0; clientId < 4; clientId++)
            {
                Vector3 position = NetworkPlayerSpawnLayout.GetSpawnPosition(clientId);
                Assert.That(Mathf.Abs(position.x), Is.LessThan(14f));
                Assert.That(Mathf.Abs(position.z), Is.LessThan(14f));
                positions[clientId] = position;
            }

            Assert.That(positions, Is.Unique);
        }
    }
}

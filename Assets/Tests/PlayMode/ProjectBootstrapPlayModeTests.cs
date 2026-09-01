using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class ProjectBootstrapPlayModeTests
    {
        [UnityTest]
        public IEnumerator Marker_CanParticipateInUnityLifecycle()
        {
            var gameObject = new GameObject("Test Bootstrap Marker");
            var marker = gameObject.AddComponent<ProjectBootstrapMarker>();

            yield return null;

            Assert.That(marker.isActiveAndEnabled, Is.True);
            Object.Destroy(gameObject);
        }
    }
}

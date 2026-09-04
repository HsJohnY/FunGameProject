using System.Collections;
using System.Linq;
using FunGame.Demo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Tests.PlayMode
{
    internal static class ModularSceneTestUtility
    {
        public static IEnumerator WaitUntilReady(Scene scene)
        {
            var mode = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SharedMapModeController>(true)).Single();
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!mode.IsReady && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(mode.IsReady, Is.True, "Gameplay must wait for all environment modules.");
        }
    }
}

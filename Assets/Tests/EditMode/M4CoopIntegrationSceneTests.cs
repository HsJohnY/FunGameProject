using System.Linq;
using FunGame.Combat;
using FunGame.Demo;
using FunGame.Networking;
using FunGame.Player;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Tests.EditMode
{
    public sealed class M4CoopIntegrationSceneTests
    {
        private const string ScenePath = "Assets/Game/Scenes/M4_CoopThreeChapterDemo.unity";

        [Test]
        public void M4场景包含三舱网络会话且不保留本地玩家()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                NetworkManager manager = roots.SelectMany(item => item.GetComponentsInChildren<NetworkManager>(true))
                    .SingleOrDefault();
                NetworkSessionController session = roots
                    .SelectMany(item => item.GetComponentsInChildren<NetworkSessionController>(true)).SingleOrDefault();
                NetworkChatController chat = roots
                    .SelectMany(item => item.GetComponentsInChildren<NetworkChatController>(true)).SingleOrDefault();

                Assert.That(manager, Is.Not.Null);
                Assert.That(session, Is.Not.Null);
                Assert.That(chat, Is.Not.Null);
                Assert.That(manager.NetworkConfig.PlayerPrefab, Is.Not.Null);
                Assert.That(manager.NetworkConfig.PlayerPrefab.GetComponent<NetworkPlayerController>(), Is.Not.Null);
                Assert.That(roots.SelectMany(item => item.GetComponentsInChildren<FirstPersonController>(true)), Is.Empty,
                    "场景不能同时保留本地玩家和由 NetworkManager 生成的联网玩家。 ");
                Assert.That(FindTransform(roots, "Chapter 2 - Power Relay Compartment"), Is.Not.Null);
                Assert.That(FindTransform(roots, "Chapter 3 - Storm Core Chamber"), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void M4场景在同步实现前冻结本地事故与战斗状态()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                MonoBehaviour[] behaviours = scene.GetRootGameObjects()
                    .SelectMany(item => item.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();

                Assert.That(behaviours.OfType<SinglePlayerDemoController>().Single().enabled, Is.False);
                Assert.That(behaviours.OfType<CombatEncounterController>().All(item => !item.enabled), Is.True);
                Assert.That(behaviours.OfType<InterferenceEnemy>().All(item => !item.enabled), Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Transform FindTransform(GameObject[] roots, string objectName)
        {
            return roots.SelectMany(item => item.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == objectName);
        }
    }
}

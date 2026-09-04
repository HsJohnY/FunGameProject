using System.Collections;
using FunGame.Networking;
using FunGame.UI;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class M4CommunicationSpawnPlayModeTests
    {
        [UnityTest]
        public IEnumerator 启动M4主机后从注册预制体生成唯一聊天对象()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("M4_CoopThreeChapterDemo", LoadSceneMode.Additive);
            while (load != null && !load.isDone)
            {
                yield return null;
            }

            Scene scene = SceneManager.GetSceneByName("M4_CoopThreeChapterDemo");
            Assert.That(scene.IsValid(), Is.True);
            GameMenuController menu = Object.FindFirstObjectByType<GameMenuController>();
            menu?.EnterGameplayForAutomation();
            NetworkSessionController session = Object.FindFirstObjectByType<NetworkSessionController>();
            NetworkManager manager = Object.FindFirstObjectByType<NetworkManager>();
            Assert.That(session, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(session.TrySetEndpointInput("127.0.0.1", "17841"), Is.True);
            Assert.That(session.StartHost(), Is.True, session.StatusText);

            float deadline = Time.realtimeSinceStartup + 3f;
            NetworkChatController chat = null;
            while (chat == null && Time.realtimeSinceStartup < deadline)
            {
                chat = Object.FindFirstObjectByType<NetworkChatController>();
                yield return null;
            }

            Assert.That(chat, Is.Not.Null, "主机启动后应生成已注册的唯一通信预制体。 ");
            Assert.That(chat.IsSpawned, Is.True);
            Assert.That(Object.FindObjectsByType<NetworkChatController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));

            manager.Shutdown();
            yield return null;
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}

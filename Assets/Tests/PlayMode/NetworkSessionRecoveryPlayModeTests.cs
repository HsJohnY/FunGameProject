using System.Collections;
using System.Net;
using System.Net.Sockets;
using FunGame.Networking;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class NetworkSessionRecoveryPlayModeTests
    {
        [UnityTest]
        public IEnumerator StartHost_端口被占用后恢复输入状态()
        {
            // 先独占一个系统分配的 UDP 端口，再让 Unity Transport 尝试监听同一端口，
            // 从而稳定复现玩家选择了未知占用端口的情况。
            using var occupiedPort = new UdpClient(AddressFamily.InterNetwork);
            occupiedPort.ExclusiveAddressUse = true;
            occupiedPort.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            int port = ((IPEndPoint)occupiedPort.Client.LocalEndPoint).Port;

            var sessionObject = new GameObject("Occupied Port Session Test");
            sessionObject.SetActive(false);
            var manager = sessionObject.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig();
            var transport = sessionObject.AddComponent<UnityTransport>();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.EnableSceneManagement = false;
            var controller = sessionObject.AddComponent<NetworkSessionController>();
            controller.Configure(manager, transport);
            sessionObject.SetActive(true);
            Assert.That(controller.TrySetEndpointInput("127.0.0.1", port.ToString()), Is.True);

            bool started = controller.StartHost();

            Assert.That(started, Is.False);
            Assert.That(controller.IsEndpointEditable, Is.True, controller.StatusText);
            Assert.That(controller.StatusText, Does.Contain("端口"));
            Assert.That(manager.IsListening, Is.False);

            Object.Destroy(sessionObject);
            yield return null;
        }
    }
}

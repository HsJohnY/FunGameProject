using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// 由主机生成唯一会话通信对象。聊天对象使用已注册预制体，
    /// 不依赖关闭了 NGO 场景管理的场景内 NetworkObject。
    /// </summary>
    public sealed class NetworkCommunicationSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private GameObject communicationPrefab;
        private NetworkObject _spawnedCommunication;

        public GameObject CommunicationPrefab => communicationPrefab;

        public void Configure(NetworkManager manager, GameObject prefab)
        {
            Unsubscribe();
            networkManager = manager;
            communicationPrefab = prefab;
            Subscribe();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (networkManager == null)
            {
                return;
            }

            networkManager.OnServerStarted -= HandleServerStarted;
            networkManager.OnServerStopped -= HandleServerStopped;
            networkManager.OnServerStarted += HandleServerStarted;
            networkManager.OnServerStopped += HandleServerStopped;
        }

        private void Unsubscribe()
        {
            if (networkManager != null)
            {
                networkManager.OnServerStarted -= HandleServerStarted;
                networkManager.OnServerStopped -= HandleServerStopped;
            }
        }

        private void HandleServerStarted()
        {
            if (!networkManager.IsServer || communicationPrefab == null || _spawnedCommunication != null)
            {
                return;
            }

            GameObject instance = Instantiate(communicationPrefab);
            _spawnedCommunication = instance.GetComponent<NetworkObject>();
            _spawnedCommunication.Spawn(true);
        }

        private void HandleServerStopped(bool wasClient)
        {
            _spawnedCommunication = null;
        }
    }
}

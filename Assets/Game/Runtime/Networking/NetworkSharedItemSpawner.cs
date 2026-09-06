using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// 主机启动后生成唯一共享任务物；客户端只接收服务器生成结果。
    /// </summary>
    public sealed class NetworkSharedItemSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 1f, -1f);

        private NetworkObject _spawnedItem;

        public void Configure(NetworkManager manager, GameObject prefab, Vector3? configuredSpawnPosition = null)
        {
            Unsubscribe();
            networkManager = manager;
            itemPrefab = prefab;
            if (configuredSpawnPosition.HasValue)
            {
                spawnPosition = configuredSpawnPosition.Value;
            }
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
            if (!networkManager.IsServer || itemPrefab == null || _spawnedItem != null)
            {
                return;
            }

            GameObject instance = Instantiate(itemPrefab, spawnPosition, itemPrefab.transform.rotation);
            _spawnedItem = instance.GetComponent<NetworkObject>();
            _spawnedItem.Spawn(true);
        }

        private void HandleServerStopped(bool wasClient)
        {
            _spawnedItem = null;
        }
    }
}

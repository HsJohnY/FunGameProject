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

        public void Configure(NetworkManager manager, GameObject prefab)
        {
            Unsubscribe();
            networkManager = manager;
            itemPrefab = prefab;
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
            networkManager.OnServerStarted += HandleServerStarted;
        }

        private void Unsubscribe()
        {
            if (networkManager != null)
            {
                networkManager.OnServerStarted -= HandleServerStarted;
            }
        }

        private void HandleServerStarted()
        {
            if (!networkManager.IsServer || itemPrefab == null || _spawnedItem != null)
            {
                return;
            }

            GameObject instance = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);
            _spawnedItem = instance.GetComponent<NetworkObject>();
            _spawnedItem.Spawn(true);
        }
    }
}

using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// 由主机生成唯一事故状态对象，使客户端通过已注册预制体获得同一状态源。
    /// </summary>
    public sealed class NetworkIncidentSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private GameObject incidentPrefab;
        private NetworkObject _spawnedIncident;

        public void Configure(NetworkManager manager, GameObject prefab)
        {
            Unsubscribe();
            networkManager = manager;
            incidentPrefab = prefab;
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
            if (!networkManager.IsServer || incidentPrefab == null || _spawnedIncident != null)
            {
                return;
            }

            GameObject instance = Instantiate(incidentPrefab, Vector3.zero, Quaternion.identity);
            _spawnedIncident = instance.GetComponent<NetworkObject>();
            _spawnedIncident.Spawn(true);
        }

        private void HandleServerStopped(bool wasClient)
        {
            _spawnedIncident = null;
        }
    }
}

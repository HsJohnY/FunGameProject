using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>仅由主机生成唯一战役控制器。</summary>
    public sealed class NetworkCampaignSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private GameObject campaignPrefab;

        public GameObject CampaignPrefab => campaignPrefab;

        public void Configure(NetworkManager manager, GameObject prefab)
        {
            networkManager = manager;
            campaignPrefab = prefab;
        }

        private void Awake()
        {
            networkManager ??= GetComponent<NetworkManager>();
        }

        private void OnEnable()
        {
            if (networkManager != null) networkManager.OnServerStarted += Spawn;
        }

        private void OnDisable()
        {
            if (networkManager != null) networkManager.OnServerStarted -= Spawn;
        }

        private void Spawn()
        {
            if (!networkManager.IsServer || campaignPrefab == null ||
                FindFirstObjectByType<NetworkCampaignController>() != null) return;
            Instantiate(campaignPrefab).GetComponent<NetworkObject>().Spawn();
        }
    }
}

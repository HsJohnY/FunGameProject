using FunGame.Incident;
using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>串联联网维修、继电器协作和核心防卫的主机权威战役。</summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkCampaignController : NetworkBehaviour
    {
        private const int RelayCount = 5;
        private const int RelaySteps = 3;
        private const int StormWaves = 3;
        [SerializeField] private GameObject enemyPrefab;
        private readonly NetworkVariable<NetworkCampaignChapter> chapter = new NetworkVariable<NetworkCampaignChapter>();
        private readonly NetworkVariable<int> relayTotalProgress = new NetworkVariable<int>();
        private readonly NetworkList<int> relayProgress = new NetworkList<int>();
        private readonly NetworkVariable<int> stormWave = new NetworkVariable<int>();
        private readonly NetworkVariable<int> enemiesRemaining = new NetworkVariable<int>();
        private readonly NetworkVariable<int> coreIntegrity = new NetworkVariable<int>(100);
        private readonly NetworkVariable<bool> awaitingCalibration = new NetworkVariable<bool>();
        private NetworkCoolingIncidentController _incident;
        private bool _chapterSpawned;

        public NetworkCampaignChapter Chapter => chapter.Value;
        public int RelayTotalProgress => relayTotalProgress.Value;
        public int CurrentStormWave => stormWave.Value;
        public int EnemiesRemaining => enemiesRemaining.Value;
        public int CoreIntegrity => coreIntegrity.Value;
        public bool CanConfirmStormWave => chapter.Value == NetworkCampaignChapter.StormDefense && awaitingCalibration.Value;
        public string CurrentObjective => chapter.Value switch
        {
            NetworkCampaignChapter.CoolingRepair => "第一章：协作恢复冷却系统",
            NetworkCampaignChapter.RelaySurge => $"第二章：继电器 {relayTotalProgress.Value}/{RelayCount * RelaySteps} · 敌人 {enemiesRemaining.Value}",
            NetworkCampaignChapter.StormDefense => awaitingCalibration.Value
                ? $"第三章：第 {stormWave.Value + 1}/{StormWaves} 波已清除，前往校准终端"
                : $"第三章：防卫第 {stormWave.Value + 1}/{StormWaves} 波 · 敌人 {enemiesRemaining.Value} · 核心 {coreIntegrity.Value}%",
            _ => "远征切片完成：冷却、配电与风暴核心全部在线"
        };

        public void Configure(GameObject prefab) => enemyPrefab = prefab;

        public override void OnNetworkSpawn()
        {
            chapter.OnValueChanged += (_, _) => RefreshDoors();
            if (IsServer)
            {
                chapter.Value = NetworkCampaignChapter.CoolingRepair;
                coreIntegrity.Value = 100;
                relayProgress.Clear();
                for (int index = 0; index < RelayCount; index++) relayProgress.Add(0);
            }
            RefreshDoors();
        }

        private void Update()
        {
            if (!IsServer) return;
            _incident ??= FindFirstObjectByType<NetworkCoolingIncidentController>();
            if (chapter.Value == NetworkCampaignChapter.CoolingRepair && _incident != null &&
                _incident.RunState == CoolingIncidentRunState.Succeeded)
            {
                chapter.Value = NetworkCampaignChapter.RelaySurge;
                SpawnEncounter(false, 7);
            }
        }

        public bool CanOperateRelay(int index) => chapter.Value == NetworkCampaignChapter.RelaySurge &&
                                                   index >= 0 && index < relayProgress.Count && relayProgress[index] < RelaySteps;

        public void TryOperateRelayServer(int index)
        {
            if (!IsServer || !CanOperateRelay(index)) return;
            relayProgress[index]++;
            relayTotalProgress.Value++;
            TryFinishRelayChapter();
        }

        public void NotifyEnemyDefeatedServer()
        {
            if (!IsServer) return;
            enemiesRemaining.Value = Mathf.Max(0, enemiesRemaining.Value - 1);
            if (chapter.Value == NetworkCampaignChapter.RelaySurge) TryFinishRelayChapter();
            else if (chapter.Value == NetworkCampaignChapter.StormDefense && enemiesRemaining.Value == 0)
                awaitingCalibration.Value = true;
        }

        public void ApplyCoreDamageServer(int amount)
        {
            if (!IsServer || chapter.Value == NetworkCampaignChapter.Completed) return;
            coreIntegrity.Value = Mathf.Max(0, coreIntegrity.Value - Mathf.Max(0, amount));
            if (coreIntegrity.Value == 0) ResetCurrentCombatServer();
        }

        public void ConfirmStormWaveServer()
        {
            if (!IsServer || !CanConfirmStormWave) return;
            awaitingCalibration.Value = false;
            if (stormWave.Value + 1 >= StormWaves)
            {
                chapter.Value = NetworkCampaignChapter.Completed;
                return;
            }
            stormWave.Value++;
            coreIntegrity.Value = 100;
            SpawnEncounter(true, 7 + stormWave.Value * 2);
        }

        private void TryFinishRelayChapter()
        {
            if (relayTotalProgress.Value < RelayCount * RelaySteps || enemiesRemaining.Value > 0) return;
            chapter.Value = NetworkCampaignChapter.StormDefense;
            stormWave.Value = 0;
            coreIntegrity.Value = 100;
            SpawnEncounter(true, 7);
        }

        private void ResetCurrentCombatServer()
        {
            foreach (NetworkCombatEnemy enemy in FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None))
                if (enemy.IsSpawned) enemy.NetworkObject.Despawn(true);
            coreIntegrity.Value = 100;
            SpawnEncounter(chapter.Value == NetworkCampaignChapter.StormDefense,
                chapter.Value == NetworkCampaignChapter.StormDefense ? 7 + stormWave.Value * 2 : 7);
        }

        private void SpawnEncounter(bool storm, int count)
        {
            if (enemyPrefab == null) return;
            Vector3 center = storm ? new Vector3(0f, 1f, 45f) : new Vector3(0f, 1f, 25f);
            Vector3 target = storm ? new Vector3(0f, 1f, 43f) : new Vector3(0f, 1f, 23f);
            enemiesRemaining.Value = count;
            for (int index = 0; index < count; index++)
            {
                // 前五只组成可被喷枪覆盖的虫群，另有侧袭体与护盾精英。
                Vector3 position = center + new Vector3((index % 3 - 1) * 0.85f, 0f, 3f + index / 3 * 1.1f);
                GameObject instance = Instantiate(enemyPrefab, position, Quaternion.identity);
                NetworkObject networkObject = instance.GetComponent<NetworkObject>();
                networkObject.Spawn();
                NetworkEnemyKind kind = index < 5 ? NetworkEnemyKind.Swarm : index == 5 ? NetworkEnemyKind.Flanker
                    : index == 6 ? NetworkEnemyKind.ShieldElite : NetworkEnemyKind.Ranged;
                instance.GetComponent<NetworkCombatEnemy>().InitializeServer(this, target,
                    kind == NetworkEnemyKind.Swarm ? 2 : kind == NetworkEnemyKind.ShieldElite ? 6 : 4,
                    kind == NetworkEnemyKind.Flanker ? 1.1f : 0.75f, kind == NetworkEnemyKind.ShieldElite, kind);
            }
        }

        private void RefreshDoors()
        {
            SetDoor("Sealed Power Compartment Door", chapter.Value == NetworkCampaignChapter.CoolingRepair);
            SetDoor("Sealed Storm Chamber Door", chapter.Value < NetworkCampaignChapter.StormDefense);
        }

        private static void SetDoor(string name, bool closed)
        {
            foreach (Transform item in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (item.name == name) item.gameObject.SetActive(closed);
        }
    }
}

using FunGame.Incident;
using FunGame.Combat;
using FunGame.Demo;
using System.Linq;
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
        [SerializeField] private GameObject enemyPrefab;
        private readonly NetworkVariable<NetworkCampaignChapter> chapter = new NetworkVariable<NetworkCampaignChapter>();
        private readonly NetworkVariable<int> relayTotalProgress = new NetworkVariable<int>();
        private readonly NetworkList<int> relayProgress = new NetworkList<int>();
        private readonly NetworkVariable<int> stormWave = new NetworkVariable<int>();
        private readonly NetworkVariable<int> enemiesRemaining = new NetworkVariable<int>();
        private readonly NetworkVariable<int> coreIntegrity = new NetworkVariable<int>(100);
        private readonly NetworkVariable<bool> awaitingCalibration = new NetworkVariable<bool>();
        private readonly NetworkVariable<int> coolingRunsCompleted = new NetworkVariable<int>();
        private readonly NetworkVariable<bool> chapterFailed = new NetworkVariable<bool>();
        private NetworkCoolingIncidentController _incident;
        private bool _chapterSpawned;
        private SinglePlayerDemoController _map;
        private CoolingCombatIntegrationController _coolingCombat;

        public NetworkCampaignChapter Chapter => chapter.Value;
        public int RelayTotalProgress => relayTotalProgress.Value;
        public int CurrentStormWave => stormWave.Value;
        public int EnemiesRemaining => enemiesRemaining.Value;
        public int CoreIntegrity => coreIntegrity.Value;
        public int StormWaveCount => _map != null ? _map.StormEncounters.Count : 5;
        public int CoolingRunsCompleted => coolingRunsCompleted.Value;
        public int RequiredCoolingRuns => _map != null ? _map.RequiredCoolingRunCount : 2;
        public bool IsCurrentChapterFailed => chapterFailed.Value;
        public int PendingEnemyCount => FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None).Count(e => e.Health > 0 && !e.IsDeployed);
        public float NextDeploymentSeconds => FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None)
            .Where(e => e.Health > 0 && !e.IsDeployed).Select(e => e.DeploymentRemaining).DefaultIfEmpty(0f).Min();
        public string WaveBriefing => _map != null && CurrentStormWave < _map.StormEncounters.Count ? _map.StormEncounters[CurrentStormWave].Briefing : string.Empty;
        public bool CanConfirmStormWave => !chapterFailed.Value && chapter.Value == NetworkCampaignChapter.StormDefense && awaitingCalibration.Value;
        public string CurrentObjective => chapterFailed.Value ? "设备离线：前往本舱恢复终端重启当前章节" : chapter.Value switch
        {
            NetworkCampaignChapter.CoolingRepair => $"第一章：稳定冷却支路 {CoolingRunsCompleted + 1}/{RequiredCoolingRuns}",
            NetworkCampaignChapter.RelaySurge => $"第二章：继电器 {relayTotalProgress.Value}/{RelayCount * RelaySteps} · 敌人 {enemiesRemaining.Value}",
            NetworkCampaignChapter.StormDefense => awaitingCalibration.Value
                ? $"第三章：第 {stormWave.Value + 1}/{StormWaveCount} 波已清除，前往校准终端"
                : $"第 {stormWave.Value + 1}/{StormWaveCount} 波 · {WaveBriefing} · 剩余 {enemiesRemaining.Value}" +
                    (PendingEnemyCount > 0 ? $" · 增援 {NextDeploymentSeconds:0}秒" : string.Empty),
            _ => "远征切片完成：冷却、配电与风暴核心全部在线"
        };

        public void Configure(GameObject prefab) => enemyPrefab = prefab;

        public override void OnNetworkSpawn()
        {
            _map = FindFirstObjectByType<SinglePlayerDemoController>(FindObjectsInactive.Include);
            _coolingCombat = FindFirstObjectByType<CoolingCombatIntegrationController>(FindObjectsInactive.Include);
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
            if (chapter.Value != NetworkCampaignChapter.CoolingRepair || _incident == null) return;
            if (_incident.RunState == CoolingIncidentRunState.Active && _incident.Phase >= CoolingIncidentPhase.LoosenConnection && !_chapterSpawned)
            {
                _chapterSpawned = true;
                SpawnEncounter(_coolingCombat.Encounter);
            }
            if (_incident.RunState == CoolingIncidentRunState.Failed || _incident.Phase == CoolingIncidentPhase.AssessSymptoms)
            {
                if (_chapterSpawned) ClearEnemies();
                _chapterSpawned = false;
            }
            if (_incident.RunState == CoolingIncidentRunState.Succeeded)
            {
                ClearEnemies();
                coolingRunsCompleted.Value++;
                _chapterSpawned = false;
                if (CoolingRunsCompleted < RequiredCoolingRuns) _incident.BeginNextBranchServer();
                else
                {
                    chapter.Value = NetworkCampaignChapter.RelaySurge;
                    SpawnEncounter(_map.RelayDefenseEncounter);
                }
            }
        }

        public bool CanOperateRelay(int index) => chapter.Value == NetworkCampaignChapter.RelaySurge &&
                                                   !chapterFailed.Value &&
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
            if (!IsServer || chapter.Value == NetworkCampaignChapter.Completed || chapterFailed.Value) return;
            coreIntegrity.Value = Mathf.Max(0, coreIntegrity.Value - Mathf.Max(0, amount));
            if (chapter.Value == NetworkCampaignChapter.CoolingRepair)
            {
                _incident?.ApplyTemperatureSpikeServer(coreIntegrity.Value == 0 ? 100f : 2.5f);
                return;
            }
            if (coreIntegrity.Value == 0)
            {
                chapterFailed.Value = true;
                ClearEnemies();
            }
        }

        public bool CanUseRecoveryConsole(int index) => index == (chapter.Value == NetworkCampaignChapter.RelaySurge ? 1 : 0) &&
            (chapter.Value == NetworkCampaignChapter.RelaySurge || chapter.Value == NetworkCampaignChapter.StormDefense) &&
            (chapterFailed.Value || CanConfirmStormWave);

        public void UseRecoveryConsoleServer(int index)
        {
            if (!IsServer || !CanUseRecoveryConsole(index)) return;
            if (!chapterFailed.Value) { ConfirmStormWaveServer(); return; }
            chapterFailed.Value = false;
            awaitingCalibration.Value = false;
            if (chapter.Value == NetworkCampaignChapter.RelaySurge)
            {
                relayTotalProgress.Value = 0;
                for (int i = 0; i < relayProgress.Count; i++) relayProgress[i] = 0;
            }
            else stormWave.Value = 0;
            ResetCurrentCombatServer();
        }

        public void ConfirmStormWaveServer()
        {
            if (!IsServer || !CanConfirmStormWave) return;
            awaitingCalibration.Value = false;
            if (stormWave.Value + 1 >= StormWaveCount)
            {
                chapter.Value = NetworkCampaignChapter.Completed;
                return;
            }
            stormWave.Value++;
            coreIntegrity.Value = 100;
            SpawnEncounter(_map.StormEncounters[stormWave.Value]);
        }

        private void TryFinishRelayChapter()
        {
            if (relayTotalProgress.Value < RelayCount * RelaySteps || enemiesRemaining.Value > 0) return;
            chapter.Value = NetworkCampaignChapter.StormDefense;
            stormWave.Value = 0;
            coreIntegrity.Value = 100;
            SpawnEncounter(_map.StormEncounters[0]);
        }

        private void ResetCurrentCombatServer()
        {
            ClearEnemies();
            SpawnEncounter(chapter.Value == NetworkCampaignChapter.StormDefense
                ? _map.StormEncounters[stormWave.Value] : _map.RelayDefenseEncounter);
        }

        private void ClearEnemies()
        {
            foreach (NetworkCombatEnemy enemy in FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None))
                if (enemy.IsSpawned) enemy.NetworkObject.Despawn(true);
            enemiesRemaining.Value = 0;
        }

        private void SpawnEncounter(CombatEncounterController encounter)
        {
            if (enemyPrefab == null || encounter == null) return;
            coreIntegrity.Value = encounter.DefenseTarget.MaxIntegrity;
            enemiesRemaining.Value = encounter.Enemies.Count;
            foreach (InterferenceEnemy source in encounter.Enemies)
            {
                GameObject instance = Instantiate(enemyPrefab, source.transform.position, source.transform.rotation);
                NetworkObject networkObject = instance.GetComponent<NetworkObject>();
                networkObject.Spawn();
                instance.GetComponent<NetworkCombatEnemy>().InitializeFromMapServer(this, source);
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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FunGame.Content;

namespace FunGame.Combat
{
    /// <summary>
    /// 协调一个被防卫设备和有限数量干扰体，并发布可供未来主机权威同步消费的状态事件。
    /// </summary>
    public sealed class CombatEncounterController : MonoBehaviour
    {
        [SerializeField] private DefendableSystemTarget defenseTarget;
        [SerializeField] private InterferenceEnemy enemy;
        [SerializeField] private List<InterferenceEnemy> additionalEnemies = new List<InterferenceEnemy>();
        [SerializeField] private string briefing;
        [SerializeField] private EncounterDefinition definition;
        public EncounterDefinition Definition => definition;
        public void ConfigureDefinition(EncounterDefinition value) => definition = value;
        public string Briefing => definition != null ? definition.Briefing : briefing;
        public int PendingEnemyCount => Enemies.Count(e => !e.IsDefeated && !e.IsDeployed);
        public float NextDeploymentSeconds => Enemies.Where(e => !e.IsDefeated && !e.IsDeployed).Select(e => e.DeploymentRemaining).DefaultIfEmpty(0f).Min();
        public void ConfigureBriefing(string value) => briefing = value;

        public event Action<CombatEncounterState> StateChanged;
        public CombatEncounterState State { get; private set; } = CombatEncounterState.Active;
        public int ResetCount { get; private set; }
        public DefendableSystemTarget DefenseTarget => defenseTarget;
        public InterferenceEnemy Enemy => enemy;
        public IReadOnlyList<InterferenceEnemy> Enemies => BuildEnemyList();
        public int RemainingEnemyCount
        {
            get
            {
                int count = 0;
                foreach (InterferenceEnemy candidate in BuildEnemyList())
                {
                    if (candidate != null && !candidate.IsDefeated)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public string CurrentInstruction
        {
            get
            {
                switch (State)
                {
                    case CombatEncounterState.Dormant:
                        return "继续处理冷却故障，留意设备周围的异常活动";
                    case CombatEncounterState.Succeeded:
                        return "干扰已清除：继续完成冷却设备维修";
                    case CombatEncounterState.Failed:
                        return "防卫失败：辅助控制设备已离线";
                    default:
                        return "小怪可用任意工具清除：扳手重击、喷枪范围清群、桥接器瘫痪破盾";
                }
            }
        }

        public void Configure(DefendableSystemTarget configuredTarget, InterferenceEnemy configuredEnemy)
        {
            Configure(configuredTarget, new[] { configuredEnemy }, true);
        }

        public void Configure(
            DefendableSystemTarget configuredTarget,
            IReadOnlyList<InterferenceEnemy> configuredEnemies,
            bool startActive = true)
        {
            defenseTarget = configuredTarget;
            enemy = configuredEnemies != null && configuredEnemies.Count > 0 ? configuredEnemies[0] : null;
            additionalEnemies.Clear();
            if (configuredEnemies != null)
            {
                for (int index = 1; index < configuredEnemies.Count; index++)
                {
                    if (configuredEnemies[index] != null)
                    {
                        additionalEnemies.Add(configuredEnemies[index]);
                    }
                }
            }

            SetState(startActive ? CombatEncounterState.Active : CombatEncounterState.Dormant);
            SetEnemiesActive(startActive);
        }

        public void BeginEncounter()
        {
            defenseTarget?.ResetSystem();
            foreach (InterferenceEnemy candidate in BuildEnemyList())
            {
                candidate?.ResetEnemy();
            }

            SetEnemiesActive(true);
            SetState(CombatEncounterState.Active);
            Debug.Log($"[Combat] encounter=defense action=begin enemies={BuildEnemyList().Count}", this);
        }

        public void PrepareDormant()
        {
            defenseTarget?.ResetSystem();
            foreach (InterferenceEnemy candidate in BuildEnemyList())
            {
                candidate?.ResetEnemy();
            }

            SetEnemiesActive(false);
            SetState(CombatEncounterState.Dormant);
        }

        public void NotifyEnemyDefeated(InterferenceEnemy defeatedEnemy = null)
        {
            if (State != CombatEncounterState.Active)
            {
                return;
            }

            defeatedEnemy?.SetEncounterActive(false);
            if (RemainingEnemyCount > 0)
            {
                return;
            }

            SetEnemiesActive(false);
            SetState(CombatEncounterState.Succeeded);
            Debug.Log("[Combat] encounter=defense result=succeeded", this);
        }

        public void NotifySystemOffline()
        {
            if (State != CombatEncounterState.Active)
            {
                return;
            }

            SetEnemiesActive(false);
            SetState(CombatEncounterState.Failed);
            Debug.Log("[Combat] encounter=defense result=failed reason=system-offline", this);
        }

        public bool ResetEncounter()
        {
            if (State == CombatEncounterState.Active || defenseTarget == null || BuildEnemyList().Count == 0)
            {
                return false;
            }

            BeginEncounter();
            ResetCount++;
            Debug.Log($"[Combat] encounter=defense action=reset count={ResetCount}", this);
            return true;
        }

        private List<InterferenceEnemy> BuildEnemyList()
        {
            var result = new List<InterferenceEnemy>(1 + additionalEnemies.Count);
            if (enemy != null)
            {
                result.Add(enemy);
            }

            foreach (InterferenceEnemy candidate in additionalEnemies)
            {
                if (candidate != null && !result.Contains(candidate))
                {
                    result.Add(candidate);
                }
            }

            return result;
        }

        private void SetEnemiesActive(bool active)
        {
            foreach (InterferenceEnemy candidate in BuildEnemyList())
            {
                candidate?.SetEncounterActive(active && !candidate.IsDefeated);
            }
        }

        private void SetState(CombatEncounterState nextState)
        {
            if (State == nextState)
            {
                return;
            }

            State = nextState;
            StateChanged?.Invoke(State);
        }
    }
}

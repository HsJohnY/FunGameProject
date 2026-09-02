using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 协调一个敌人和一个被防卫设备，提供成功、失败及可重复测试的重置边界。
    /// </summary>
    public sealed class CombatEncounterController : MonoBehaviour
    {
        [SerializeField] private DefendableSystemTarget defenseTarget;
        [SerializeField] private InterferenceEnemy enemy;

        public CombatEncounterState State { get; private set; } = CombatEncounterState.Active;
        public int ResetCount { get; private set; }
        public DefendableSystemTarget DefenseTarget => defenseTarget;
        public InterferenceEnemy Enemy => enemy;

        public string CurrentInstruction
        {
            get
            {
                switch (State)
                {
                    case CombatEncounterState.Succeeded:
                        return "防卫完成：设备恢复安全，可在训练控制台重新开始";
                    case CombatEncounterState.Failed:
                        return "防卫失败：设备已离线，可在训练控制台重新开始";
                    default:
                        return "保护冷却控制单元：取得冲击扳手并击退线路干扰体";
                }
            }
        }

        public void Configure(DefendableSystemTarget configuredTarget, InterferenceEnemy configuredEnemy)
        {
            defenseTarget = configuredTarget;
            enemy = configuredEnemy;
            State = CombatEncounterState.Active;
        }

        public void NotifyEnemyDefeated()
        {
            if (State != CombatEncounterState.Active)
            {
                return;
            }

            State = CombatEncounterState.Succeeded;
            enemy?.SetEncounterActive(false);
            Debug.Log("[Combat] encounter=defense result=succeeded", this);
        }

        public void NotifySystemOffline()
        {
            if (State != CombatEncounterState.Active)
            {
                return;
            }

            State = CombatEncounterState.Failed;
            enemy?.SetEncounterActive(false);
            Debug.Log("[Combat] encounter=defense result=failed reason=system-offline", this);
        }

        /// <summary>
        /// 在同一场景内恢复设备、敌人和遭遇状态，供人工连续回归使用。
        /// </summary>
        public bool ResetEncounter()
        {
            if (State == CombatEncounterState.Active || defenseTarget == null || enemy == null)
            {
                return false;
            }

            defenseTarget.ResetSystem();
            enemy.ResetEnemy();
            State = CombatEncounterState.Active;
            ResetCount++;
            Debug.Log($"[Combat] encounter=defense action=reset count={ResetCount}", this);
            return true;
        }
    }
}

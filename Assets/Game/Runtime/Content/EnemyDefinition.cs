using FunGame.Combat;
using UnityEngine;

namespace FunGame.Content
{
    /// <summary>只读敌人参数。当前生命、冷却及部署状态属于实体实例。</summary>
    [CreateAssetMenu(menuName = "FunGame/Content/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField, Min(1)] private int maxHealth = 3;
        public int MaxHealth => maxHealth;
        [SerializeField, Min(0.1f)] private float moveSpeed = 1.35f;
        public float MoveSpeed => moveSpeed;
        [SerializeField, Min(0.1f)] private float attackRange = 1.3f;
        public float AttackRange => attackRange;
        [SerializeField, Min(0.05f)] private float attackIntervalSeconds = 1.25f;
        public float AttackIntervalSeconds => attackIntervalSeconds;
        [SerializeField, Min(0f)] private float attackWindupSeconds = 0.45f;
        public float AttackWindupSeconds => attackWindupSeconds;
        [SerializeField, Min(1)] private int interferenceDamage = 10;
        public int InterferenceDamage => interferenceDamage;
        [SerializeField, Min(1)] private int wrenchDamage = 2;
        public int WrenchDamage => wrenchDamage;
        [SerializeField, Min(0f)] private float knockbackDistance = 1.25f;
        public float KnockbackDistance => knockbackDistance;
        [SerializeField, Range(0.1f, 1f)] private float sealantSpeedMultiplier = 0.35f;
        public float SealantSpeedMultiplier => sealantSpeedMultiplier;
        [SerializeField, Min(0.1f)] private float sealantSlowSeconds = 2.25f;
        public float SealantSlowSeconds => sealantSlowSeconds;
        [SerializeField, Min(0.05f)] private float sealantPulseIntervalSeconds = 0.15f;
        public float SealantPulseIntervalSeconds => sealantPulseIntervalSeconds;
        [SerializeField, Min(0f)] private float sealantPushDistance = 0.18f;
        public float SealantPushDistance => sealantPushDistance;
        [SerializeField, Min(1)] private int sealantDamage = 1;
        public int SealantDamage => sealantDamage;
        [SerializeField, Min(0.1f)] private float sealantSplashRadius = 1.65f;
        public float SealantSplashRadius => sealantSplashRadius;
        [SerializeField, Min(0.1f)] private float bridgerStunSeconds = 1.4f;
        public float BridgerStunSeconds => bridgerStunSeconds;
        [SerializeField, Min(0)] private int bridgerOverloadDamage = 1;
        public int BridgerOverloadDamage => bridgerOverloadDamage;
        [SerializeField, Min(0f)] private float defeatedVisualSeconds = 0.75f;
        public float DefeatedVisualSeconds => defeatedVisualSeconds;
        [SerializeField] private InterferenceEnemyBehavior behavior = InterferenceEnemyBehavior.Direct;
        public InterferenceEnemyBehavior Behavior => behavior;
        [SerializeField] private bool requiresCircuitDisruption = false;
        public bool RequiresCircuitDisruption => requiresCircuitDisruption;
    }
}

using System;

namespace FunGame.Combat
{
    /// <summary>
    /// 保存基础干扰敌人的生命和攻击节奏；不依赖 Unity 生命周期，便于规则测试和未来网络权威端复用。
    /// </summary>
    public sealed class InterferenceEnemyRules
    {
        private readonly int _maxHealth;
        private readonly float _attackIntervalSeconds;
        private float _attackCooldownSeconds;

        public InterferenceEnemyRules(int maxHealth, float attackIntervalSeconds)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }

            if (attackIntervalSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(attackIntervalSeconds));
            }

            _maxHealth = maxHealth;
            _attackIntervalSeconds = attackIntervalSeconds;
            Reset();
        }

        public int Health { get; private set; }
        public int MaxHealth => _maxHealth;
        public bool IsDefeated => Health <= 0;

        /// <summary>
        /// 推进攻击冷却；仅当目标仍在范围内且冷却结束时返回一次攻击许可。
        /// </summary>
        public bool Advance(float deltaTime, bool targetInRange)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (IsDefeated)
            {
                return false;
            }

            _attackCooldownSeconds = Math.Max(0f, _attackCooldownSeconds - deltaTime);
            if (!targetInRange || _attackCooldownSeconds > 0f)
            {
                return false;
            }

            _attackCooldownSeconds = _attackIntervalSeconds;
            return true;
        }

        /// <summary>
        /// 应用一次离散工具命中；返回值表示本次命中后是否被击败。
        /// </summary>
        public bool ReceiveHit(int damage)
        {
            if (damage <= 0 || IsDefeated)
            {
                return IsDefeated;
            }

            Health = Math.Max(0, Health - damage);
            return IsDefeated;
        }

        public void Reset()
        {
            Health = _maxHealth;
            // 初次接触目标时允许立即干扰，让敌人行为与危险原因保持清晰。
            _attackCooldownSeconds = 0f;
        }
    }
}

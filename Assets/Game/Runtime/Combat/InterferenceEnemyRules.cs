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
        private readonly float _attackWindupSeconds;
        private float _attackCooldownSeconds;
        private float _windupRemainingSeconds;

        public InterferenceEnemyRules(int maxHealth, float attackIntervalSeconds, float attackWindupSeconds = 0.4f)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }

            if (attackIntervalSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(attackIntervalSeconds));
            }

            if (attackWindupSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(attackWindupSeconds));
            }

            _maxHealth = maxHealth;
            _attackIntervalSeconds = attackIntervalSeconds;
            _attackWindupSeconds = attackWindupSeconds;
            Reset();
        }

        public int Health { get; private set; }
        public int MaxHealth => _maxHealth;
        public bool IsDefeated => Health <= 0;
        public bool IsTelegraphing { get; private set; }

        /// <summary>
        /// 推进攻击冷却；仅当目标仍在范围内且冷却结束时返回一次攻击许可。
        /// </summary>
        public InterferenceEnemyAction Advance(float deltaTime, bool targetInRange)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (IsDefeated)
            {
                return InterferenceEnemyAction.None;
            }

            _attackCooldownSeconds = Math.Max(0f, _attackCooldownSeconds - deltaTime);
            if (!targetInRange)
            {
                IsTelegraphing = false;
                _windupRemainingSeconds = 0f;
                return InterferenceEnemyAction.None;
            }

            if (IsTelegraphing)
            {
                _windupRemainingSeconds = Math.Max(0f, _windupRemainingSeconds - deltaTime);
                if (_windupRemainingSeconds > 0f)
                {
                    return InterferenceEnemyAction.None;
                }

                IsTelegraphing = false;
                _attackCooldownSeconds = _attackIntervalSeconds;
                return InterferenceEnemyAction.AttackCommitted;
            }

            if (_attackCooldownSeconds > 0f)
            {
                return InterferenceEnemyAction.None;
            }

            IsTelegraphing = true;
            _windupRemainingSeconds = _attackWindupSeconds;
            if (_attackWindupSeconds > 0f)
            {
                return InterferenceEnemyAction.TelegraphStarted;
            }

            IsTelegraphing = false;
            _attackCooldownSeconds = _attackIntervalSeconds;
            return InterferenceEnemyAction.AttackCommitted;
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
            // 初次接触会先进入蓄力，而不是无提示地立即扣除设备完整度。
            _attackCooldownSeconds = 0f;
            _windupRemainingSeconds = 0f;
            IsTelegraphing = false;
        }
    }
}

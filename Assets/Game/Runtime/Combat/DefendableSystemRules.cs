using System;

namespace FunGame.Combat
{
    /// <summary>
    /// 保存被防卫设备的完整度，避免把首个战斗原型扩展为玩家生命或传统血条系统。
    /// </summary>
    public sealed class DefendableSystemRules
    {
        private readonly int _maxIntegrity;

        public DefendableSystemRules(int maxIntegrity)
        {
            if (maxIntegrity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxIntegrity));
            }

            _maxIntegrity = maxIntegrity;
            Reset();
        }

        public int Integrity { get; private set; }
        public int MaxIntegrity => _maxIntegrity;
        public bool IsOffline => Integrity <= 0;

        /// <summary>
        /// 应用敌人干扰并返回设备是否因此离线。
        /// </summary>
        public bool ApplyInterference(int damage)
        {
            if (damage <= 0 || IsOffline)
            {
                return IsOffline;
            }

            Integrity = Math.Max(0, Integrity - damage);
            return IsOffline;
        }

        public void Reset()
        {
            Integrity = _maxIntegrity;
        }
    }
}

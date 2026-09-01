using System;

namespace FunGame.Interaction
{
    /// <summary>
    /// 保存与物理命中顺序无关的上下文动作选择规则。
    /// </summary>
    public static class InteractionSelection
    {
        /// <summary>
        /// 判断候选动作是否应替换当前动作。
        /// 条件不足不会降低动作优先级，避免误触另一个低优先级动作。
        /// </summary>
        public static bool IsBetter(InteractionOption candidate, InteractionOption current)
        {
            if (candidate.Priority != current.Priority)
            {
                return candidate.Priority > current.Priority;
            }

            return string.Compare(candidate.TargetId, current.TargetId, StringComparison.Ordinal) < 0;
        }
    }
}

using UnityEngine;

namespace FunGame.Tools
{
    /// <summary>
    /// 一次已执行工具动作的只读结果，供表现层消费而不反向持有玩法规则。
    /// </summary>
    public readonly struct ToolActionFeedback
    {
        public ToolActionFeedback(ToolKind tool, MonoBehaviour target, bool succeeded)
        {
            Tool = tool;
            Target = target;
            Succeeded = succeeded;
        }

        public ToolKind Tool { get; }
        public MonoBehaviour Target { get; }
        public bool Succeeded { get; }
    }
}

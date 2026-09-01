namespace FunGame.Interaction
{
    /// <summary>
    /// 描述一次可显示、可比较的上下文动作，不保存 Unity 场景对象引用。
    /// </summary>
    public readonly struct InteractionOption
    {
        public InteractionOption(
            string targetId,
            string targetName,
            string actionLabel,
            InteractionPriority priority,
            bool isAvailable = true,
            string unavailableReason = "")
        {
            TargetId = targetId;
            TargetName = targetName;
            ActionLabel = actionLabel;
            Priority = priority;
            IsAvailable = isAvailable;
            UnavailableReason = unavailableReason;
        }

        public string TargetId { get; }
        public string TargetName { get; }
        public string ActionLabel { get; }
        public InteractionPriority Priority { get; }
        public bool IsAvailable { get; }
        public string UnavailableReason { get; }
    }
}

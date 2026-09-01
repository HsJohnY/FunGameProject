namespace FunGame.Tools
{
    /// <summary>
    /// 描述当前工具对准目标时的具体动作和兼容性反馈。
    /// </summary>
    public readonly struct ToolActionOption
    {
        public ToolActionOption(
            string targetId,
            string targetName,
            string actionLabel,
            ToolKind requiredTool,
            ToolKind equippedTool,
            bool targetAllowsAction = true,
            string blockedReason = "")
        {
            TargetId = targetId;
            TargetName = targetName;
            ActionLabel = actionLabel;
            RequiredTool = requiredTool;
            EquippedTool = equippedTool;
            IsAvailable = targetAllowsAction && requiredTool == equippedTool;
            BlockedReason = !targetAllowsAction
                ? blockedReason
                : IsAvailable
                    ? string.Empty
                    : $"需要{requiredTool.GetDisplayName()}";
        }

        public string TargetId { get; }
        public string TargetName { get; }
        public string ActionLabel { get; }
        public ToolKind RequiredTool { get; }
        public ToolKind EquippedTool { get; }
        public bool IsAvailable { get; }
        public string BlockedReason { get; }
    }
}

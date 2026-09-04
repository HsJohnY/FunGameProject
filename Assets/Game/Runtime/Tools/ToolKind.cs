namespace FunGame.Tools
{
    /// <summary>
    /// 首个灰盒允许进入玩家主工具位的工具类型。
    /// </summary>
    public enum ToolKind
    {
        None = 0,
        ImpactWrench = 1,
        SealantGun = 2,
        CircuitBridger = 3
    }

    public static class ToolKindExtensions
    {
        public static string GetDisplayName(this ToolKind tool)
        {
            switch (tool)
            {
                case ToolKind.ImpactWrench:
                    return "冲击扳手";
                case ToolKind.SealantGun:
                    return "密封喷枪";
                case ToolKind.CircuitBridger:
                    return "线路桥接器";
                default:
                    return "空手";
            }
        }
    }
}

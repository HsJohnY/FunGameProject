namespace FunGame.Interaction
{
    /// <summary>
    /// 定义同一准星目标上多个上下文动作的稳定优先级。
    /// 数值越大优先级越高，不允许使用场景组件顺序代替此规则。
    /// </summary>
    public enum InteractionPriority
    {
        Inspect = 100,
        Pickup = 200,
        Device = 300,
        ContinuousAction = 400,
        TaskItemPlacement = 500,
        EmergencyRescue = 600
    }
}

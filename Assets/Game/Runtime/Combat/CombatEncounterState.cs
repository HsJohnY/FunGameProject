namespace FunGame.Combat
{
    /// <summary>
    /// 描述最小防卫遭遇当前所处的可观察阶段。
    /// </summary>
    public enum CombatEncounterState
    {
        Dormant = 0,
        Active = 1,
        Succeeded = 2,
        Failed = 3
    }
}

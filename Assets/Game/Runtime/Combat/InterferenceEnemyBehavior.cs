namespace FunGame.Combat
{
    /// <summary>
    /// 首批受控敌人变化；三者都只威胁设备，不引入玩家生命值或独立武器体系。
    /// </summary>
    public enum InterferenceEnemyBehavior
    {
        Direct = 0,
        FlankingAttach = 1,
        RangedPulse = 2
    }
}

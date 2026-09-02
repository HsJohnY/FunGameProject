namespace FunGame.Combat
{
    /// <summary>
    /// 首批受控敌人变化；两者都只威胁设备，不引入玩家生命值。
    /// </summary>
    public enum InterferenceEnemyBehavior
    {
        Direct = 0,
        FlankingAttach = 1
    }
}

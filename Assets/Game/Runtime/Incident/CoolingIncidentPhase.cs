namespace FunGame.Incident
{
    /// <summary>
    /// 冷却舱固定事故链的唯一权威阶段。
    /// </summary>
    public enum CoolingIncidentPhase
    {
        ContainLeak = 0,
        LoosenConnection = 1,
        InstallReplacementPipe = 2,
        TightenConnection = 3,
        ResetPump = 4,
        Stabilized = 5
    }
}

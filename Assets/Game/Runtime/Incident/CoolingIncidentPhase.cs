namespace FunGame.Incident
{
    /// <summary>
    /// 冷却舱固定事故链的唯一权威阶段。
    /// </summary>
    public enum CoolingIncidentPhase
    {
        AssessSymptoms = -2,
        RestoreControlPower = -1,
        ContainLeak = 0,
        LoosenConnection = 1,
        InstallReplacementPipe = 2,
        TightenConnection = 3,
        VerifyPressure = 4,
        ResetPump = 5,
        Stabilized = 6
    }
}

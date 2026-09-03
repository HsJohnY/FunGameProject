namespace FunGame.Networking
{
    /// <summary>
    /// 网络事故场景中可由上下文交互触发的最小动作集合。
    /// </summary>
    public enum NetworkIncidentAction
    {
        SealLeak = 0,
        OperateFastener = 1,
        InstallPipe = 2,
        OperatePump = 3
    }
}

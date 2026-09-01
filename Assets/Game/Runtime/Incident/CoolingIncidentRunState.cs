namespace FunGame.Incident
{
    /// <summary>
    /// 一次事故的运行结果状态，与事故内部维修阶段分开保存。
    /// </summary>
    public enum CoolingIncidentRunState
    {
        Active = 0,
        Succeeded = 1,
        Failed = 2
    }
}

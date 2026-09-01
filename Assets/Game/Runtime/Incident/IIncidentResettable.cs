namespace FunGame.Incident
{
    /// <summary>
    /// 参与事故重置的场景对象，实现后必须恢复到初始可玩状态。
    /// </summary>
    public interface IIncidentResettable
    {
        void ResetIncidentState();
    }
}

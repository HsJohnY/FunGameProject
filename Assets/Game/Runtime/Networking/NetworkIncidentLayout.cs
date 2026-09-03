using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// 事故交互站的确定性布局，同时供场景生成器和服务器距离验证使用。
    /// </summary>
    public static class NetworkIncidentLayout
    {
        public static Vector3 GetStationPosition(NetworkIncidentAction action)
        {
            return action switch
            {
                NetworkIncidentAction.SealLeak => new Vector3(-4f, 1f, 7f),
                NetworkIncidentAction.OperateFastener => new Vector3(0f, 1f, 7f),
                NetworkIncidentAction.OperatePump => new Vector3(4f, 1f, 7f),
                NetworkIncidentAction.InstallPipe => new Vector3(0f, 1f, 10f),
                _ => Vector3.zero
            };
        }
    }
}

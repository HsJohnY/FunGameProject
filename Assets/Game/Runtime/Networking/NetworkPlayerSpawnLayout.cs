using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// 为网络验证场景提供确定性的出生位置。
    /// 同一个客户端编号在各端都会得到相同结果，且不会依赖场景对象查找顺序。
    /// </summary>
    public static class NetworkPlayerSpawnLayout
    {
        private const float HorizontalSpacing = 2.5f;

        public static Vector3 GetSpawnPosition(ulong clientId)
        {
            // MVP 目标为 2–4 人；超过四人时继续沿 X 轴排列，避免意外重叠。
            float centeredIndex = (float)clientId - 1.5f;
            return new Vector3(centeredIndex * HorizontalSpacing, 1f, -3f);
        }
    }
}

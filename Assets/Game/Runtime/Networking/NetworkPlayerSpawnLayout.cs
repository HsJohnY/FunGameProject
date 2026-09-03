using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// 为网络验证场景提供确定性的出生位置。
    /// 同一个客户端编号在各端都会得到相同结果，且不会依赖场景对象查找顺序。
    /// </summary>
    public static class NetworkPlayerSpawnLayout
    {
        public const float FallResetHeight = -8f;

        private const float HorizontalSpacing = 4f;
        private const float RowSpacing = 4f;

        public static Vector3 GetSpawnPosition(ulong clientId)
        {
            // 2–4 人按两列排列；更大的客户端编号继续向后扩展行数，避免意外重叠。
            float x = clientId % 2 == 0 ? -HorizontalSpacing * 0.5f : HorizontalSpacing * 0.5f;
            float z = -4f + clientId / 2 * RowSpacing;
            return new Vector3(x, 1.1f, z);
        }
    }
}

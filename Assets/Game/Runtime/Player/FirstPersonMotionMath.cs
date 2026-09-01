using UnityEngine;

namespace FunGame.Player
{
    /// <summary>
    /// 保存第一人称移动中不依赖场景状态的计算规则，便于快速单元测试。
    /// </summary>
    public static class FirstPersonMotionMath
    {
        public const float MinimumPitch = -85f;
        public const float MaximumPitch = 85f;

        /// <summary>
        /// 限制镜头俯仰角，防止越过头顶后发生视角翻转。
        /// </summary>
        public static float ClampPitch(float pitch)
        {
            return Mathf.Clamp(pitch, MinimumPitch, MaximumPitch);
        }

        /// <summary>
        /// 将二维移动输入限制在单位圆内，避免斜向移动比直线移动更快。
        /// </summary>
        public static Vector2 ClampMoveInput(Vector2 input)
        {
            return Vector2.ClampMagnitude(input, 1f);
        }
    }
}

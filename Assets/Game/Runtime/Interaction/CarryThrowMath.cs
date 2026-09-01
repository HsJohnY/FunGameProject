using UnityEngine;

namespace FunGame.Interaction
{
    /// <summary>
    /// 保存轻型抛放的方向计算；质量差异交给 Rigidbody 冲量模型处理。
    /// </summary>
    public static class CarryThrowMath
    {
        /// <summary>
        /// 计算固定前向冲量和最低向上冲量之和。
        /// 向上观察会自然增加垂直分量，形成更高的抛物线。
        /// </summary>
        public static Vector3 CalculateImpulse(Vector3 viewForward, float forwardImpulse, float upwardImpulse)
        {
            Vector3 normalizedForward = viewForward.sqrMagnitude > 0f ? viewForward.normalized : Vector3.forward;
            return normalizedForward * Mathf.Max(0f, forwardImpulse) + Vector3.up * Mathf.Max(0f, upwardImpulse);
        }
    }
}

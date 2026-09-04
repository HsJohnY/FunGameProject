using UnityEngine;

namespace FunGame.Demo
{
    public readonly struct DemoMarkerPlacement
    {
        public DemoMarkerPlacement(Vector2 position, bool isEdge, float behindSide)
        {
            Position = position;
            IsEdge = isEdge;
            BehindSide = behindSide;
        }

        public Vector2 Position { get; }
        public bool IsEdge { get; }
        public float BehindSide { get; }
    }

    /// <summary>
    /// 为屏幕内和屏幕外目标计算稳定标记位置；目标在正后方时保留上一侧，避免左右乱跳。
    /// </summary>
    public static class DemoMarkerLayout
    {
        public static DemoMarkerPlacement Calculate(
            Vector3 cameraLocalTarget,
            Vector2 projectedGuiPosition,
            Rect safeRect,
            float previousBehindSide)
        {
            float rememberedSide = Mathf.Approximately(previousBehindSide, 0f)
                ? 1f
                : Mathf.Sign(previousBehindSide);
            bool inFront = cameraLocalTarget.z > 0.01f;
            if (inFront && safeRect.Contains(projectedGuiPosition))
            {
                return new DemoMarkerPlacement(projectedGuiPosition, false, rememberedSide);
            }

            Vector2 center = safeRect.center;
            Vector2 direction;
            if (!inFront)
            {
                // 正后方附近的左右符号对微小视角变化很敏感。死区按距离放大，
                // 只有玩家已经明确朝另一侧转动后才换边；背后目标也始终贴左右边，
                // 避免轻微俯仰把提示甩到屏幕底部。
                float sideSwitchThreshold = Mathf.Max(0.55f, Mathf.Abs(cameraLocalTarget.z) * 0.45f);
                if (Mathf.Abs(cameraLocalTarget.x) > sideSwitchThreshold)
                {
                    rememberedSide = Mathf.Sign(cameraLocalTarget.x);
                }

                direction = new Vector2(rememberedSide, 0f);
            }
            else
            {
                direction = projectedGuiPosition - center;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = new Vector2(rememberedSide, 0f);
            }

            Vector2 half = safeRect.size * 0.5f;
            float horizontalScale = Mathf.Abs(direction.x) > 0.0001f
                ? half.x / Mathf.Abs(direction.x)
                : float.MaxValue;
            float verticalScale = Mathf.Abs(direction.y) > 0.0001f
                ? half.y / Mathf.Abs(direction.y)
                : float.MaxValue;
            float scale = Mathf.Min(horizontalScale, verticalScale);
            Vector2 position = center + direction * scale;
            return new DemoMarkerPlacement(position, true, rememberedSide);
        }
    }
}

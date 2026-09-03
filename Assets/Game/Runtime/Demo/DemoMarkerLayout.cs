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
                // 正后方附近的左右符号对微小视角变化很敏感；越过明显死区后才允许换边。
                if (Mathf.Abs(cameraLocalTarget.x) > 0.55f)
                {
                    rememberedSide = Mathf.Sign(cameraLocalTarget.x);
                }

                float vertical = Mathf.Clamp(
                    -cameraLocalTarget.y / Mathf.Max(1f, Mathf.Abs(cameraLocalTarget.z)),
                    -0.65f,
                    0.65f);
                direction = new Vector2(rememberedSide, vertical);
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

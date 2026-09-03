namespace FunGame.Networking
{
    public enum NetworkQualityLevel
    {
        Good,
        Playable,
        Degraded
    }

    /// <summary>
    /// 仅用于 M3-6 验证记录，不自动踢出玩家或改变同步参数。
    /// </summary>
    public static class NetworkQualityRules
    {
        public static NetworkQualityLevel Evaluate(ulong roundTripMilliseconds)
        {
            if (roundTripMilliseconds <= 100) return NetworkQualityLevel.Good;
            if (roundTripMilliseconds <= 200) return NetworkQualityLevel.Playable;
            return NetworkQualityLevel.Degraded;
        }

        public static string GetLabel(ulong roundTripMilliseconds)
        {
            return Evaluate(roundTripMilliseconds) switch
            {
                NetworkQualityLevel.Good => "良好",
                NetworkQualityLevel.Playable => "可玩",
                _ => "较差"
            };
        }
    }
}

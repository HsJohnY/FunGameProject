using System;
using System.Net;

namespace FunGame.Networking
{
    /// <summary>
    /// 会话地址的纯规则层。它不依赖 Unity 生命周期，便于单元测试和后续替换大厅界面。
    /// </summary>
    public static class NetworkEndpointRules
    {
        public const ushort DefaultPort = 7777;
        public const string DefaultAddress = "127.0.0.1";

        public static bool TryNormalize(string addressText, string portText, out string address, out ushort port, out string error)
        {
            address = addressText?.Trim() ?? string.Empty;
            port = 0;

            if (string.IsNullOrWhiteSpace(address))
            {
                error = "请输入主机 IPv4 地址";
                return false;
            }

            if (!IPAddress.TryParse(address, out IPAddress parsedAddress) ||
                parsedAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                error = "地址必须是有效的 IPv4，例如 127.0.0.1";
                return false;
            }

            address = parsedAddress.ToString();
            if (!ushort.TryParse(portText?.Trim(), out port) || port == 0)
            {
                error = "端口必须是 1–65535 的整数";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}

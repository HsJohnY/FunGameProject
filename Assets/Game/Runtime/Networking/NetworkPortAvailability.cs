using System.Net;
using System.Net.Sockets;

namespace FunGame.Networking
{
    /// <summary>
    /// 在启动 Unity Transport 前检查本机 UDP 监听端口，避免已知冲突进入底层错误流程。
    /// 这只是启动前检查；极小概率的检查后竞争仍由 OnTransportFailure 兜底。
    /// </summary>
    public static class NetworkPortAvailability
    {
        public static bool CanBindUdp(ushort port)
        {
            Socket probe = null;
            try
            {
                probe = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                {
                    ExclusiveAddressUse = true
                };
                probe.Bind(new IPEndPoint(IPAddress.Any, port));
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            finally
            {
                probe?.Dispose();
            }
        }
    }
}

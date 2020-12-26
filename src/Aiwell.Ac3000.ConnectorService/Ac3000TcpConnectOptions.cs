using System.Net.Sockets;

namespace Aiwell.Ac3000
{
    public class Ac3000TcpConnectOptions
    {
        public string Host { get; set; } = null!;
        public int Port { get; set; }
    }
}

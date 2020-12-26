using System.Net.Sockets;

namespace Aiwell.Ac3000
{
    public class Ac3000TcpOptions
    {
        public string Host { get; set; } = null!;
        public int Port { get; set; }
    }
}

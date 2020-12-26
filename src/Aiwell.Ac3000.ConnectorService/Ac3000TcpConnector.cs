using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aiwell.Ac3000
{
    public class Ac3000TcpConnector : Ac3000BaseConnector
    {
        private readonly IOptionsMonitor<Ac3000TcpConnectOptions> optionsMonitor;
        private readonly TcpClient tcpClient;

        public Ac3000TcpConnector(
            IOptionsMonitor<Ac3000TcpConnectOptions> optionsMonitor,
            TcpClient tcpClient,
            ILoggerFactory? loggerFactory = null
            ) : base(loggerFactory)
        {
            this.tcpClient = tcpClient ??
                throw new ArgumentNullException(nameof(tcpClient));
            this.optionsMonitor = optionsMonitor;
        }

        public override async Task ConnectAsync(CancellationToken cancelToken = default)
        {
            var options = optionsMonitor.CurrentValue;
            var hostname = options.Host;
            var port = options.Port;
            Logger.LogDebug(new EventId(0, "Connecting"),
                $"->? Connecting to {{{nameof(hostname)}}}:{{{nameof(port)}}}",
                hostname, port);
            await tcpClient.ConnectAsync(hostname, port, cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            Stream?.Dispose();
            Stream = tcpClient.GetStream();
            Logger.LogDebug(new EventId(1, "Connected"),
                $"<-> Connection established to {{{nameof(tcpClient.Client.RemoteEndPoint)}}}",
                tcpClient.Client.RemoteEndPoint);
        }

        public override void Close()
        {
            Logger.LogDebug(new EventId(6, "ClosingConnection"),
                $"<-> Closing connection to {{{nameof(tcpClient.Client.RemoteEndPoint)}}}",
                tcpClient.Client.RemoteEndPoint);
            Stream.Close();
            tcpClient.Close();
            Logger.LogDebug(new EventId(7, "Disconnected"),
                "</> Connection closed");
        }

        #region IDisposable Implementation
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            tcpClient.Dispose();
        }
        #endregion
    }
}

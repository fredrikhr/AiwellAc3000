using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Aiwell.Ac3000
{
    public class Ac3000SerialPortConnector : Ac3000BaseConnector
    {
        private readonly SerialPort serialPort;

        public override bool IsConnected => serialPort.IsOpen;

        public Ac3000SerialPortConnector(
            SerialPort serialPort,
            ILoggerFactory? loggerFactory = null
            ) : base(loggerFactory)
        {
            this.serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
        }

        public override Task ConnectAsync(CancellationToken cancelToken = default)
        {
            serialPort.Open();
            Stream = serialPort.BaseStream;
            Logger.LogDebug(new EventId(1, "Connected"),
                $"<-> Serial Port connection established with port {{{nameof(serialPort.PortName)}}}",
                serialPort.PortName);
            return Task.CompletedTask;
        }

        public override void Close()
        {
            serialPort.Close();
            Stream = null!;
            Logger.LogDebug(new EventId(7, "Disconnected"),
                "</> Serial Port connection closed");
        }

        #region IDisposable Implementation
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            serialPort.Dispose();
        }
        #endregion
    }
}

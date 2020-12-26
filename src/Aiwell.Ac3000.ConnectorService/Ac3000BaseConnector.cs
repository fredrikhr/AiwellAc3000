using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Aiwell.Ac3000
{
    public abstract class Ac3000BaseConnector : IDisposable
    {
        private Stream stream = Stream.Null;
        private static readonly PipeReader nullReader = PipeReader.Create(Stream.Null);

        protected ILogger Logger { get; }
        protected Stream Stream
        {
            get => stream;
            set
            {
                stream = value ?? Stream.Null;
                Reader = value is null || value == Stream.Null
                    ? nullReader : PipeReader.Create(stream);
            }
        }
        protected PipeReader Reader { get; private set; } = nullReader;

        public Ac3000BaseConnector(
            ILoggerFactory? loggerFactory = null
            ) : base()
        {
            loggerFactory ??= Microsoft.Extensions.Logging.Abstractions
                .NullLoggerFactory.Instance;
            Logger = loggerFactory.CreateLogger(GetType());
        }

        public abstract Task ConnectAsync(CancellationToken cancelToken = default);

        protected Task WriteAsync(ReadOnlyMemory<byte> buffer,
            CancellationToken cancelToken = default)
        {
            Logger.LogDebug(new EventId(2, "SendingData"),
                "==> ({BytesSent} B) [{Data}]",
                buffer.Length.ToString(CultureInfo.InvariantCulture).PadLeft(3),
                new Ac3000PayloadHexString(buffer));
            return Stream.WriteAsync(buffer, cancelToken).AsTask();
        }

        protected async Task<ReadOnlyMemory<byte>> ReadEnsureLengthAsync(
            int length, CancellationToken cancelToken = default)
        {
            ReadResult readResult;
            for (readResult = await Reader.ReadAsync(cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false);
                readResult.Buffer.Length < length;
                readResult = await Reader.ReadAsync(cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false))
            {
                Logger.LogDebug(new EventId(5, "ReceivedPartialData"),
                    "<== Partial data received, waiting to receive more ({BytesReceived} B / {BytesExpected} B)" + Environment.NewLine +
                    "[{Data}]",
                    readResult.Buffer.Length.ToString(CultureInfo.InvariantCulture),
                    length.ToString(CultureInfo.InvariantCulture),
                    new Ac3000PayloadHexString(readResult.Buffer));
            }
            var readSlice = readResult.Buffer.Slice(0, length);
            ReadOnlyMemory<byte> readBuffer = readSlice.ToArray();
            Reader.AdvanceTo(readSlice.End);
            Logger.LogDebug(new EventId(4, "ReceivedData"),
                "<== ({BytesReceived} B) {Data}",
                readBuffer.Length.ToString(CultureInfo.InvariantCulture).PadLeft(3),
                new Ac3000PayloadHexString(readBuffer));
            return readBuffer;
        }

        public virtual async Task<ReadOnlyMemory<byte>> ExchangeDataAsync(
            ReadOnlyMemory<byte> request, int responseLength,
            CancellationToken cancelToken = default)
        {
            await WriteAsync(request, cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            Logger.LogDebug(new EventId(3, "WaitingToReceiveData"),
                "<=? Waiting to receive data, {Expected} byte(s) expected.",
                responseLength);
            return await ReadEnsureLengthAsync(responseLength, cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public abstract void Close();

        #region IDisposable Implementation
        protected virtual void Dispose(bool disposing)
        {
            stream.Close();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Ac3000BaseConnector() => Dispose(false);
        #endregion
    }
}

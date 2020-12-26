using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Aiwell.Ac3000
{
    public class Ac3000Client
    {
        private readonly Ac3000BaseConnector connector;

        public Ac3000Client(
            Ac3000BaseConnector connector
            )
        {
            this.connector = connector ?? throw new ArgumentNullException(nameof(connector));
        }

        public async Task<Ac3000SystemCounters> GetSystemCounters(
            CancellationToken cancelToken = default)
        {
            var request = new byte[]
            {
                129, 1, 3, 0, 0, 0, 110, 0, default, default
            };
            ModbusCrcUtility.WriteCrc16Trailer(request);

            var response = await connector.ExchangeDataAsync(
                request, responseLength: 120, cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            return ReadVerifySystemCounters(response.Span);
        }

        private static Ac3000SystemCounters ReadVerifySystemCounters(ReadOnlySpan<byte> buffer)
        {
            bool crcCorrect = ModbusCrcUtility.VerifyCrc16Trailer(buffer);

            return new Ac3000SystemCounters
            {
                Ac3000Version = new Version(buffer[8], buffer[9], buffer[10]),
                FRAMVersion = buffer[11],
                DataflashVersion = buffer[12],
                MainPageCounter = BinaryPrimitives.ReadInt16BigEndian(buffer[14..15]),
                ProgramCode = buffer[16],
                ControlRegister = buffer[25],
                NumberOfZones = buffer[59],
                NumberOfPrograms = buffer[60],
                NumberOfSensorInterfaces = buffer[61],
                NumberOfProgramSensors = BinaryPrimitives.ReadInt16BigEndian(buffer[62..63]),
                NumberOfCommonSensors = BinaryPrimitives.ReadInt16BigEndian(buffer[64..65]),
            };
        }
    }

    public class Ac3000SystemCounters
    {
        private static readonly Version NullVersion = new Version();

        public Version Ac3000Version { get; init; } = NullVersion;
        public byte FRAMVersion { get; init; }
        public byte DataflashVersion { get; init; }
        public short MainPageCounter { get; init; }
        public byte ProgramCode { get; init; }
        public byte ControlRegister { get; init; }
        public short NumberOfZones { get; init; }
        public short NumberOfPrograms { get; init; }
        public short NumberOfSensorInterfaces { get; init; }
        public short NumberOfProgramSensors { get; init; }
        public short NumberOfCommonSensors { get; init; }
        public int NumberOfSensors => NumberOfProgramSensors + NumberOfCommonSensors;
    }
}

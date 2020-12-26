using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aiwell.Ac3000
{
    public class Ac3000PayloadHexString
    {
        public ReadOnlyMemory<byte> SingleBuffer { get; }
        public ReadOnlySequence<byte> SequenceBuffer { get; }

        public Ac3000PayloadHexString(ReadOnlyMemory<byte> buffer)
            : base()
        {
            SingleBuffer = buffer;
        }

        public Ac3000PayloadHexString(ReadOnlySequence<byte> sequence)
            : base()
        {
            SequenceBuffer = sequence;
        }

        public override string ToString() => this switch
        {
            { SequenceBuffer: { IsEmpty: false } sequence } => SequenceToHexString(sequence),
            { SingleBuffer: { IsEmpty: false } buffer } => BufferToHexString(buffer),
            _ => string.Empty
        };

        private static string BufferToHexString(ReadOnlyMemory<byte> buffer)
        {
            var hexBuilder = new StringBuilder(buffer.Length * 3 - 1);
            foreach (byte value in buffer.Span)
            {
                if (hexBuilder.Length > 0)
                    hexBuilder.Append(' ');
                hexBuilder.Append(value.ToString("X2", CultureInfo.InvariantCulture));
            }
            return hexBuilder.ToString();
        }

        private static string SequenceToHexString(ReadOnlySequence<byte> sequence)
        {
            var hexBuilder = new StringBuilder((int)(sequence.Length * 3 - 1));
            foreach (var buffer in sequence)
            {
                hexBuilder.Append(BufferToHexString(buffer));
            }
            return hexBuilder.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis.Resp
{
    internal static class DebugHelper
    {
        public static void DumpMemoryToOutput(Memory<byte> memory, int offset)
        {
            var builder = new StringBuilder();
            var charBuilder = new StringBuilder();

            var buffer = memory.Slice(offset, memory.Length - offset > 64 ? 64 : memory.Length - offset).Span;

            // Write the hex
            for (int i = 0; i < buffer.Length; i++)
            {
                builder.Append(buffer[i].ToString("X2", CultureInfo.InvariantCulture));
                builder.Append(' ');

                var bufferChar = (char)buffer[i];
                if (char.IsControl(bufferChar))
                {
                    charBuilder.Append('.');
                }
                else
                {
                    charBuilder.Append(bufferChar);
                }

                if ((i + 1) % 16 == 0)
                {
                    builder.Append("  ");
                    builder.Append(charBuilder);
                    if (i != buffer.Length - 1)
                    {
                        builder.AppendLine();
                    }
                    charBuilder.Clear();
                }
                else if ((i + 1) % 8 == 0)
                {
                    builder.Append(' ');
                    charBuilder.Append(' ');
                }
            }

            // Different than charBuffer.Length since charBuffer contains an extra " " after the 8th byte.
            var numBytesInLastLine = buffer.Length % 16;

            if (numBytesInLastLine > 0)
            {
                // 2 (between hex and char blocks) + num bytes left (3 per byte)
                var padLength = 2 + (3 * (16 - numBytesInLastLine));
                // extra for space after 8th byte
                if (numBytesInLastLine < 8)
                {
                    padLength++;
                }

                builder.Append(new string(' ', padLength));
                builder.Append(charBuilder);
            }

            Debug.WriteLine(builder.ToString());
        }
    }
}

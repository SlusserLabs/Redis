using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis.Resp
{
    /// <summary>
    /// A high-performance API for forward-only, non-cached writing of Redis Serialization Protocol (RESP) v3 encoded data.
    /// </summary>
    public ref struct RespWriter2
    {
        private const int _initialGrowthSize = 256;
        private const int _growthSize = 4096;

        private readonly IBufferWriter<byte> _bufferWriter;
        private readonly bool _skipValidation;

        private Memory<byte> _memory;
        private int _bytesPending;
        private long _bytesCommitted;

        /// <summary>
        /// Initializes a new instance of the <see cref="RespWriter2" /> class with the specified <see cref="IBufferWriter{T}" /> <paramref name="bufferWriter" />.
        /// </summary>
        /// <param name="bufferWriter">The buffer to write RESP data to.</param>
        /// <param name="options">Defines custom behavior of the writer.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="bufferWriter" /> argument is <c>null</c>.</exception>
        public RespWriter2(IBufferWriter<byte> bufferWriter, RespWriterOptions options = default)
        {
            if (bufferWriter == null)
            {
                throw new ArgumentNullException(nameof(bufferWriter));
            }

            _bufferWriter = bufferWriter;
            _skipValidation = options.SkipValidation;

            _memory = default;
            _bytesPending = 0;
            _bytesCommitted = 0;
        }

        /// <summary>
        /// Commits the RESP data buffered so far to the output.
        /// </summary>
        public void Flush()
        {
            // Resetting the memory will cause it to be allocated again on the next call to Grow
            _memory = default; // TODO ???

            if (_bytesPending != 0)
            {
                _bufferWriter.Advance(_bytesPending);
                _bytesCommitted += _bytesPending;
                _bytesPending = 0;
            }
        }

        internal void WriteRaw(ReadOnlySpan<byte> value)
        {
            if (_memory.Length - _bytesPending < value.Length)
            {
                Grow(value.Length);
            }

            value.CopyTo(_memory.Span.Slice(_bytesPending));
            _bytesPending += value.Length;
        }

        /// <summary>
        /// Writes the start of a RESP Array for the specified number of elements.
        /// </summary>
        /// <param name="length">The number of elements in the array.</param>
        /// <remarks>
        /// The following sequence will be written to the buffer: <c>"*{<paramref name="length" />}\r\n".</c>
        /// The caller must subsequently write <paramref name="length" /> elements or the data will be invalid.
        /// </remarks>
        public void WriteArrayStart(long length)
        {
            if (length > 0)
            {
                var decimalCount = length < 10 ? 1 : FormattingHelper.CountDecimalDigits(length);
                var sizeRequired = 3 + decimalCount;
                if (_memory.Length - _bytesPending < sizeRequired)
                {
                    Grow(sizeRequired);
                }

                var output = _memory.Span;
                output[_bytesPending++] = RespConstants.Asterisk;
                if (decimalCount == 1)
                {
                    output[_bytesPending++] = (byte)('0' + length);
                }
                else
                {
                    FormattingHelper.WriteDigits(length, decimalCount, output.Slice(_bytesPending));
                    _bytesPending += decimalCount;
                }

                output[_bytesPending++] = RespConstants.CarriageReturn;
                output[_bytesPending++] = RespConstants.LineFeed;
            }
            else if (length == 0)
            {
                if (_memory.Length - _bytesPending < RespConstants.EmptyArray.Length)
                {
                    Grow(RespConstants.EmptyArray.Length);
                }

                RespConstants.EmptyArray.CopyTo(_memory.Span.Slice(_bytesPending));
                _bytesPending += RespConstants.EmptyArray.Length;
            }
            else if (length == -1)
            {
                if (_memory.Length - _bytesPending < RespConstants.NullArray.Length)
                {
                    Grow(RespConstants.NullArray.Length);
                }

                RespConstants.NullArray.CopyTo(_memory.Span.Slice(_bytesPending));
                _bytesPending += RespConstants.NullArray.Length;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(length), "A RESP Array can be 0 or more elements long, or -1 to indicate a Null Array");
            }
        }

        /// <summary>
        /// Writes a RESP Bulk String containing the single byte <paramref name="value" />.
        /// </summary>
        /// <param name="value">The single byte string to write.</param>
        /// <remarks>The following sequence will be written to the buffer: "$1\r\n{<paramref name="value" />}\r\n".</remarks>
        public void WriteBulkString(byte value)
        {
            if (_memory.Length - _bytesPending < 7)
            {
                Grow(7);
            }

            var output = _memory.Span;
            output[_bytesPending++] = RespConstants.Dollar;
            output[_bytesPending++] = (byte)('1');
            output[_bytesPending++] = RespConstants.CarriageReturn;
            output[_bytesPending++] = RespConstants.LineFeed;
            output[_bytesPending++] = value;
            output[_bytesPending++] = RespConstants.CarriageReturn;
            output[_bytesPending++] = RespConstants.LineFeed;
        }

        /// <summary>
        /// Writes a RESP Bulk String.
        /// </summary>
        /// <param name="value">The string of bytes to write.</param>
        /// <exception cref="InvalidOperationException">The length of <paramref name="value" /> exceeds 512 MBs and <see cref="RespWriterOptions.SkipValidation" /> is not enabled.</exception>
        /// <remarks>The following sequence will be written to the buffer: "${length}\r\n{<paramref name="value" />}\r\n".</remarks>
        public void WriteBulkString(ReadOnlySpan<byte> value)
        {
            if (!_skipValidation && value.Length > RespConstants.MaxBulkStringLength)
            {
                // TODO Any other data to include in the exception?
                throw new InvalidOperationException("Bulk strings cannot exceed 512 MBs.");
            }

            // TODO We need to handle large strings without buffering everything

            // Determine the number of bytes to write and whether to grow the buffer
            var decimalDigitsByteCount = value.Length < 10 ? 1 : FormattingHelper.CountDecimalDigits((uint)value.Length);
            var totalByteCount = decimalDigitsByteCount + value.Length + 5; // Dollar, {length} carriage return, line feed, {value}, carriage return, line feed
            if (_memory.Length - _bytesPending < totalByteCount)
            {
                Grow(totalByteCount);
            }

            var output = _memory.Span;

            output[_bytesPending++] = RespConstants.Dollar;

            if (decimalDigitsByteCount == 1)
            {
                output[_bytesPending++] = (byte)('0' + value.Length);
            }
            else
            {
                FormattingHelper.WriteDigits((uint)value.Length, output.Slice(_bytesPending, decimalDigitsByteCount));
                _bytesPending += decimalDigitsByteCount;
            }

            output[_bytesPending++] = RespConstants.CarriageReturn;
            output[_bytesPending++] = RespConstants.LineFeed;

            value.CopyTo(output.Slice(_bytesPending));
            _bytesPending += value.Length;

            output[_bytesPending++] = RespConstants.CarriageReturn;
            output[_bytesPending++] = RespConstants.LineFeed;
        }

        // TODO DEbug only
        internal void DumpMemoryToOutput()
        {
            DebugHelper.DumpMemoryToOutput(_memory, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureAvailableMemory(int requiredSize)
        {
            if (_memory.Length - _bytesPending < requiredSize)
            {
                Grow(requiredSize);
            }
        }

        private void Grow(int sizeRequired)
        {
            Debug.Assert(sizeRequired > 0);

            // TODO I think this may only allow initialGrowthSize and never growthSize

            int growSize;
            if (_memory.Length == 0)
            {
                // First time putting data in this buffer
                growSize = _initialGrowthSize;
            }
            else
            {
                Flush();
                growSize = _growthSize;
            }

            Debug.Assert(_bytesPending == 0);

            var sizeHint = Math.Max(growSize, sizeRequired);
            _memory = _bufferWriter.GetMemory(sizeHint);

            Debug.Assert(_memory.Length >= sizeHint);
        }
    }
}

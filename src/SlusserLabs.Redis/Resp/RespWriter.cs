using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis.Resp
{
    /// <summary>
    /// A high-performance API for forward-only, non-cached writing of REdis Serialization Protocol (RESP) v2 encoded data.
    /// </summary>
    public sealed class RespWriter
    {
        private const int _initialGrowthSize = 256;
        private const int _growthSize = 4096;

        private ArrayBufferWriter<byte>? _arrayBufferWriter;
        private IBufferWriter<byte>? _bufferWriter;
        private Stream? _stream;

        private RespWriterOptions _options;

        private Memory<byte> _memory;
        private int _bytesPending;
        private long _bytesCommitted;

        /// <summary>
        /// Initializes a new instance of the <see cref="RespWriter" /> class with the specified <see cref="IBufferWriter{byte}" /> <paramref name="output" />.
        /// </summary>
        /// <param name="output">The buffer to write RESP data to.</param>
        /// <param name="options">Defines custom behavior of the writer.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="output" /> argument is <c>null</c>.</exception>
        public RespWriter(IBufferWriter<byte> output, RespWriterOptions options = default)
        {
            _bufferWriter = output ?? throw new ArgumentNullException(nameof(output));
            _options = options;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RespWriter" /> class with the specified <see cref="Stream" /> <paramref name="output" />.
        /// </summary>
        /// <param name="output">The stream to write RESP data to.</param>
        /// <param name="options">Defines custom behavior of the writer.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="output" /> argument is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="output" /> stream does not support writing.</exception>
        public RespWriter(Stream output, RespWriterOptions options = default)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (!output.CanWrite)
                throw new ArgumentException("Stream is not writable.", nameof(output));

            _stream = output;
            _options = options;
            _arrayBufferWriter = new ArrayBufferWriter<byte>();
        }

        /// <summary>
        /// Gets the <see cref="RespWriterOptions" /> used to create this <see cref="RespWriter" /> instance.
        /// </summary>
        public RespWriterOptions Options => _options;

        /// <summary>
        /// Gets the number of bytes buffered, waiting to be flushed to the output.
        /// </summary>
        public int BytesPending => _bytesPending;

        /// <summary>
        /// Gets the number of bytes written to the output by the <see cref="RespWriter" />.
        /// </summary>
        /// <remarks>
        /// For instances constructed with <see cref="IBufferWriter{byte}" />, this is how much the buffer has advanced.
        /// For instances constructed with <see cref="Stream" />, this is the number of bytes written to the stream.
        /// </remarks>
        public long BytesCommitted => _bytesCommitted;

        /// <summary>
        /// Commits the RESP data buffered so far to the output.
        /// </summary>
        /// <remarks>
        /// For instances constructed with <see cref="IBufferWriter{byte}" />, this advances the writer to include the buffered data.
        /// For instances constructed with <see cref="Stream" />, this writes data to the stream and flushes it.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="RespWriter" /> has been disposed.</exception>
        public void Flush()
        {
            CheckNotDisposed();

            // Resetting the memory will cause it to be allocated again
            // on the next call to Grow.
            _memory = default;

            if (_stream != null)
            {
                Debug.Assert(_arrayBufferWriter != null);
                if (_bytesPending != 0)
                {
                    _arrayBufferWriter.Advance(_bytesPending);
                    _stream.Write(_arrayBufferWriter.WrittenSpan);
                    _arrayBufferWriter.Clear();

                    _bytesCommitted += _bytesPending;
                    _bytesPending = 0;
                }

                _stream.Flush();
            }
            else
            {
                Debug.Assert(_bufferWriter != null);
                if (_bytesPending != 0)
                {
                    _bufferWriter.Advance(_bytesPending);
                    _bytesCommitted += _bytesPending;
                    _bytesPending = 0;
                }
            }
        }

        /// <summary>
        /// Writes a Null Bulk String, "$-1\r\n".
        /// </summary>
        public void WriteNullBulkString()
        {
            var sizeRequired = RespConstants.NullBulkString.Length;
            if (_memory.Length - _bytesPending < sizeRequired)
            {
                Grow(sizeRequired);
            }

            var output = _memory.Span;
            output[_bytesPending++] = RespConstants.Dollar;
            output[_bytesPending++] = RespConstants.Minus;
            output[_bytesPending++] = RespConstants.One;
            output[_bytesPending++] = RespConstants.CarriageReturn;
            output[_bytesPending++] = RespConstants.LineFeed;
        }

        /// <summary>
        /// Writes a Bulk String, "$&lt;value-length&gt;\r\n&lt;<paramref name="value" />&gt;\r\n".
        /// </summary>
        /// <param name="value">The string of byte data to write.</param>
        /// <exception cref="InvalidOperationException">The result would be invalid RESP (while validation is enabled).</exception>
        public void WriteBulkString(ReadOnlySpan<byte> value)
        {
            if (!_options.SkipValidation)
            {
                if (value.Length > RespConstants.MaxBulkStringLength)
                    throw new InvalidOperationException("Bulk strings cannot exceed 512 MBs.");
            }

            // Dollar, value length, CarriageReturn, LineFeed, value, CarriageReturn, LineFeed
            var lengthDigits = value.Length < 10 ? 1 : FormattingHelper.CountDigits((uint)value.Length);
            var sizeRequired = 5 + lengthDigits + value.Length;
            if (_memory.Length - _bytesPending < sizeRequired)
                Grow(sizeRequired);

            var output = _memory.Span;

            output[_bytesPending++] = RespConstants.Dollar;

            if (lengthDigits == 1)
            {
                output[_bytesPending++] = (byte)('0' + value.Length);
            }
            else
            {
                FormattingHelper.WriteDigits((uint)value.Length, output.Slice(_bytesPending, lengthDigits));
                _bytesPending += lengthDigits;
            }

            output[_bytesPending++] = RespConstants.CarriageReturn;
            output[_bytesPending++] = RespConstants.LineFeed;

            value.CopyTo(output.Slice(_bytesPending));
            _bytesPending += value.Length;

            output[_bytesPending++] = RespConstants.CarriageReturn;
            output[_bytesPending++] = RespConstants.LineFeed;
        }

        /// <summary>
        /// Writes a Simple String, "+&lt;<paramref name="value"/>&gt;\r\n".
        /// </summary>
        /// <param name="value">The string of byte data to write.</param>
        /// <exception cref="InvalidOperationException">The result would be invalid RESP (while validation is enabled).</exception>
        public void WriteSimpleString(ReadOnlySpan<byte> value)
        {
            if (!_options.SkipValidation)
            {
                if (value.IndexOfAny(RespConstants.CarriageReturn, RespConstants.LineFeed) > 0)
                    throw new InvalidOperationException("Simple strings cannot contain carriage returns or line feeds.");
            }

            // Plus, value, CarriageReturn, LineFeed
            var sizeRequired = 3 + value.Length;
            if (_memory.Length - _bytesPending < sizeRequired)
                Grow(sizeRequired);

            var output = _memory.Span;

            output[_bytesPending++] = RespConstants.Plus;
            value.CopyTo(output.Slice(_bytesPending));
            _bytesPending += value.Length;

            output[_bytesPending++] = RespConstants.CarriageReturn;
            output[_bytesPending++] = RespConstants.LineFeed;
        }

        /// <summary>
        /// Writes an Integer, ":&lt;<paramref name="value" />&gt;\r\n".
        /// </summary>
        /// <param name="value">The signed 64-bit integer to write.</param>
        public void WriteInteger(long value)
        {
            Span<byte> output;

            if (value is >= 0 and < 10)
            {
                // Most common fast-path
                if (_memory.Length - _bytesPending < 4)
                    Grow(4);

                output = _memory.Span;
                output[_bytesPending++] = RespConstants.Colon;
                output[_bytesPending++] = (byte)('0' + value);
                output[_bytesPending++] = RespConstants.CarriageReturn;
                output[_bytesPending++] = RespConstants.LineFeed;
                return;
            }

            var absValue = value < 0 ? unchecked((ulong)(-1 * value)) : (ulong)value;
            int lengthDigits = FormattingHelper.CountDigits(absValue);

            // Colon, value, Carriage Return, LineFeed
            var sizeRequired = 3 + lengthDigits + (value < 0 ? 1 : 0); // (sign)
            if (_memory.Length - _bytesPending < sizeRequired)
                Grow(sizeRequired);

            output = _memory.Span;
            output[_bytesPending++] = RespConstants.Colon;
            if (value < 0)
                output[_bytesPending++] = RespConstants.Minus;

            FormattingHelper.WriteDigits(absValue, output.Slice(_bytesPending, lengthDigits));
            _bytesPending += lengthDigits;

            output[_bytesPending++] = RespConstants.CarriageReturn;
            output[_bytesPending++] = RespConstants.LineFeed;
        }

        /// <summary>
        /// Writes an Array start, "*&lt;<paramref name="length" />&gt;\r\n".
        /// </summary>
        /// <param name="length">The length of the array to write.</param>
        /// <remarks>The caller must write <paramref name="length" /> elements or the data will be invalid.</remarks>
        public void WriteArrayStart(uint length)
        {
            // TODO Count array writes before allowing next array

            // Asterisk, length, Carriage Return, LineFeed
            var lengthDigits = length < 10 ? 1 : FormattingHelper.CountDigits((uint)length);
            var sizeRequired = 3 + lengthDigits;
            if (_memory.Length - _bytesPending < sizeRequired)
                Grow(sizeRequired);

            var output = _memory.Span;

            output[_bytesPending++] = RespConstants.Asterisk;

            if (lengthDigits == 1)
            {
                output[_bytesPending++] = (byte)('0' + length);
            }
            else
            {
                FormattingHelper.WriteDigits((uint)length, output.Slice(_bytesPending, lengthDigits));
                _bytesPending += lengthDigits;
            }

            output[_bytesPending++] = RespConstants.CarriageReturn;
            output[_bytesPending++] = RespConstants.LineFeed;
        }

        /// <summary>
        /// Writes the specified <paramref name="value" /> straight to the output with no additional formatting or validation.
        /// </summary>
        /// <param name="value">The raw bytes to write to the output.</param>
        public void WriteRaw(ReadOnlySpan<byte> value)
        {
            var sizeRequired = value.Length;
            if (_memory.Length - _bytesPending < sizeRequired)
                Grow(sizeRequired);

            var output = _memory.Span;

            value.CopyTo(output.Slice(_bytesPending));
            _bytesPending += value.Length;
        }

        private void CheckNotDisposed()
        {
            // TODO Profile to see if the order of these is optimal
            if (_stream == null)
            {
                if (_bufferWriter == null)
                {
                    throw new ObjectDisposedException(nameof(RespWriter));
                }
            }
        }

        private void Grow(int requiredSize)
        {
            Debug.Assert(requiredSize > 0);

            int growSize;
            if (_memory.Length == 0)
            {
                // First time putting data in the buffer
                growSize = _initialGrowthSize;
            }
            else
            {
                Flush();
                growSize = _growthSize;
            }

            Debug.Assert(_bytesPending == 0);
            var sizeHint = Math.Max(growSize, requiredSize);
            if (_stream != null)
            {
                Debug.Assert(_arrayBufferWriter != null);
                _memory = _arrayBufferWriter.GetMemory(sizeHint);
            }
            else
            {
                Debug.Assert(_bufferWriter != null);
                _memory = _bufferWriter.GetMemory(sizeHint);
            }

            Debug.Assert(_memory.Length >= sizeHint);
        }
    }
}

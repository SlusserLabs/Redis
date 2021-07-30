// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis.Resp
{
    /// <summary>
    /// Provides a high-performance API for forward-only, read-only access to REdis Serialization Protocol (RESP) v2 encoded data.
    /// </summary>
    public sealed partial class RespReader
    {
        private RespReaderOptions _options;

        private long _bytesConsumed;
        private RespTokenType _tokenType;
        private ReadOnlySequence<byte> _tokenSequence;
        private ReadOnlySequence<byte> _valueSequence;

        /// <summary>
        /// Initializes a new instance of the <see cref="RespReader" /> class.
        /// </summary>
        /// <param name="options">Defines custom behavior of the reader.</param>
        public RespReader(RespReaderOptions options = default)
        {
            _options = options;
        }

        /// <summary>
        /// Gets the <see cref="RespReaderOptions" /> used to create this <see cref="RespReader" /> instance.
        /// </summary>
        public RespReaderOptions Options => _options;

        /// <summary>
        /// Gets the total number of bytes consumed by this instance of <see cref="RespReader" />.
        /// </summary>
        /// <remarks>Calling <see cref="Reset" /> will zero this value.</remarks>
        public long BytesConsumed => _bytesConsumed;

        /// <summary>
        /// Gets the type of the last processed RESP token.
        /// </summary>
        public RespTokenType TokenType => _tokenType;

        /// <summary>
        /// Gets the entire byte sequence of the processed <see cref="TokenType" />, including any control bytes.
        /// </summary>
        public ReadOnlySequence<byte> TokenSequence => _tokenSequence;

        /// <summary>
        /// Gets the byte sequence of the processed <see cref="TokenType" />, excluding any control bytes.
        /// </summary>
        public ReadOnlySequence<byte> ValueSequence => _valueSequence;

        /// <summary>
        /// Resets the internal state of this instance so it can be reused.
        /// </summary>
        public void Reset()
        {
            _bytesConsumed = 0;

            _tokenType = RespTokenType.None;
            _tokenSequence = ReadOnlySequence<byte>.Empty;
            _valueSequence = ReadOnlySequence<byte>.Empty;
        }

        /// <summary>
        /// Tries to read the next RESP token from the sequence.
        /// </summary>
        /// <param name="sequenceReader">The byte sequence to read from.</param>
        /// <returns><c>true</c> if a token was read successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="RespException">The byte sequence contains invalid RESP data.</exception>
        /// <remarks>
        /// A return value of <c>false</c> indicates there was not enough data in the sequence and the caller should
        /// append more data and try again.
        /// </remarks>
        public bool TryRead(ref SequenceReader<byte> sequenceReader)
        {
            // To keep the code simple, we make a deliberate trade-off and only Advance on complete token boundaries.
            // Allowing the parser to pause at any position, mid-token, requires maintaining a LOT of state and introduces
            // complexity in resuming from that state. Choosing to parse only entire tokens may mean we reprocess bytes we've
            // already seen if we didn't have enough data to complete the token boundary in the last pass, however, those
            // cases should be rare. Most of the time we're going to have enough data for an entire token.

            // Reset any previous state
            _tokenType = RespTokenType.None;
            _tokenSequence = ReadOnlySequence<byte>.Empty;
            _valueSequence = ReadOnlySequence<byte>.Empty;

            if (!sequenceReader.End)
            {
                // Read the control byte
                var firstByte = sequenceReader.CurrentSpan[sequenceReader.CurrentSpanIndex];
                switch (firstByte)
                {
                    case RespConstants.Plus:
                        return TryReadSimpleString(ref sequenceReader);
                    case RespConstants.Dollar:
                        return TryReadBulkStringLength(ref sequenceReader);
                    case RespConstants.Minus:
                        return TryReadError(ref sequenceReader);
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to read a RESP Bulk String of the specified <paramref name="length" /> from the sequence.
        /// </summary>
        /// <param name="sequenceReader">The byte sequence to read from.</param>
        /// <param name="length">The expected length of the Bulk String.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length" /> is less than zero.</exception>
        /// <exception cref="RespException">The byte sequence contains invalid RESP data.</exception>
        /// <returns><c>true</c> if a token was read successfully; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// The <paramref name="length" /> must be obtained from a previous call to <see cref="TryRead" /> which
        /// yields a <see cref="RespTokenType.RespTokenType" />.
        /// </para>
        /// <para>
        /// A return value of <c>false</c> indicates there was not enough data in the sequence and the caller should
        /// append more data and try again.
        /// </para>
        /// </remarks>
        public bool TryReadBulkString(ref SequenceReader<byte> sequenceReader, long length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Bulk String length cannot be negative.");
            if (!_options.SkipValidation && length > RespConstants.MaxBulkStringLength)
                throw new ArgumentOutOfRangeException(nameof(length), "Bulk String length cannot exceed 512 MB.");

            var tokenLength = length + 2;
            if (sequenceReader.TrySliceTo(out ReadOnlySequence<byte> tokenSeqeunce, tokenLength))
            {
                // Ensure terminating <CR><LF>
                if (!_options.SkipValidation)
                {
                    var crPosition = tokenSeqeunce.GetPosition(length);
                    var lfPosition = tokenSeqeunce.GetPosition(1, crPosition);

                    if (tokenSeqeunce.GetValueAt(crPosition) != RespConstants.CarriageReturn || tokenSeqeunce.GetValueAt(lfPosition) != RespConstants.LineFeed)
                        throw new RespException("Bulk String was not properly terminated.", length);
                }

                sequenceReader.Advance(tokenLength);

                _tokenType = RespTokenType.BulkString;
                _tokenSequence = tokenSeqeunce;
                _valueSequence = tokenSeqeunce.Slice(0, length);

                return true;
            }

            return false;
        }

        private bool TryReadSimpleString(ref SequenceReader<byte> sequenceReader)
        {
            Debug.Assert(sequenceReader.CurrentSpan[sequenceReader.CurrentSpanIndex] == RespConstants.Plus);

            if (sequenceReader.TryReadTo(out ReadOnlySequence<byte> tokenSequence, RespConstants.CarriageReturnLineFeed))
            {
                _tokenType = RespTokenType.SimpleString;
                _tokenSequence = sequenceReader.GetConsumedSequence();
                _valueSequence = tokenSequence.Slice(1); // Trim '+'

                return true;
            }

            return false;
        }

        private bool TryReadInteger(ref SequenceReader<byte> sequenceReader)
        {
            Debug.Assert(sequenceReader.CurrentSpan[sequenceReader.CurrentSpanIndex] == RespConstants.Colon);

            if (sequenceReader.TryReadTo(out ReadOnlySequence<byte> tokenSequence, RespConstants.CarriageReturnLineFeed))
            {
                _tokenType = RespTokenType.Integer;
                _tokenSequence = sequenceReader.GetConsumedSequence();
                _valueSequence = tokenSequence.Slice(1); // Trim ':'

                return true;
            }

            if (!_options.SkipValidation && sequenceReader.Remaining > (RespConstants.MinInt64DStringLength + RespConstants.CrLfStringLength))
            {
                // We're not going to find a valid integer if we haven't yet; it would overflow
                throw new RespException("Token exceeded the max possible length of a signed 64-bit integer.", 1);
            }

            return false;
        }

        private bool TryReadError(ref SequenceReader<byte> sequenceReader)
        {
            Debug.Assert(sequenceReader.CurrentSpan[sequenceReader.CurrentSpanIndex] == RespConstants.Minus);

            if (sequenceReader.TryReadTo(out ReadOnlySequence<byte> tokenSequence, RespConstants.CarriageReturnLineFeed))
            {
                _tokenType = RespTokenType.Error;
                _tokenSequence = sequenceReader.GetConsumedSequence();
                _valueSequence = tokenSequence.Slice(1); // Trim '-'

                return true;
            }

            return false;
        }

        private bool TryReadBulkStringLength(ref SequenceReader<byte> sequenceReader)
        {
            Debug.Assert(sequenceReader.CurrentSpan[sequenceReader.CurrentSpanIndex] == RespConstants.Dollar);

            if (sequenceReader.TryReadTo(out ReadOnlySequence<byte> tokenSequence, RespConstants.CarriageReturnLineFeed))
            {
                _tokenType = RespTokenType.BulkStringPrefixedLength;
                _tokenSequence = sequenceReader.GetConsumedSequence();
                _valueSequence = tokenSequence.Slice(1); // Trim '$'

                return true;
            }

            return false;
        }
    }
}

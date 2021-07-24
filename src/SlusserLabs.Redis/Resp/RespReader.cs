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
        /// Tries to read the next RESP token from the input.
        /// </summary>
        /// <param name="sequenceReader">The byte sequence to read from.</param>
        /// <returns><c>true</c> if a token was read successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="RespException">The byte sequence contains invalid RESP data.</exception>
        /// <remarks>
        /// Unless an exception is thrown, the caller should assume the sequence did not contain enough data to read
        /// an entire token and should try again by pass a longer sequence of bytes.
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
                    case RespConstants.Dollar:
                        return TryReadBulkStringLength(ref sequenceReader);
                }
            }

            return false;
        }

        private bool TryReadBulkStringLength(ref SequenceReader<byte> reader)
        {
            Debug.Assert(reader.CurrentSpan[reader.CurrentSpanIndex] == RespConstants.Dollar);

            if (reader.TryReadTo(out ReadOnlySequence<byte> tokenSequence, RespConstants.CarriageReturnLineFeed))
            {
                _tokenType = RespTokenType.BulkStringPrefixedLength;
                _tokenSequence = reader.GetConsumedSequence();
                _valueSequence = tokenSequence.Slice(1); // Trim control byte

                return true;
            }

            return false;
        }
    }
}

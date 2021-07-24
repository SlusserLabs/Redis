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

        private RespReaderState _state;

        private int _valueStart;
        private int _tokenLength;
        private bool _tokenComplete;
        private RespTokenType _tokenType;

        private int? _bulkStringLength;

        private ReadOnlyMemorySequenceSegment<byte>? _startSegment;
        private ReadOnlyMemorySequenceSegment<byte>? _currentSegment;

        /// <summary>
        /// Initializes a new instance of the <see cref="RespReader" /> class.
        /// </summary>
        /// <param name="options">Defines custom behavior of the reader.</param>
        public RespReader(RespReaderOptions options = default)
        {
            _options = options;
        }

        private enum RespReaderState
        {
            None,

            BulkStringDollar,
            BulkStringLengthNegativeSign,
            BulkStringLengthNegativeOne,
            BulkStringLengthFirstNumeric,
            BulkStringLengthNumeric,
            BulkStringLengthCarriageReturn,
            BulkStringLengthLineFeed,
            BulkStringValue,
            BulkStringValueCarriageReturn,
            BulkStringValueLineFeed
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
        /// Gets the number of bytes in the current token.
        /// </summary>
        public int TokenLength
        {
            get
            {
                if (_tokenComplete == false)
                    return 0;

                return _tokenLength;
            }
        }

        /// <summary>
        /// Gets the type of the last processed RESP token.
        /// </summary>
        public RespTokenType TokenType
        {
            get
            {
                if (_tokenComplete == false)
                    return RespTokenType.None;

                return _tokenType;
            }
        }

        /// <summary>
        /// Gets the entire byte sequence of the processed <see cref="TokenType" />, including any control bytes.
        /// </summary>
        public ReadOnlySequence<byte> TokenSequence
        {
            get
            {
                if (_tokenComplete == false)
                    return ReadOnlySequence<byte>.Empty;

                return new ReadOnlySequence<byte>(_startSegment!, 0, _currentSegment!, _tokenLength);
            }
        }

        /// <summary>
        /// Gets the byte sequence of the processed <see cref="TokenType" />, excluding any control bytes.
        /// </summary>
        public ReadOnlySequence<byte> ValueSequence
        {
            get
            {
                if (_tokenComplete == false)
                    return ReadOnlySequence<byte>.Empty;

                // Same as token sequence without control bytes
                return new ReadOnlySequence<byte>(_startSegment!, _valueStart, _currentSegment!, _tokenLength - _valueStart - 2);
            }
        }

        /// <summary>
        /// Resets the internal state of this instance so it can be reused.
        /// </summary>
        public void Reset()
        {
            //_bytesConsumed = 0;

            //_valueStart = 0;
            //_tokenLength = 0;
            //_tokenComplete = false;
            //_tokenType = RespTokenType.None;
            //_lexemeType = RespLexemeType.None;

            //_startSegment = null;
            //_currentSegment = null;
        }

        /// <summary>
        /// Reads the next RESP token from the input.
        /// </summary>
        /// <param name="input">The byte data to read from.</param>
        /// <param name="isFinalBlock">Indicates whether <paramref name="input" /> is the last of the input to process.</param>
        /// <returns><c>true</c> if a token was read successfully; otherwise, <c>false</c>.</returns>
        public bool Read(ReadOnlyMemory<byte> input, bool isFinalBlock = false)
        {
            var result = false;
            if (input.Length > 0)
            {
                // Starting from a completed token or first run?
                if (_tokenType != RespTokenType.None || _state == RespReaderState.None)
                {
                    _startSegment = new ReadOnlyMemorySequenceSegment<byte>(input);
                    _currentSegment = _startSegment;
                }
                else
                {
                    _currentSegment = _currentSegment!.Append(input);
                }

                var pos = 0;
                var buffer = input.Span;

                switch (_state)
                {
                    case RespReaderState.None:
                        result = ConsumeControlByte(buffer, ref pos);
                        break;

                    case RespReaderState.BulkStringDollar:
                    case RespReaderState.BulkStringLengthNegativeSign:
                    case RespReaderState.BulkStringLengthNegativeOne:
                    case RespReaderState.BulkStringLengthFirstNumeric:
                    case RespReaderState.BulkStringLengthNumeric:
                    case RespReaderState.BulkStringLengthCarriageReturn:
                    case RespReaderState.BulkStringLengthLineFeed:
                    case RespReaderState.BulkStringValue:
                    case RespReaderState.BulkStringValueCarriageReturn:
                    case RespReaderState.BulkStringValueLineFeed:
                        result = ConsumeBulkString(buffer, ref pos);
                        break;

                    default:
                        throw new InvalidOperationException(); // Not a valid state
                }

                // Update running totals
                _bytesConsumed += pos;
                _tokenLength += pos;
            }

            return result;
        }

        private bool ConsumeControlByte(ReadOnlySpan<byte> buffer, ref int pos)
        {
            Debug.Assert(pos == 0);
            Debug.Assert(buffer.Length > 0);
            Debug.Assert(_state == RespReaderState.None);
            Debug.Assert(_tokenType == RespTokenType.None);

            while (pos < buffer.Length)
            {
                var b = buffer[pos];
                switch (b)
                {
                    case RespConstants.Dollar:
                        _state = RespReaderState.BulkStringDollar;
                        pos++;
                        return ConsumeBulkString(buffer, ref pos);

                    default:
                        throw new RespException(); // TODO Not a control byte
                }
            }

            return false;
        }
    }
}

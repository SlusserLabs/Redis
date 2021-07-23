// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis.Resp
{
    /// <summary>
    /// Provides a high-performance API for forward-only, read-only access to REdis Serialization Protocol (RESP) v2 encoded data.
    /// </summary>
    public sealed class RespReader
    {
        private RespReaderOptions _options;

        private long _bytesConsumed;

        private int _valueStart;
        private int _tokenLength;
        private bool _tokenComplete;
        private RespTokenType _tokenType;
        private RespLexemeType _lexemeType;

        private ReadOnlyMemorySequenceSegment<byte>? _firstSegment;
        private ReadOnlyMemorySequenceSegment<byte>? _currentSegment;

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

                return new ReadOnlySequence<byte>(_firstSegment!, 0, _currentSegment!, _tokenLength);
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
                return new ReadOnlySequence<byte>(_firstSegment!, 1, _currentSegment!, _tokenLength - 2);
            }
        }

        /// <summary>
        /// Resets the internal state of this instance so it can be reused.
        /// </summary>
        public void Reset()
        {
            _bytesConsumed = 0;

            _tokenLength = 0;
            _tokenComplete = false;
            _tokenType = RespTokenType.None;
            _lexemeType = RespLexemeType.None;

            _firstSegment = null;
            _currentSegment = null;
        }

        /// <summary>
        /// Reads the next RESP token from the input.
        /// </summary>
        /// <param name="input">The byte data to read from.</param>
        /// <param name="isFinalBlock">Indicates whether <paramref name="input" /> is the last of the input to process.</param>
        /// <returns><c>true</c> if a token was read successfully; otherwise, <c>false</c>.</returns>
        public bool Read(ReadOnlyMemory<byte> input, bool isFinalBlock = false)
        {
            if (input.Length == 0)
                goto DONE;

            if (_tokenComplete || _tokenType == RespTokenType.None)
            {
                // Setup for a new (or first) token
                _tokenLength = 0;
                _tokenComplete = false;
                _tokenType = RespTokenType.None;
                _lexemeType = RespLexemeType.None;

                _firstSegment = new ReadOnlyMemorySequenceSegment<byte>(input);
                _currentSegment = _firstSegment;
            }
            else
            {
                // Continue processing the current token
                _currentSegment = _currentSegment!.Append(input);
            }

            var pos = 0;
            var span = input.Span;

            switch (_tokenType)
            {
                case RespTokenType.SimpleString:
                    ConsumeSimpleString(span, ref pos);
                    break;

                default:
                    ConsumeControlByte(span, ref pos);
                    break;
            }

        DONE:
            return _tokenComplete;
        }

        private bool ConsumeControlByte(ReadOnlySpan<byte> span, ref int pos)
        {
            while (pos < span.Length)
            {
                var b = span[pos];
                switch (b)
                {
                    case RespConstants.Plus:
                        _lexemeType = RespLexemeType.SimpleString;
                        _tokenType = RespTokenType.SimpleString;
                        _bytesConsumed++;
                        _valueStart++;
                        _tokenLength++;
                        pos++;
                        return ConsumeSimpleString(span, ref pos);

                    default:
                        throw new RespException(); // TODO Not a control byte
                }
            }

            return false;
        }

        private bool ConsumeSimpleString(ReadOnlySpan<byte> span, ref int pos)
        {
            while (pos < span.Length)
            {
                var b = span[pos];
                switch (_lexemeType)
                {
                    case RespLexemeType.SimpleStringValue:
                        // Look for CR (or illegal characters)
                        switch (b)
                        {
                            case RespConstants.CarriageReturn:
                                _lexemeType = RespLexemeType.CarriageReturn;
                                _bytesConsumed++;
                                _tokenLength++;
                                pos++;
                                break;

                            default:
                                _bytesConsumed++;
                                _tokenLength++;
                                pos++;
                                break;
                        }

                        break;

                    case RespLexemeType.CarriageReturn:
                        // Should only be followed by a LF
                        switch (b)
                        {
                            case RespConstants.LineFeed:
                                _lexemeType = RespLexemeType.LineFeed;
                                _bytesConsumed++;
                                _tokenLength++;
                                return true;
                        }

                        break;
                }
            }

            return false;
        }
    }
}

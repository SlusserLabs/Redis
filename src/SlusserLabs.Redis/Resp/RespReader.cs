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

        private ReadOnlyMemorySequenceSegment<byte>? _firstSegment;
        private ReadOnlyMemorySequenceSegment<byte>? _currentSegment;

        private long _bytesConsumed;
        private int _tokenEndPosition;
        private RespTokenType _tokenType;
        private RespLexemeType _lexemeType;
        private bool _tokenComplete;

        private ReadOnlySequence<byte> _tokenSequence;
        private ReadOnlySequence<byte> _valueSequence;

        /// <summary>
        /// Initializes a new instance of the <see cref="RespReader" /> class.
        /// </summary>
        /// <param name="options">Defines custom behavior of the reader.</param>
        public RespReader(RespReaderOptions options = default)
        {
            _options = options;

            _valueSequence = ReadOnlySequence<byte>.Empty;
            _tokenSequence = ReadOnlySequence<byte>.Empty;
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
        public RespTokenType TokenType => _tokenComplete ? _tokenType : RespTokenType.None;

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
            ResetImpl(true);
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

            if (_tokenComplete)
            {
                // Setup for a new token
                _tokenEndPosition = 0;
                _tokenType = RespTokenType.None;
                _lexemeType = RespLexemeType.None;

                _valueSequence = ReadOnlySequence<byte>.Empty;
                _tokenSequence = ReadOnlySequence<byte>.Empty;
            }

            _firstSegment = new ReadOnlyMemorySequenceSegment<byte>(input);
            _currentSegment = _firstSegment;

            var pos = 0;
            var span = input.Span;
            while (pos < span.Length)
            {
                var b = span[pos];

            REPROCESS:
                switch (_lexemeType)
                {
                    case RespLexemeType.None:
                        // The start of a new lexeme/token should always be a control byte
                        switch (b)
                        {
                            case RespConstants.Plus:
                                _lexemeType = RespLexemeType.SimpleString;
                                _tokenType = RespTokenType.SimpleString;
                                _bytesConsumed++;
                                _tokenEndPosition++;
                                pos++;
                                break;
                        }

                        break;

                    case RespLexemeType.SimpleString:
                        _lexemeType = RespLexemeType.SimpleStringValue;
                        goto REPROCESS;

                    case RespLexemeType.SimpleStringValue:
                        // Look for CR (or illegal characters)
                        switch (b)
                        {
                            case RespConstants.CarriageReturn:
                                _lexemeType = RespLexemeType.CarriageReturn;
                                _bytesConsumed++;
                                _tokenEndPosition++;
                                pos++;
                                break;

                            default:
                                _bytesConsumed++;
                                _tokenEndPosition++;
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
                                _tokenEndPosition++;
                                _tokenComplete = true;
                                goto DONE;
                        }

                        break;
                }
            }

        DONE:
            if (isFinalBlock)
            {
                // Ensure we've reached a terminal state
            }

            if (_tokenComplete)
            {
                _tokenSequence = new ReadOnlySequence<byte>(_firstSegment!, 0, _currentSegment!, _tokenEndPosition);
                _valueSequence = new ReadOnlySequence<byte>(_firstSegment!, 1, _currentSegment!, _tokenEndPosition - 2);
            }

            return _tokenComplete;
        }

        private void ResetImpl(bool includeBytesConsumed)
        {
            //_bytesConsumed = 0;
            //_tokenStartIndex = 0;
            //_valueStartIndex = 0;
            //_runningValueLength = 0;

            //_lexemeType = RespLexemeType.None;
            //_tokenType = RespTokenType.None;

            //_firstSegment = null;
            //_currentSegment = null;

            //_valueSequence = ReadOnlySequence<byte>.Empty;
            //_tokenSequence = ReadOnlySequence<byte>.Empty;
        }
    }
}

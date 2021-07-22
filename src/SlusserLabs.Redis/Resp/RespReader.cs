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

        private MemorySegment? _firstSegment;
        private MemorySegment? _currentSegment;
        private RespLexemeType _lexemeType;
        private RespTokenType _tokenType;

        private int _bytesConsumed; // Within the memory
        private int _tokenStartIndex; // Where the token begins within the memory
        private int _valueStartIndex; // Where the token value begins (excluding control byte) within the memory
        private int _runningValueLength; // The token value length (excluding control bytes)

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
        /// Gets the type of the last processed RESP token.
        /// </summary>
        public RespTokenType TokenType => _tokenType;

        /// <summary>
        /// Gets the entire byte sequence of the processed <see cref="TokenType" />, including any control bytes.
        /// </summary>
        public ReadOnlySequence<byte> TokenSequence
        {
            get
            {
                return new ReadOnlySequence<byte>(_firstSegment!, _tokenStartIndex, _currentSegment!, _bytesConsumed);
            }
        }

        /// <summary>
        /// Gets the byte sequence of the processed <see cref="TokenType" />, excluding any control bytes.
        /// </summary>
        public ReadOnlySequence<byte> ValueSequence
        {
            get
            {
                return new ReadOnlySequence<byte>(_firstSegment!, _valueStartIndex, _currentSegment!, _runningValueLength + _valueStartIndex);
            }
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
            if (input.Length == 0)
                return result;

            // Did we return a complete token in the last operation? Reset state for the next run
            if (_tokenType == RespTokenType.None)
            {
                // Start a new memory chain
                _firstSegment = new MemorySegment(input);
                _currentSegment = _firstSegment;
                _lexemeType = RespLexemeType.None;
                _bytesConsumed = 0;
                _valueStartIndex = 0;
                _tokenStartIndex = 0;
                _runningValueLength = 0;
            }
            else
            {
                // Append memory to existing chain
                _currentSegment = _currentSegment!.Link(new MemorySegment(input));
            }

            var span = input.Span;
            var pos = 0;

            while (pos < input.Length)
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
                                _lexemeType = RespLexemeType.SimpleStringStart;
                                _tokenType = RespTokenType.SimpleString;
                                _tokenStartIndex = _bytesConsumed;
                                _bytesConsumed++;
                                pos++;
                                break;
                            default:
                                throw new InvalidOperationException("Unrecognized RESP control byte.");
                        }
                        break;

                    case RespLexemeType.SimpleStringStart:
                        // The first byte of a Simple String value
                        _lexemeType = RespLexemeType.SimpleString;
                        _valueStartIndex = _bytesConsumed;
                        goto REPROCESS;

                    case RespLexemeType.SimpleString:
                        // Look for the end of the Simple String
                        switch (b)
                        {
                            case RespConstants.CarriageReturn:
                                _lexemeType = RespLexemeType.CarriageReturn;
                                _bytesConsumed++;
                                pos++;
                                break;

                            case (byte)'\n':
                                // TODO Throw because invalid char in Simple String
                                break;

                            default:
                                _bytesConsumed++;
                                _runningValueLength++;
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
                                result = true;
                                goto DONE;

                            default:
                                // TODO Throw
                                break;
                        }
                        break;

                    //case RespLexemeType.LineFeed:
                    //    _lexemeType = RespLexemeType.None;
                    //    result = true;
                    //    goto DONE;
                }
            }

        DONE:
            if (isFinalBlock)
            {
                // Ensure we've reached a terminal state
            }

            return result;
        }
    }
}

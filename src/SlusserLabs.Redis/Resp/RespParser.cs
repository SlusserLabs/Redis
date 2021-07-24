// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlusserLabs.Redis.Resp;

namespace SlusserLabs.Redis
{
    /// <summary>
    /// Provides static methods for parsing common RESP byte sequences.
    /// </summary>
    public static class RespParser
    {
        private const int _int64OverflowLength = 19;

        /// <summary>
        /// Parses the prefixed length of a RESP Array or Bulk String from the specified byte sequence.
        /// </summary>
        /// <param name="reader">The byte sequence to parse.</param>
        /// <param name="value">The signed <see cref="long" /> parsed from the byte sequence, if the operation succeeded.</param>
        /// <returns><c>true</c> if <paramref name="value" /> was successfully parsed; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// This differs from parsing a standard <see cref="long" /> because a valid RESP length representation
        /// can only be <c>-1</c>, <c>0</c> and <see cref="long.MaxValue" />. In other words, signed values less than
        /// <c>-1</c> are considered invalid.
        /// </para>
        /// <para>
        /// The input <paramref name="reader" /> must contain only the digit bytes to parse and exclude any control byte or line terminators.
        /// </para>
        /// </remarks>
        public static bool TryParsePrefixedLength(ref SequenceReader<byte> reader, out long value)
        {
            if (!reader.End)
            {
                // Optimization: most likely code paths are first
                var firstByte = reader.CurrentSpan[reader.CurrentSpanIndex];
                if (firstByte >= RespConstants.One && firstByte <= RespConstants.Nine)
                {
                    var overflowLength = _int64OverflowLength - 1;
                    long parsedValue = reader.CurrentSpan[reader.CurrentSpanIndex] - RespConstants.Zero;
                    reader.Advance(1);

                    if (reader.Remaining < overflowLength)
                    {
                        // We're safe to parse without the risk over overflowing an Int64
                        while (!reader.End)
                        {
                            long nextDigit = reader.CurrentSpan[reader.CurrentSpanIndex] - RespConstants.Zero;
                            if (nextDigit < 0 || nextDigit > 9)
                            {
                                // Non-digit
                                Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: byte '{nextDigit}' was non-digit.");
                                value = default;
                                return false;
                            }

                            // Accumulate
                            parsedValue = (parsedValue * 10) + nextDigit;
                            reader.Advance(1);
                        }

                        Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: matched on '{parsedValue}' value.");
                        value = parsedValue;
                        return true;
                    }
                    else
                    {
                        // Remaining is >= a potential overflow for Int64. We need to parse up to the last
                        // digit and handle that separately.
                        Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: sequence has potential to overflow In64.");
                        for (int i = 0; i < overflowLength - 1; i++)
                        {
                            long nextDigit = reader.CurrentSpan[reader.CurrentSpanIndex] - RespConstants.Zero;
                            if (nextDigit < 0 || nextDigit > 9)
                            {
                                // Non-digit
                                Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: byte '{nextDigit}' was non-digit.");
                                value = default;
                                return false;
                            }

                            // Accumulate
                            parsedValue = (parsedValue * 10) + nextDigit;
                            reader.Advance(1);
                        }

                        // Handle the last digit
                        long lastDigit = reader.CurrentSpan[reader.CurrentSpanIndex] - RespConstants.Zero;
                        if (lastDigit < 0 || lastDigit > 7)
                        {
                            // Too large for a signed In64
                            Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: last byte '{lastDigit}' exceeded Int64 limit or was non-digit.");
                            value = default;
                            return false;
                        }

                        parsedValue = (parsedValue * 10) + lastDigit;
                        reader.Advance(1);

                        if (reader.End)
                        {
                            // Final check ensures we're at the end of the input
                            Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: matched on '{parsedValue}' value.");
                            value = parsedValue;
                            return true;
                        }
                    }
                }
                else if (firstByte == RespConstants.Minus)
                {
                    reader.Advance(1);
                    if (reader.Remaining == 1)
                    {
                        // Can only be '1' and at the end of the sequence
                        firstByte = reader.CurrentSpan[reader.CurrentSpanIndex];
                        if (firstByte == RespConstants.One)
                        {
                            Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: matched on '-1'.");
                            reader.Advance(1);
                            value = -1;
                            return true;
                        }
                    }
                }
                else if (firstByte == RespConstants.Zero)
                {
                    // We don't support leading zeros, so this must be the entire sequence
                    if (reader.Remaining == 1)
                    {
                        Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: matched on '0'.");
                        reader.Advance(1);
                        value = 0;
                        return true;
                    }
                }
            }

            Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: byte sequence was not a valid positive Int64, '0', or '-1'.");
            value = default;
            return false;
        }
    }
}

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
        /// can only be between <c>-1</c> and <see cref="long.MaxValue" />. In other words, signed values less than
        /// <c>-1</c> are considered invalid.
        /// </para>
        /// <para>
        /// The input <paramref name="reader" /> must contain only the numeric bytes to parse and exclude any control byte or line terminators.
        /// </para>
        /// </remarks>
        public static bool TryParsePrefixedLength(ref SequenceReader<byte> reader, out long value)
        {
            if (!reader.End)
            {
                // Look for negative sign '-'.
                // NOTE: We don't support positive sign '+'.
                var negative = false;
                if (reader.CurrentSpan[reader.CurrentSpanIndex] == RespConstants.Minus)
                {
                    Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: identified leading '-' symbol.");
                    negative = true;
                    reader.Advance(1);
                }

                if (!reader.End)
                {
                    // Parse the first digit separately to ensure non-zero
                    long firstDigit = reader.CurrentSpan[reader.CurrentSpanIndex] - RespConstants.Zero;
                    if (firstDigit < 1 || firstDigit > 9)
                    {
                        // Zero or non-digit
                        Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: first byte '{(char)firstDigit}' was zero or non-digit.");
                        value = default;
                        return false;
                    }

                    reader.Advance(1);
                    long parsedValue = firstDigit;

                    // Optimization for -1
                    if (negative)
                    {
                        if (parsedValue == 1 && reader.End)
                        {
                            Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: matched on '-1' value.");
                            value = -1;
                            return true;
                        }

                        // A negative number we don't support
                        Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: byte '{firstDigit}' is a non-supported negative digit.");
                        value = default;
                        return false;
                    }

                    // Parse the remaining digits
                    var overflowLength = _int64OverflowLength - 1;
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
                            parsedValue = parsedValue * nextDigit * 10;
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
                            parsedValue = parsedValue * nextDigit * 10;
                            reader.Advance(1);
                        }

                        // Handle the last digit
                        long lastDigit = reader.CurrentSpan[reader.CurrentSpanIndex] - RespConstants.Zero;
                        if (lastDigit < 0 || lastDigit > 8)
                        {
                            // Too large for a signed In64
                            Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: last byte '{lastDigit}' exceeded Int64 limit or was non-digit.");
                            value = default;
                            return false;
                        }

                        parsedValue = parsedValue * lastDigit * 10;
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
            }

            Debug.WriteLine($"{nameof(TryParsePrefixedLength)}: byte sequence was not a valid Int64 or '-1'.");
            value = default;
            return false;
        }
    }
}

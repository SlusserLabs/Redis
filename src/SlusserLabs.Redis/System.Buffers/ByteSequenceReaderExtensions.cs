// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Buffers
{
    /// <summary>
    /// Extension methods for <see cref="SequenceReader{T}" /> instances when working with bytes.
    /// </summary>
    public static class ByteSequenceReaderExtensions
    {
        /// <summary>
        /// Gets the consumed portion of the <see cref="SequenceReader{T}" />.
        /// </summary>
        /// <param name="reader">The byte <see cref="SequenceReader{T}" /> instance.</param>
        /// <returns>A <see cref="ReadOnlySequence{T}" /> of bytes consumed by the <paramref name="reader" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySequence<byte> GetConsumedSequence(this in SequenceReader<byte> reader)
        {
            return reader.Sequence.Slice(0, reader.Consumed);
        }

        /// <summary>
        /// Tries to slice from the current position up to <paramref name="count" /> bytes without advancing the reader.
        /// </summary>
        /// <param name="reader">The byte <see cref="SequenceReader{T}" /> instance.</param>
        /// <param name="span">The slice requested, if the operation succeeded.</param>
        /// <param name="count">The length of the slice.</param>
        /// <returns><c>true</c> if a token was read successfully; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TrySliceTo(this in SequenceReader<byte> reader, out ReadOnlySequence<byte> span, long count)
        {
            if (reader.Remaining >= count)
            {
                span = reader.Sequence.Slice(0, count);
                return true;
            }

            span = default;
            return false;
        }
    }
}

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
    }
}

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Buffers
{
    /// <summary>
    /// Extension methods for <see cref="ReadOnlySequence{T}" /> instances when working with bytes.
    /// </summary>
    public static class ByteReadOnlySequenceExtensions
    {
        /// <summary>
        /// Gets the byte at the specified <paramref name="position" />.
        /// </summary>
        /// <param name="sequence">The byte <see cref="ReadOnlySequence{T}" /> instance.</param>
        /// <param name="position">The position to get the value at.</param>
        /// <returns>The byte at the specified position.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetValueAt(this in ReadOnlySequence<byte> sequence, SequencePosition position)
        {
            return sequence.Slice(position).FirstSpan[0];
        }
    }
}

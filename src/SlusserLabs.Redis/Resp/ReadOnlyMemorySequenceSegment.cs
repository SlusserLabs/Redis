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
    internal sealed class ReadOnlyMemorySequenceSegment<T> : ReadOnlySequenceSegment<T>
    {
        public ReadOnlyMemorySequenceSegment(ReadOnlyMemory<T> segment)
        {
            Memory = segment;
        }

        public ReadOnlyMemorySequenceSegment<T> Append(ReadOnlyMemory<T> segment)
        {
            var nextSegment = new ReadOnlyMemorySequenceSegment<T>(segment);

            // The sum of node lengths *before* the current node
            nextSegment.RunningIndex = RunningIndex += Memory.Length;

            Next = nextSegment;
            return nextSegment;
        }
    }
}

// References:
// https://www.stevejgordon.co.uk/creating-a-readonlysequence-from-array-data-in-dotnet

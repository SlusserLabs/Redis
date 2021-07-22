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
    internal class MemorySegment : ReadOnlySequenceSegment<byte>
    {
        public MemorySegment(ReadOnlyMemory<byte> first)
        {
            Memory = first;
        }

        public MemorySegment Link(MemorySegment next)
        {
            Debug.Assert(next != null);

            // The sum of node lengths *before* the current node.
            next.RunningIndex = RunningIndex += Memory.Length;

            Next = next;
            return next;
        }
    }
}

// References:
// https://www.stevejgordon.co.uk/creating-a-readonlysequence-from-array-data-in-dotnet

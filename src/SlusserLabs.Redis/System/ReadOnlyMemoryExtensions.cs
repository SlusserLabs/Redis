// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    internal static class ReadOnlyMemoryExtensions
    {
        public static ArraySegment<byte> AsArray(this ReadOnlyMemory<byte> memory)
        {
            if (!MemoryMarshal.TryGetArray(memory, out var array))
            {
                throw new InvalidOperationException($"The {nameof(ReadOnlyMemory<byte>)} is not backed by an array.");
            }

            return array;
        }
    }
}

// TODO Cite reference
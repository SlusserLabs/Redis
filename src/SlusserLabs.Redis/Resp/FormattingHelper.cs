// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis.Resp
{
    internal static class FormattingHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDigits(ulong value)
        {
            int digits = 1;
            uint part;
            if (value >= 10000000)
            {
                if (value >= 100000000000000)
                {
                    part = (uint)(value / 100000000000000);
                    digits += 14;
                }
                else
                {
                    part = (uint)(value / 10000000);
                    digits += 7;
                }
            }
            else
            {
                part = (uint)value;
            }

            if (part < 10)
            {
                // no-op
            }
            else if (part < 100)
            {
                digits++;
            }
            else if (part < 1000)
            {
                digits += 2;
            }
            else if (part < 10000)
            {
                digits += 3;
            }
            else if (part < 100000)
            {
                digits += 4;
            }
            else if (part < 1000000)
            {
                digits += 5;
            }
            else
            {
                Debug.Assert(part < 10000000);
                digits += 6;
            }

            return digits;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountDecimalDigits(long value)
        {
            int digits = 1;
            if (value >= 100000)
            {
                value /= 100000;
                digits += 5;
            }

            if (value < 10)
            {
                // no-op
            }
            else if (value < 100)
            {
                digits++;
            }
            else if (value < 1000)
            {
                digits += 2;
            }
            else if (value < 10000)
            {
                digits += 3;
            }
            else
            {
                Debug.Assert(value < 100000);
                digits += 4;
            }

            return digits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDigits(long value, int length, Span<byte> buffer)
        {
            // https://github.com/dotnet/runtime/blob/v5.0.8/src/libraries/System.Private.CoreLib/src/System/Buffers/Text/Utf8Formatter/FormattingHelpers.cs

            Debug.Assert(value >= 0);
            Debug.Assert(length > 0);
            Debug.Assert(buffer.Length >= length);

            for (int i = length - 1; i >= 1; i--)
            {
                long temp = '0' + value;
                value /= 10;
                buffer[i] = (byte)(temp - (value * 10));
            }

            Debug.Assert(value < 10);
            buffer[0] = (byte)('0' + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDigits(ulong value, Span<byte> buffer)
        {
            // We can mutate the 'value' parameter since it's a copy-by-value local.
            // It'll be used to represent the value left over after each division by 10.

            for (int i = buffer.Length - 1; i >= 1; i--)
            {
                ulong temp = '0' + value;
                value /= 10;
                buffer[i] = (byte)(temp - (value * 10));
            }

            Debug.Assert(value < 10);
            buffer[0] = (byte)('0' + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDigits(uint value, Span<byte> buffer)
        {
            Debug.Assert(buffer.Length >= CountDecimalDigits(value));

            // We can mutate the 'value' parameter since it's a copy-by-value local.
            // It'll be used to represent the value left over after each division by 10.

            for (int i = buffer.Length - 1; i >= 1; i--)
            {
                uint temp = '0' + value;
                value /= 10;
                buffer[i] = (byte)(temp - (value * 10));
            }

            Debug.Assert(value < 10);
            buffer[0] = (byte)('0' + value);
        }
    }
}

// References:
// https://github.com/dotnet/runtime/blob/v5.0.8/src/libraries/System.Private.CoreLib/src/System/Buffers/Text/FormattingHelpers.CountDigits.cs
// https://github.com/dotnet/runtime/blob/v5.0.8/src/libraries/System.Private.CoreLib/src/System/Buffers/Text/Utf8Formatter/FormattingHelpers.cs

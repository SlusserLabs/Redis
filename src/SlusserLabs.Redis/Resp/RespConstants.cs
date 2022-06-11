// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1203 // Constants should appear before fields
#pragma warning disable SA1516 // Elements should be separated by blank line

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis.Resp
{
    internal static class RespConstants
    {
        public const byte Dollar = (byte)'$';
        public const byte Minus = (byte)'-';
        public const byte Plus = (byte)'+';
        public const byte Colon = (byte)':';
        public const byte Asterisk = (byte)'*';
        public const byte Zero = (byte)'0';
        public const byte One = (byte)'1';
        public const byte Nine = (byte)'9';
        public const byte CarriageReturn = (byte)'\r';
        public const byte LineFeed = (byte)'\n';

        // 512 MBs comes from the Redis documentation https://redis.io/topics/protocol#resp-bulk-strings
        public const int MaxBulkStringLength = 512 * 1024 * 1024;

        public const string MaxInt64DString = "9223372036854775807";
        public const string MinIn64DString = "-9223372036854775808";
        public static readonly int MaxInt64DStringLength = MaxInt64DString.Length;
        public static readonly int MinInt64DStringLength = MinIn64DString.Length;

        public static ReadOnlySpan<byte> CarriageReturnLineFeed => new byte[] { (byte)'\r', (byte)'\n' };

        public static ReadOnlySpan<byte> NullArray => new byte[]
        {
            (byte)'*',
            (byte)'-',
            (byte)'1',
            (byte)'\r',
            (byte)'\n'
        };

        public static ReadOnlySpan<byte> EmptyArray => new byte[]
        {
            (byte)'*',
            (byte)'0',
            (byte)'\r',
            (byte)'\n'
        };

        public static ReadOnlySpan<byte> NullBulkString => new byte[]
        {
            (byte)'$',
            (byte)'-',
            (byte)'1',
            (byte)'\r',
            (byte)'\n'
        };

        public static ReadOnlySpan<byte> EmptyBulkString => new byte[]
        {
            (byte)'$',
            (byte)'0',
            (byte)'\r',
            (byte)'\n',
            (byte)'\r',
            (byte)'\n'
        };

        public static ReadOnlySpan<byte> HelloBulkString => new byte[]
        {
            (byte)'$',
            (byte)'5',
            (byte)'\r',
            (byte)'\n',
            (byte)'H',
            (byte)'E',
            (byte)'L',
            (byte)'L',
            (byte)'O',
            (byte)'\r',
            (byte)'\n'
        };

        public static ReadOnlySpan<byte> AuthBulkString => new byte[]
        {
            (byte)'$',
            (byte)'4',
            (byte)'\r',
            (byte)'\n',
            (byte)'A',
            (byte)'U',
            (byte)'T',
            (byte)'H',
            (byte)'\r',
            (byte)'\n'
        };

        public static ReadOnlySpan<byte> SetNameBulkString => new byte[]
        {
            (byte)'$',
            (byte)'7',
            (byte)'\r',
            (byte)'\n',
            (byte)'S',
            (byte)'E',
            (byte)'T',
            (byte)'N',
            (byte)'A',
            (byte)'M',
            (byte)'E',
            (byte)'\r',
            (byte)'\n'
        };
    }
}

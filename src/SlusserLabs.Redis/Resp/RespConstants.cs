// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

        public static ReadOnlySpan<byte> CarriageReturnLineFeed => new byte[] { (byte)'\r', (byte)'\n' };

        public static ReadOnlySpan<byte> NullBulkString => new byte[] { (byte)'$', (byte)'-', (byte)'1', (byte)'\r', (byte)'\n' };
    }
}

﻿// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis.Resp
{
    internal enum RespLexemeType : byte
    {
        None,
        CarriageReturn,
        LineFeed,
        SimpleString,
        SimpleStringValue,
        Error,
        ErrorValue,
        BulkString,
        BulkStringLength,
        BulkStringValue
    }
}

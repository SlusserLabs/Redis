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
    partial class RespReader
    {
        private bool ConsumeBulkString(ReadOnlySpan<byte> buffer, ref int pos)
        {
            //if (_bulkStringLength != null)
            //    return ConsumeBulkStringValue(buffer, ref pos);

            return ConsumeBulkStringLength(buffer, ref pos);
        }

        private bool ConsumeBulkStringLength(ReadOnlySpan<byte> buffer, ref int pos)
        {
            while (pos < buffer.Length)
            {
                var b = buffer[pos];
                switch (_state)
                {
                    case RespReaderState.BulkStringDollar:
                        // Can only be '-' or '1'-'9'
                        if (b == RespConstants.Minus)
                        {
                            _state = RespReaderState.BulkStringLengthNegativeSign;
                            _valueStart = _tokenLength + pos;
                            pos++;
                        }
                        else if (b >= RespConstants.One && b <= RespConstants.Nine)
                        {
                            _state = RespReaderState.BulkStringLengthNumeric;
                            _valueStart = _tokenLength + pos;
                            pos++;
                        }
                        else
                        {
                            throw new RespException(); // Invalid byte
                        }

                        break;

                    case RespReaderState.BulkStringLengthNegativeSign:
                        // Can only be '1'
                        if (b == RespConstants.One)
                        {
                            _state = RespReaderState.BulkStringLengthNegativeOne;
                            pos++;
                        }
                        else
                        {
                            throw new RespException(); // Invalid byte
                        }

                        break;

                    case RespReaderState.BulkStringLengthNegativeOne:
                        // Can only be '\r'
                        if (b == RespConstants.CarriageReturn)
                        {
                            _state = RespReaderState.BulkStringLengthCarriageReturn;
                            pos++;
                        }
                        else
                        {
                            throw new RespException(); // Invalid byte
                        }

                        break;

                    case RespReaderState.BulkStringLengthCarriageReturn:
                        // Can only be '\n'
                        if (b == RespConstants.LineFeed)
                        {
                            // Parse the bulk string length
                            // var valueSpan = new ReadOnlySequence<byte>(_startSegment!, _valueStart, _currentSegment!, _tokenLength + pos - 2).ToArray();



                            _state = RespReaderState.BulkStringLengthLineFeed;
                            pos++;
                        }
                        else
                        {
                            throw new RespException(); // Invalid byte
                        }

                        break;
                }
            }

            return false;
        }
    }
}

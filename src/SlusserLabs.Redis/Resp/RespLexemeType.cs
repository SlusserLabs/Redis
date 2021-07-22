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
        SimpleStringStart,
        SimpleString

    }
}

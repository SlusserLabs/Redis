using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis.Resp
{
    internal enum RespVersion : sbyte
    {
        Unknown = -1,
        Resp2 = 2,
        Resp3 = 3
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis.Resp
{
    /// <summary>
    /// Defines the various RESP tokens that make up a RESP data.
    /// </summary>
    public enum RespTokenType : byte
    {
        /// <summary>
        /// There is no value (as distinct from <see cref="NullBulkString" /> or <see cref="NullArray" />).
        /// </summary>
        None,

        /// <summary>
        /// The token is a RESP Simple String.
        /// </summary>
        SimpleString
    }
}

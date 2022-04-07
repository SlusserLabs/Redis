using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis
{
    /// <summary>
    /// Extension methods for common Redis commands.
    /// </summary>
    public static class RedisClientExtensions
    {
        public static async ValueTask HSet(ReadOnlyMemory<byte> key)
        {
            // Ensure we have a connection
            // Write the command and flush

            return;
        }
    }
}

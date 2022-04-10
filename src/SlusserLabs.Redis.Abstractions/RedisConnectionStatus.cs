using System;
using System.Collections.Generic;
using System.Text;

namespace SlusserLabs.Redis
{
    /// <summary>
    /// Defines the status of an <see cref="IRedisConnection" />.
    /// </summary>
    public enum RedisConnectionStatus
    {
        /// <summary>
        /// A connection with the Redis server has not yet been established.
        /// </summary>
        New,

        /// <summary>
        /// A connection with the Redis server has been established and it is ready to service commands.
        /// </summary>
        Ready,

        /// <summary>
        /// The connection is currently in the process of sending a request or receiving a response.
        /// </summary>
        Busy,

        /// <summary>
        /// The connection has experienced an error and needs to be disposed of.
        /// </summary>
        Failed
    }
}

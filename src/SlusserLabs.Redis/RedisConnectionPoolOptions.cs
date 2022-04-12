// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis
{
    /// <summary>
    /// Options for configuring an <see cref="IRedisConnectionPool" />.
    /// All connections in the pool share the same options and behavior.
    /// </summary>
    public class RedisConnectionPoolOptions
    {
        /// <summary>
        /// The default <see cref="MaxPoolSize" />.
        /// </summary>
        public const int DefaultMaxPoolSize = 20;

        /// <summary>
        /// Gets or sets a name-value string used to set the Redis endpoint and configure the <see cref="RedisConnectionPoolOptions" />.
        /// </summary>
        /// <remarks>
        /// Values that are explicitly specified via their property on the <see cref="RedisConnectionPoolOptions" /> will take precedence
        /// over those same values specified in the <see cref="ConnectionString" />.
        /// </remarks>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the endpoint of the Redis server to connect to.
        /// </summary>
        public EndPoint? EndPoint { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of <see cref="IRedisConnection" /> instances to keep in the pool.
        /// The default is <c>20</c>.
        /// </summary>
        /// <see cref="DefaultMaxPoolSize" />
        public int? MaxPoolSize { get; set; }

        /// <summary>
        /// Gets or sets whether to enable Nagle's algorithm for all connections.
        /// The default is <c>true</c>.
        /// </summary>
        public bool NoDelay { get; set; } = true;
    }
}

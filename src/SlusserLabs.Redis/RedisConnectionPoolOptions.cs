// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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

        internal string? EndPoint { get; set; }

        /// <summary>
        /// Gets or sets the connection string used to connect to a Redis server.
        /// </summary>
        /// <remarks>
        /// Values that are explicitly specified via their property on the <see cref="RedisConnectionPoolOptions" /> will take precedence
        /// over those same values specified in the <see cref="ConnectionString" />.
        /// </remarks>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of <see cref="IRedisConnection" /> instances to keep in the pool.
        /// The default is <c>20</c>.
        /// </summary>
        /// <see cref="DefaultMaxPoolSize" />
        public int? MaxPoolSize { get; set; }
    }
}

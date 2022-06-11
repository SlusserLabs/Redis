// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Options = Microsoft.Extensions.Options.Options;

namespace SlusserLabs.Redis
{
    /// <summary>
    /// Extension methods for <see cref="IRedisConnectionPool" /> instances.
    /// </summary>
    public static class RedisConnectionPoolExtensions
    {
        /// <summary>
        /// Returns an <see cref="IRedisConnection" /> from the default configuration pool.
        /// </summary>
        /// <param name="pool">An <see cref="IRedisConnectionPool" /> instance.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An <see cref="IRedisConnection" /> from the pool.</returns>
        public static ValueTask<IRedisConnection> RentAsync(this IRedisConnectionPool pool, CancellationToken cancellationToken)
        {
            return pool.RentAsync(Options.DefaultName, Timeout.InfiniteTimeSpan, cancellationToken);
        }

        /// <summary>
        /// Returns an <see cref="IRedisConnection" /> from the default configuration pool.
        /// </summary>
        /// <param name="pool">An <see cref="IRedisConnectionPool" /> instance.</param>
        /// <param name="timeout">The amount of time to wait for a connection to become available from the pool.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An <see cref="IRedisConnection" /> from the pool.</returns>
        public static ValueTask<IRedisConnection> RentAsync(this IRedisConnectionPool pool, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return pool.RentAsync(Options.DefaultName, timeout, cancellationToken);
        }
    }
}

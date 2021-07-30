// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SlusserLabs.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IRedisClientBuilder" />.
    /// </summary>
    public static class RedisClientBuilderExtensions
    {
        /// <summary>
        /// Sets the length of time that a <see cref="RedisConnection" /> instance can be reused. Each named
        /// client can have its own configured connection lifetime value. The default value is two minutes. Set the lifetime
        /// to <see cref="Timeout.InfiniteTimeSpan" /> to disable connection expiry.
        /// </summary>
        /// <param name="builder">An <see cref="IRedisClientBuilder" /> instance.</param>
        /// <param name="connectionLifetime">The connection lifetime.</param>
        /// <returns>The configured <see cref="IRedisClientBuilder" /> instance.</returns>
        /// <remarks>
        /// <para>
        /// The default implementation of <see cref="IRedisClientFactory" /> will pool the <see cref="RedisConnection" />
        /// instances created by the factory to reduce resource consumption. This setting configures the amount of time a
        /// connection can be pooled before it is scheduled for removal from the pool and disposal.
        /// </para>
        /// <para>
        /// Pooling of connections is desirable as each connection typically manages its own underlying TCP connections; creating
        /// more connections than necessary can result in connection delays. Some connections also keep connections open indefinitely
        /// which can prevent the connection from reacting to DNS changes. The <paramref name="connectionLifetime" /> value should
        /// be chosen with an understanding of the application's requirement to respond to changes in the network environment.
        /// </para>
        /// <para>
        /// Expiry of a connection will not immediately dispose of the connection. An expired connection is placed in a separate pool
        /// which is processed at intervals to dispose connections only when they become unreachable. Using long-lived <see cref="RedisClient" />
        /// instances will prevent the underlying <see cref="RedisConnection" /> from being disposed until all reference are
        /// garbage-collected.
        /// </para>
        /// </remarks>
        public static IRedisClientBuilder SetConnectionLifetime(this IRedisClientBuilder builder, TimeSpan connectionLifetime)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (connectionLifetime != Timeout.InfiniteTimeSpan && connectionLifetime < RedisClientFactoryOptions.MinConnectionLifetime)
            {
                throw new ArgumentOutOfRangeException(nameof(connectionLifetime), $"The connection lifetime must be a least {RedisClientFactoryOptions.MinConnectionLifetime} second(s).");
            }

            builder.Services.Configure<RedisClientFactoryOptions>(builder.Name, options => options.ConnectionLifetime = connectionLifetime);
            return builder;
        }
    }
}

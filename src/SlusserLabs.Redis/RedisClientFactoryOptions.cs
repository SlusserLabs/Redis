// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlusserLabs.Redis
{
    /// <summary>
    /// Options for configuring an <see cref="IRedisClientFactory" />.
    /// </summary>
    public class RedisClientFactoryOptions
    {
        internal static readonly TimeSpan MinConnectionLifetime = TimeSpan.FromSeconds(1);

        // Remember to keep these consistent with documentation
        private TimeSpan _connectionLifetime = TimeSpan.FromMinutes(2);
        private int _minPoolSize;
        private int _maxPoolSize = 20;

        /// <summary>
        /// Gets or sets the length of time that a <see cref="RedisConnection" /> instance can be reused. Each named
        /// client can have its own configured connection lifetime value. The default value is two minutes. Set the lifetime
        /// to <see cref="Timeout.InfiniteTimeSpan" /> to disable connection expiry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default implementation of <see cref="IRedisClientFactory" /> will pool the <see cref="RedisConnection" />
        /// instances created by the factory to reduce resource consumption. This setting configures the amount of time a
        /// connection can be pooled before it is scheduled for removal from the pool and disposal.
        /// </para>
        /// <para>
        /// Pooling of connections is desirable as each connection typically manages its own underlying TCP connections; creating
        /// more connections than necessary can result in connection delays. Some connections also keep connections open indefinitely
        /// which can prevent the connection from reacting to DNS changes. The <paramref name="value" /> should be chosen
        /// with an understanding of the application's requirement to respond to changes in the network environment.
        /// </para>
        /// <para>
        /// Expiry of a connection will not immediately dispose of the connection. An expired connection is placed in a separate pool
        /// which is processed at intervals to dispose connections only when they become unreachable. Using long-lived <see cref="RedisClient" />
        /// instances will prevent the underlying <see cref="RedisConnection" /> from being disposed until all reference are
        /// garbage-collected.
        /// </para>
        /// </remarks>
        public TimeSpan ConnectionLifetime
        {
            get => _connectionLifetime;
            set
            {
                if (value != Timeout.InfiniteTimeSpan && value < MinConnectionLifetime)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"The connection lifetime must be a least {MinConnectionLifetime} second(s).");
                }

                _connectionLifetime = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum number of <see cref="RedisConnection" /> instances to keep in the pool.
        /// The default is <c>0</c> (zero).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Connections are added to the pool on demand up to the maximum specified. When a <see cref="RedisClient" /> is created,
        /// the <see cref="RedisConnection" /> it uses is obtained from the pool if a usable connection is available.
        /// </para>
        /// </remarks>
        public int MinPoolSize
        {
            get => _minPoolSize;
            set
            {
                if (value < 0 || value > _maxPoolSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The minimum pool size cannot be less than 0 (zero) or greater than the maximum pool size.");
                }

                _minPoolSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of <see cref="RedisConnection" /> instances to keep in the pool.
        /// The default is <c>20</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Connections are added to the pool on demand up to the maximum specified. When a <see cref="RedisClient" /> is created,
        /// the <see cref="RedisConnection" /> it uses is obtained from the pool if a usable connection is available.
        /// </para>
        /// <para>
        /// If the maximum pool size has been reached and no usable connection is available, the request is queued.
        /// </para>
        /// </remarks>
        public int MaxPoolSize
        {
            get => _maxPoolSize;
            set
            {
                if (value < 1 || value < _minPoolSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "The maximum pool size cannot be less than 1 or less than the minimum pool size.");
                }

                _maxPoolSize = value;
            }
        }
    }
}

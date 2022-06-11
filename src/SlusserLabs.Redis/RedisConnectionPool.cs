// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SlusserLabs.Redis
{
    internal sealed class RedisConnectionPool : IRedisConnectionPool
    {
        private readonly IServiceProvider _services;
        private readonly IOptionsMonitor<RedisConnectionPoolOptions> _options;

        // Using Lazy ensures that only one instance is created per name
        private readonly ConcurrentDictionary<string, Lazy<RedisConnectionPoolImpl>> _namedPools;
        private readonly Func<string, Lazy<RedisConnectionPoolImpl>> _poolFactory;

        public RedisConnectionPool(IServiceProvider services, IOptionsMonitor<RedisConnectionPoolOptions> options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _services = services;
            _options = options;

            // Configuration names are case-sensitive
            _namedPools = new ConcurrentDictionary<string, Lazy<RedisConnectionPoolImpl>>(StringComparer.Ordinal);
            _poolFactory = (name) =>
            {
                return new Lazy<RedisConnectionPoolImpl>(() =>
                {
                    return CreateImpl(name);
                }, LazyThreadSafetyMode.ExecutionAndPublication);
            };
        }

        public ValueTask<IRedisConnection> RentAsync(string name, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var pool = _namedPools.GetOrAdd(name, _poolFactory).Value;
            return pool.RentAsync(timeout, cancellationToken);
        }

        private RedisConnectionPoolImpl CreateImpl(string name)
        {
            var options = _options.Get(name);
            if (options.EndPoint == null)
            {
                // Our Options validation logic ensures we always have a Redis endpoint, so this
                // can only happen when the user has requested a named configuration that doesn't exist....
                throw new InvalidOperationException(
                    $"A Redis connection pool does not exist for the configuration name '{name}'. " +
                    $"Are you missing a call to {nameof(RedisConnectionPoolServiceCollectionExtensions.AddRedisConnectionPool)} for this named configuration?");
            }

            options.Immutable = true; // Freeze the options
            var impl = new RedisConnectionPoolImpl(name, options);

            return impl;
        }

        private sealed class RedisConnectionPoolImpl
        {
            private readonly SemaphoreSlim _semaphore;
            private readonly ConcurrentQueue<RedisConnection> _idleConnections;

            private readonly string _name;
            private readonly RedisConnectionPoolOptions _options;

            public RedisConnectionPoolImpl(string name, RedisConnectionPoolOptions options)
            {
                Debug.Assert(name != null);
                Debug.Assert(options != null && !string.IsNullOrEmpty(options.ConnectionString));

                _name = name;
                _options = options;
                _semaphore = new SemaphoreSlim((int)options.MaxPoolSize!, (int)options.MaxPoolSize!);
                _idleConnections = new ConcurrentQueue<RedisConnection>();
            }

            public ValueTask<IRedisConnection> RentAsync(TimeSpan timeout, CancellationToken cancellationToken)
            {
                var waitTask = _semaphore.WaitAsync(timeout, cancellationToken);
                if (waitTask.IsCompletedSuccessfully && waitTask.Result)
                {
                    // Lease acquired; process synchronously
                    var connection = GetOrCreateIdleConnection();
                    return new ValueTask<IRedisConnection>(connection);
                }

                return RentSlowPathAsync(waitTask, timeout);
            }

            private async ValueTask<IRedisConnection> RentSlowPathAsync(Task<bool> waitTask, TimeSpan timeout)
            {
                var leaseAcquired = await waitTask.ConfigureAwait(false);
                if (!leaseAcquired)
                {
                    throw new TimeoutException($"No available connections in the pool. Waited for '{timeout}'.");
                }

                var connection = GetOrCreateIdleConnection();
                return connection;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private RedisConnection GetOrCreateIdleConnection()
            {
                if (_idleConnections.TryDequeue(out var connection))
                {
                    return connection;
                }

                return new RedisConnection(Guid.NewGuid().ToString(), _options);
            }
        }
    }
}

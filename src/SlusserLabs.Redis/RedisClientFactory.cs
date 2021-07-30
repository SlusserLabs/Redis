// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable SA1116 // Split parameters should start on line after declaration

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace SlusserLabs.Redis
{
    internal sealed class RedisClientFactory : IRedisClientFactory
    {
        private readonly ConcurrentDictionary<string, Lazy<RedisConnectionPool>> _pools;
        private readonly Func<string, Lazy<RedisConnectionPool>> _poolFactory;

        public RedisClientFactory(IOptionsMonitor<RedisClientFactoryOptions> optionsMonitor)
        {
            // Configuration names are case-insensitive
            _pools = new ConcurrentDictionary<string, Lazy<RedisConnectionPool>>(StringComparer.Ordinal);
            _poolFactory = (name) =>
            {
                return new Lazy<RedisConnectionPool>(() =>
                {
                    return new RedisConnectionPool(default);
                }, LazyThreadSafetyMode.ExecutionAndPublication);
            };
        }

        public RedisClient CreateClient(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var pool = _pools.GetOrAdd(name, _poolFactory).Value;
            var connection = pool.Rent();

            var client = new RedisClient(connection, disposeConnection: false);

            //if (name == null)
            //{
            //    throw new ArgumentNullException(nameof(name));
            //}

            //ActiveHandlerTrackingEntry entry = _activeHandlers.GetOrAdd(name, _entryFactory).Value;

            //StartHandlerEntryTimer(entry);

            //return entry.Handler;

            return default!;
        }
    }
}

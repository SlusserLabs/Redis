// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlusserLabs.Redis
{
    internal sealed class PooledRedisClientFactory : IRedisClientFactory
    {
        // One pool using "name:id" keys instead of a pool of pools.
        // Lazy is used to guard against duplicate creation.
        private readonly ConcurrentDictionary<string, Lazy<ActiveRedisConnectionEntry>> _activeConnections;
        private readonly Func<string, Lazy<ActiveRedisConnectionEntry>> _entryFactory;

        public PooledRedisClientFactory()
        {
            // // Names are not case-sensitive
            _activeConnections = new ConcurrentDictionary<string, Lazy<ActiveRedisConnectionEntry>>(StringComparer.Ordinal);
            //_entryFactory = (name) =>
            //{
            //    return new Lazy<ActiveRedisConnectionEntry>(() =>
            //    {
            //        return CreateHandlerEntry(name);
            //    }, LazyThreadSafetyMode.ExecutionAndPublication);
            //};
        }

        public RedisClient CreateClient(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return default!;
        }

        // An entry in the active connections pool
        private class ActiveRedisConnectionEntry
        {
        }
    }
}

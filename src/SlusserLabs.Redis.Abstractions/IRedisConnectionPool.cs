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
    /// A pool of <see cref="IRedisConnection" /> instances grouped and configured by name.
    /// </summary>
    public interface IRedisConnectionPool
    {
        /// <summary>
        /// Returns an <see cref="IRedisConnection" /> from the named configuration pool.
        /// </summary>
        /// <param name="name">The pool configuration name.</param>
        /// <param name="timeout">The amount of time to wait for a connection to become available from the pool.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An <see cref="IRedisConnection" /> from the pool.</returns>
        ValueTask<IRedisConnection> RentAsync(string name, TimeSpan timeout, CancellationToken cancellationToken);
    }
}

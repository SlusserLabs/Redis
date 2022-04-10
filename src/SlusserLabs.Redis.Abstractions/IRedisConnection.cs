// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlusserLabs.Redis
{
    /// <summary>
    /// A TCP connection to a Redis server.
    /// </summary>
    /// <remarks>
    /// The connection must be returned to the pool by calling <see cref="IDisposable.Dispose" />
    /// or memory leaks will occur.
    /// </remarks>
    public interface IRedisConnection : IDisposable
    {
        /// <summary>
        /// Gets the status of this connection.
        /// </summary>
        RedisConnectionStatus Status { get; }

        /// <summary>
        /// Gets the local endpoint for this connection.
        /// </summary>
        /// <remarks>
        /// While still in the <see cref="RedisConnectionStatus.New" /> status, the return value is <c>null</c>.
        /// While in the <see cref="RedisConnectionStatus.Failed" /> status, the return value is undefined.
        /// </remarks>
        EndPoint? LocalEndPoint { get; }

        /// <summary>
        /// Gets the remote endpoint for this connection.
        /// </summary>
        /// <remarks>
        /// While still in the <see cref="RedisConnectionStatus.New" /> status, the return value is <c>null</c>.
        /// While in the <see cref="RedisConnectionStatus.Failed" /> status, the return value is undefined.
        /// </remarks>
        EndPoint? RemoteEndPoint { get; }

        Task TestAsync(CancellationToken cancellationToken);
    }
}

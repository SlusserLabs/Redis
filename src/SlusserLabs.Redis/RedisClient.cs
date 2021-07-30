// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlusserLabs.Redis
{
    /// <summary>
    /// Provides a client for sending requests and receiving responses from a Redis server.
    /// </summary>
    public class RedisClient : IDisposable
    {
        private readonly RedisConnection _connection;
        private readonly bool _disposeConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisClient" /> class with the specified
        /// <paramref name="connection" /> and whether the connection should be closed when this
        /// instance is disposed.
        /// </summary>
        /// <param name="connection">The <see cref="RedisConnection" /> responsible for processing the RESP messages.</param>
        /// <param name="disposeConnection">
        /// <c>true</c> if the inner connection should be disposed by a call to <see cref="Dispose"/>;
        /// otherwise, <c>false</c> to allow the connection to reused.
        /// </param>
        public RedisClient(RedisConnection connection, bool disposeConnection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            _connection = connection;
            _disposeConnection = disposeConnection;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

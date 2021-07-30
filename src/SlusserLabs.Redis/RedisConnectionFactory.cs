// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SlusserLabs.Redis
{
    internal sealed class RedisConnectionFactory : IConnectionFactory
    {
        private readonly RedisClientFactoryOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConnectionFactory" /> class.
        /// </summary>
        /// <param name="options">The options accessor for accessing <see cref="RedisClientFactoryOptions" />.</param>
        /// <param name="loggerFactory">A logger factory for logging diagnostic information.</param>
        public RedisConnectionFactory(IOptions<RedisClientFactoryOptions> options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _options = options.Value;

            var logger = loggerFactory.CreateLogger("SlusserLabs.Redis.RedisClient");
        }

        public ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}

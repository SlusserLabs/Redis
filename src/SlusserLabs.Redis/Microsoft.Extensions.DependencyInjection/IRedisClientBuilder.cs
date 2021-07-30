// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SlusserLabs.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// A builder for configuring named <see cref="RedisClient" /> instance returned by an <see cref="IRedisClientFactory" />.
    /// </summary>
    public interface IRedisClientBuilder
    {
        /// <summary>
        /// Gets the name of the client configured by this builder.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the application services collection.
        /// </summary>
        IServiceCollection Services { get; }
    }
}

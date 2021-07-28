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
    /// A factory that can create <see cref="RedisClient" /> instances with custom configuration for a given logical name.
    /// </summary>
    public interface IRedisClientFactory
    {
        /// <summary>
        /// Creates an configures a <see cref="RedisClient" /> instance using the configuration that corresponds to the logical
        /// <paramref name="name" /> specified.
        /// </summary>
        /// <param name="name">The logical name of the client to create.</param>
        /// <returns>A new <see cref="RedisClient" /> instance.</returns>
        RedisClient CreateClient(string name);
    }
}

// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace SlusserLabs.Redis
{
    /// <summary>
    /// Options for configuring a <see cref="RedisClient" />.
    /// </summary>
    public class RedisClientOptions
    {
        /// <summary>
        /// Gets or sets the connection string used to connect to a Redis server.
        /// </summary>
        public string? Configuration { get; set; }
    }
}

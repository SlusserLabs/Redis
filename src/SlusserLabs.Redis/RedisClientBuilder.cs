// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace SlusserLabs.Redis
{
    /// <summary>
    /// A builder for configuring named <see cref="RedisClient" /> instances.
    /// </summary>
    public class RedisClientBuilder : IRedisClientBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisClientBuilder" /> class.
        /// </summary>
        /// <param name="services">The application services collection.</param>
        /// <param name="name">The logical name of the client being configured.</param>
        public RedisClientBuilder(IServiceCollection services, string name)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Services = services;
            Name = name;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}

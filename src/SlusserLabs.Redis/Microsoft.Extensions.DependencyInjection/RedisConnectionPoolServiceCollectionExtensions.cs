// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SlusserLabs.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    using Options = Microsoft.Extensions.Options.Options;

    /// <summary>
    /// Extension methods to configure an <see cref="IServiceCollection" /> with an <see cref="IRedisConnectionPool" />.
    /// </summary>
    public static class RedisConnectionPoolServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a default <see cref="IRedisConnectionPool" /> and related services to an <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to configure.</param>
        /// <param name="configureOptions">A callback for configuring <see cref="RedisConnectionPoolOptions" />.</param>
        /// <returns>The configured <see cref="IServiceCollection" />.</returns>
        public static IServiceCollection AddRedisConnectionPool(this IServiceCollection services, Action<RedisConnectionPoolOptions> configureOptions)
        {
            return AddRedisConnectionPool(services, Options.DefaultName, configureOptions);
        }

        /// <summary>
        /// Adds a named <see cref="IRedisConnectionPool" /> and related services to an <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to configure.</param>
        /// <param name="name">The configuration name of the <see cref="IRedisConnectionPool" />.</param>
        /// <param name="configureOptions">A callback for configuring <see cref="RedisConnectionPoolOptions" />.</param>
        /// <returns>The configured <see cref="IServiceCollection" />.</returns>
        public static IServiceCollection AddRedisConnectionPool(this IServiceCollection services, string name, Action<RedisConnectionPoolOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            services.AddOptions();
            services.Configure(name, configureOptions);
            services.TryAddSingleton<IValidateOptions<RedisConnectionPoolOptions>, RedisConnectionPoolOptionsValidator>();

            services.TryAddSingleton<RedisConnectionPool>();
            services.TryAddSingleton<IRedisConnectionPool>(serviceProvider => serviceProvider.GetRequiredService<RedisConnectionPool>());

            return services;
        }
    }
}

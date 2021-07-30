// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SlusserLabs.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure an <see cref="IServiceCollection"/> for <see cref="IRedisClientFactory"/>.
    /// </summary>
    public static class RedisClientFactoryServiceCollectionExtensions
    {
        private static readonly string _defaultName = Options.Options.DefaultName;

        /// <summary>
        /// Adds the <see cref="IRedisClientFactory"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
        /// <param name="configureOptions">A callback for configuring <see cref="RedisClientOptions" />.</param>
        /// <returns>An <see cref="IRedisClientBuilder" /> that can be used to configure the client.</returns>
        public static IRedisClientBuilder AddRedisClient(this IServiceCollection services, Action<RedisClientOptions> configureOptions)
        {
            var builder = AddRedisClient(services, _defaultName, configureOptions);

            // Easy injection of the default client
            services.TryAddTransient(s => s.GetRequiredService<IRedisClientFactory>().CreateClient(_defaultName));

            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IRedisClientFactory"/> and related services to the <see cref="IServiceCollection"/> for a named client.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
        /// <param name="name">The logical name of the <see cref="RedisClient" /> to configure.</param>
        /// <param name="configureOptions">A callback for configuring <see cref="RedisClientOptions" />.</param>
        /// <returns>An <see cref="IRedisClientBuilder" /> that can be used to configure the client.</returns>
        public static IRedisClientBuilder AddRedisClient(this IServiceCollection services, string name, Action<RedisClientOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            services.AddLogging();
            services.AddOptions();

            services.Configure(name, configureOptions);
            services.TryAddSingleton<RedisClientFactory>();
            services.TryAddSingleton<IRedisClientFactory>(serviceProvider => serviceProvider.GetRequiredService<RedisClientFactory>());

            return new RedisClientBuilder(services, name);
        }
    }
}

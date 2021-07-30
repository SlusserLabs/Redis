// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlusserLabs.Redis;
using SlusserLabs.Redis.Resp;

namespace ConsoleApp1
{
    class Program
    {
        static Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var host = CreateHostBuilder(args)
                .UseConsoleLifetime()
                .Build();

            using var scope = host.Services.CreateScope();
            var provider = scope.ServiceProvider;

            using var client = provider.GetRequiredService<RedisClient>();

            return host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddRedisClient(options =>
                    {
                        options.Configuration = "localhost";
                    });
                });
    }
}

// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SlusserLabs.Redis;
using SlusserLabs.Redis.Resp;

namespace ConsoleApp1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // var n = new ArrayBufferWriter<byte>();
            // var m = n.GetMemory(512 * 1024 * 1024);

            var host = CreateHostBuilder(args)
                .UseConsoleLifetime()
                .Build();

            using var scope = host.Services.CreateScope();
            var provider = scope.ServiceProvider;

            var pool = provider.GetRequiredService<IRedisConnectionPool>();
            using var connection = await pool.RentAsync(CancellationToken.None);
            await connection.TestAsync(CancellationToken.None);

            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddRedisConnectionPool(options =>
                    {
                        options.ConnectionString = "127.0.0.1:6379";
                    });
                });
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace SlusserLabs.Redis
{
    internal sealed class RedisConnectionPool
    {
        private readonly RedisConnectionFactory _factory;

        public RedisConnectionPool(RedisConnectionFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _factory = factory;
        }

        public RedisConnection Rent()
        {
            return default;
        }

        public void Return(RedisConnection connection)
        {
        }
    }
}

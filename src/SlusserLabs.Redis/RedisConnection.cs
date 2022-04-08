// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace SlusserLabs.Redis
{
    internal sealed class RedisConnection : IRedisConnection, IDisposable
    {
        private Socket _socket;

        internal RedisConnection()
        {

        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        //public Task HelloAsync()
        //{

        //}

        //private Task ConnectAsync(CancellationToken cancellationToken)
        //{

        //}
    }
}

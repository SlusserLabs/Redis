// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace SlusserLabs.Redis
{
    internal sealed class RedisConnection : IRedisConnection, IDisposable
    {
        private readonly RedisConnectionPoolOptions _options;
        private Socket? _socket;
        private State _state;

        internal RedisConnection(RedisConnectionPoolOptions options)
        {
            Debug.Assert(options != null && options.EndPoint != null);

            _options = options;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public async Task TestAsync()
        {
            if (_state == State.New)
            {
               //  _socket
            }

            // if(_sta)

            // if(_)


            //_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //await _socket.ConnectAsync(_options.EndPoint!, 6379, CancellationToken.None);
        }

        //private Task ConnectAsync(CancellationToken cancellationToken)
        //{

        //}

        private enum State
        {
            New,
            Hello,
            Doomed
        }
    }
}

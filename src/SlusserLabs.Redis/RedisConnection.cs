// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace SlusserLabs.Redis
{
    internal sealed class RedisConnection : IRedisConnection, IDisposable
    {
        private readonly RedisConnectionPoolOptions _options;

        private RedisConnectionStatus _status;

        private Socket? _socket;
        private Task? _sendingTask;
        private Task? _receivingTask;

        private List<ArraySegment<byte>>? _sendBuffers;
        private PipeReader? _sendPipe;

        internal RedisConnection(RedisConnectionPoolOptions options)
        {
            Debug.Assert(options != null && options.EndPoint != null);

            _options = options;
        }

        public RedisConnectionStatus Status => _status;
        public EndPoint? LocalEndPoint => _socket?.LocalEndPoint;
        public EndPoint? RemoteEndPoint => _socket?.RemoteEndPoint;

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public async Task TestAsync(CancellationToken cancellationToken)
        {
            if (_status == RedisConnectionStatus.Failed)
            {
                throw new InvalidOperationException("A previous exception has broken the connection and it must be disposed of.");
            }

            try
            {
                if (_status == RedisConnectionStatus.New)
                {
                    await HelloAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                _status = RedisConnectionStatus.Failed;
                throw;
            }
        }

        internal void Reset()
        {
            //if (keepAlive)
            //{
            //    Debug.Assert(_status == RedisConnectionStatus.New || _status == RedisConnectionStatus.Ready);


            //    return;
            //}


            //// Full reset, don't reuse the socket
            //_socket?.Dispose();
            //_socket = null;
            //_status = RedisConnectionStatus.New;

            //if(_socket != null)
            //{

            //}

            //if (_socket != null && !keepAlive)
            //{
            //    _

            //    return;
            //}

            //_status = RedisConnectionStatus.Ready;
        }

        private async Task DoSend()
        {
            try
            {
                while (true)
                {
                    // Get data from the send buffer
                    var result = await _sendPipe!.ReadAsync().ConfigureAwait(false);
                    if (result.IsCanceled)
                    {
                        break;
                    }

                    // Write to the socket
                    var buffers = result.Buffer;
                    if (!buffers.IsEmpty)
                    {
                        int bytesSent;
                        if (buffers.IsSingleSegment)
                        {
                            bytesSent = await _socket!.SendAsync(buffers.First, SocketFlags.None, CancellationToken.None)
                                                      .ConfigureAwait(false);
                        }
                        else
                        {
                            _sendBuffers ??= new List<ArraySegment<byte>>();
                            foreach (var b in buffers)
                            {
                                _sendBuffers.Add(b.AsArray());
                            }

                            bytesSent = await _socket!.SendAsync(_sendBuffers, SocketFlags.None)
                                                      .ConfigureAwait(false);

                            _sendBuffers.Clear();
                        }

                        _sendPipe.AdvanceTo(buffers.GetPosition(bytesSent));
                        if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                throw;
            }
            finally
            {

            }
        }

        private async Task DoReceive()
        {
        }

        private async ValueTask HelloAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_status == RedisConnectionStatus.New);
            Debug.Assert(_socket == null);

            var protocolType = ProtocolType.Tcp; // TODO Support Unix domain sockets
            _socket = new Socket(_options.EndPoint!.AddressFamily, SocketType.Stream, protocolType)
            {
                NoDelay = _options.NoDelay
            };

            // TODO Support connect timeout
            // The call to ConnectAsync will perform async DNS resolution. (TODO check if true on all versions)
            // We want to do this every time we connect and not cache it, because... uh... the cloud.
            await _socket.ConnectAsync(_options.EndPoint, cancellationToken).ConfigureAwait(false);

            // TODO TLS

            // Start the pump
            _sendingTask = DoSend();
            _receivingTask = DoReceive();


        }
    }
}

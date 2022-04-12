// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SlusserLabs.Redis.Resp;
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
        private Pipe? _sendPipe;
        private Pipe? _receivePipe;

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

                // Send the PING command
                var writer = _sendPipe!.Writer;
                var respWriter = new RespWriter(writer);
                respWriter.WriteArrayStart(1);
                respWriter.WriteBulkString(Encoding.ASCII.GetBytes("PING"));
                respWriter.Flush();
                await _sendPipe.Writer.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Receive the PONG response
                var reader = _receivePipe!.Reader;
                var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;
                Console.WriteLine(Encoding.UTF8.GetString(buffer));
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
            var reader = _sendPipe!.Reader;

            try
            {
                while (true)
                {
                    // Get data from the send buffer
                    var result = await reader.ReadAsync().ConfigureAwait(false);
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

                        reader.AdvanceTo(buffers.GetPosition(bytesSent));
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
            try
            {
                while(true)
                {
                    var buffer = _receivePipe!.Writer.GetMemory(4096);
                    var bytesReceived = await _socket!.ReceiveAsync(buffer, SocketFlags.None, CancellationToken.None).ConfigureAwait(false);
                    if (bytesReceived == 0)
                    {
                        break;
                    }

                    _receivePipe.Writer.Advance(bytesReceived);
                    var result = await _receivePipe.Writer.FlushAsync(CancellationToken.None).ConfigureAwait(false);

                    if(result.IsCompleted || result.IsCanceled)
                    {
                        break;
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

            _sendPipe = new Pipe();
            _receivePipe = new Pipe();

            // Start the pump
            _sendingTask = DoSend();
            _receivingTask = DoReceive();
        }
    }
}

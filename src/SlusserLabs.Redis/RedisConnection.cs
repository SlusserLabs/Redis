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
using SlusserLabs.Redis.Helpers;
using SlusserLabs.Redis.Resp;

namespace SlusserLabs.Redis
{
    internal sealed class RedisConnection : IRedisConnection, IDisposable
    {
        private const int _minBlockSize = 4096; // Chosen because most operating systems use 4k pages

        private readonly string _connectionId;
        private readonly RedisConnectionPoolOptions _options;

        private RedisConnectionStatus _status;

        private Socket? _socket;
        private Task? _sendingTask;
        private Task? _receivingTask;

        private List<ArraySegment<byte>>? _sendBuffers;
        private Pipe? _sendPipe;
        private Pipe? _receivePipe;

        internal RedisConnection(string connectionId, RedisConnectionPoolOptions options)
        {
            Debug.Assert(!string.IsNullOrEmpty(connectionId));
            Debug.Assert(options != null && options.EndPoint != null);

            _connectionId = connectionId;
            _options = options;
        }

        public string ConnectionId => _connectionId;
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
            // if (keepAlive)
            // {
            //     Debug.Assert(_status == RedisConnectionStatus.New || _status == RedisConnectionStatus.Ready);

            // return;
            // }

            // // Full reset, don't reuse the socket
            // _socket?.Dispose();
            // _socket = null;
            // _status = RedisConnectionStatus.New;

            // if(_socket != null)
            // {

            // }

            // if (_socket != null && !keepAlive)
            // {
            //     _

            // return;
            // }

            // _status = RedisConnectionStatus.Ready;
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
                while (true)
                {
                    if (_options.AllocateReceiveBufferOnDemand)
                    {
                        // Wait for data with an empty buffer
                        await _socket!.ReceiveAsync(Memory<byte>.Empty, SocketFlags.None, CancellationToken.None).ConfigureAwait(false);
                    }

                    // Receive data
                    var pipeWriter = _receivePipe!.Writer;
                    var buffer = pipeWriter.GetMemory(_minBlockSize);
                    var bytesReceived = await _socket!.ReceiveAsync(buffer, SocketFlags.None, CancellationToken.None).ConfigureAwait(false);
                    if (bytesReceived == 0)
                    {
                        break;
                    }

                    // Make the data available to pipe readers
                    pipeWriter.Advance(bytesReceived);
                    var result = await pipeWriter.FlushAsync(CancellationToken.None).ConfigureAwait(false);
                    if (result.IsCompleted || result.IsCanceled)
                    {
                        // Reader has shut down; stop writing
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
            await ConnectAsync(cancellationToken);

            WriteHello(_sendPipe!.Writer, _options);
            await _sendPipe.Writer.FlushAsync(cancellationToken).ConfigureAwait(false);

            // Receive the response
            var reader = _receivePipe!.Reader;
            var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = result.Buffer;
            Console.WriteLine(Encoding.UTF8.GetString(buffer));

            static void WriteHello(PipeWriter writer, RedisConnectionPoolOptions options)
            {
                // The HELLO command and RESP v3 handshake
                var respWriter = new RespWriter2(writer, new RespWriterOptions
                {
                    SkipValidation = true
                });

                if (options.RespVersion == RespVersion.Unknown)
                {
                    // Do a simple HELLO and look for possible error response indicating
                    respWriter.WriteArrayStart(2);
                    respWriter.WriteRaw(RespConstants.HelloBulkString);
                    respWriter.WriteBulkString((byte)'3');
                }

                respWriter.Flush();
            }
        }

        private async ValueTask ConnectAsync(CancellationToken cancellationToken)
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

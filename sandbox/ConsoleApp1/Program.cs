// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Buffers;
using System.Text;
using SlusserLabs.Redis.Resp;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var reader = new RespReader();
            var input = Encoding.ASCII.GetBytes("$-1\r\n");
            var sequenceReader = new SequenceReader<byte>(new ReadOnlySequence<byte>(input));

            if (sequenceReader.TryReadTo(out ReadOnlySequence<byte> tokenSequence, new byte[] { (byte)'\r', (byte)'\n' }))
            {
                Console.WriteLine("Found <CR><LF>");
                return;
            }

            Console.WriteLine("Did not find <CR><LF>");
        }
    }
}

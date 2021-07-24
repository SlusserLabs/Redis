// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
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

            var result = reader.Read(input, true);

            Console.WriteLine($"Result: {result}, TokenType: {reader.TokenType}, TokenLength: {reader.TokenLength}, BytesConsumed: {reader.BytesConsumed}");
            Console.WriteLine($"Token: {Encoding.ASCII.GetString(reader.TokenSequence)}");
            Console.WriteLine($"Value: {Encoding.ASCII.GetString(reader.ValueSequence)}");
        }
    }
}

// Copyright (c) SlusserLabs, Jacob Slusser. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace SlusserLabs.Redis.Resp.Tests
{
    [ExcludeFromCodeCoverage]
    public class RespReaderTests
    {
        [Fact]
        public void Ctor_WithDefaults_ShouldHaveInitialState()
        {
            var reader = new RespReader();

            Assert.Equal(RespTokenType.None, reader.TokenType);
            Assert.Equal(0, reader.TokenSequence.Length);
            Assert.Equal(0, reader.ValueSequence.Length);

            Assert.False(reader.Read(Memory<byte>.Empty));
        }

        [Theory]
        [InlineData("+\r\n", "")]
        [InlineData("+OK\r\n", "OK")]
        [InlineData("+Hello World!\r\n", "Hello World!")]
        public void Read_WithSimpleString_ShouldYieldValue(string input, string expected)
        {
            var memory = Encoding.ASCII.GetBytes(input).AsMemory();
            var expectedBytes = Encoding.ASCII.GetBytes(expected);
            var reader = new RespReader();

            Assert.False(reader.Read(Memory<byte>.Empty));
            Assert.True(reader.Read(memory, true));

            Assert.Equal(RespTokenType.SimpleString, reader.TokenType);
            Assert.True(reader.TokenSequence.IsSingleSegment);
            Assert.True(reader.ValueSequence.IsSingleSegment);
            Assert.Equal(memory.ToArray(), reader.TokenSequence.First.ToArray());
            Assert.Equal(expectedBytes.ToArray(), reader.ValueSequence.First.ToArray());
        }

        [Theory]
        // [InlineData("+\r\n", "")]
        [InlineData("+OK\r\n", "OK")]
        // [InlineData("+Hello World!\r\n", "Hello World!")]
        public void ReadMultiSegment_WithSimpleString_ShouldYieldValue(string input, string expected)
        {
            var sequence = new List<Memory<byte>>();
            foreach (var b in Encoding.ASCII.GetBytes(input))
                sequence.Add(new byte[] { b });

            var expectedBytes = Encoding.ASCII.GetBytes(expected);
            var reader = new RespReader();

            var i = 0;
            while (i < sequence.Count - 1)
            {
                reader.Read(sequence[i]).ShouldBe(false);
            }

            reader.Read(sequence[i], true).ShouldBe(true);

            reader.TokenType.ShouldBe(RespTokenType.SimpleString);
            //Assert.False(reader.TokenSequence.IsSingleSegment);
            //Assert.False(reader.ValueSequence.IsSingleSegment);
            //Assert.Equal(memory.ToArray(), reader.TokenSequence.First.ToArray());
            //Assert.Equal(expectedBytes.ToArray(), reader.ValueSequence.First.ToArray());
        }
    }
}

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
        public void Ctor_ShouldHaveInitialState()
        {
            var reader = new RespReader();

            reader.TokenType.ShouldBe(RespTokenType.None);
            reader.TokenSequence.Length.ShouldBe(0);
            reader.ValueSequence.Length.ShouldBe(0);
        }

        [Fact]
        public void Reset_ShouldHaveInitialState()
        {
            var reader = new RespReader();

            reader.Read(Encoding.ASCII.GetBytes("+\r\n"));
            reader.Reset();

            reader.TokenType.ShouldBe(RespTokenType.None);
            reader.TokenSequence.Length.ShouldBe(0);
            reader.ValueSequence.Length.ShouldBe(0);
        }

        [Theory]
        [InlineData("+1\r\n+2\r\n+3\r\n", "1", "2", "3")]
        public void Read_WithMultipleSimpleStrings_ShouldYieldMultipleTokens(string input, string expected1, string expected2, string expected3)
        {
            var reader = new RespReader();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var expectedBytes = new List<byte[]>
            {
                Encoding.ASCII.GetBytes(expected1),
                Encoding.ASCII.GetBytes(expected2),
                Encoding.ASCII.GetBytes(expected3)
            };

            var memory = new ReadOnlyMemory<byte>(inputBytes);
            for (int i = 0; i < 3; i++)
            {
                reader.Read(memory).ShouldBeTrue();
                reader.TokenType.ShouldBe(RespTokenType.SimpleString);
                reader.TokenSequence.Length.ShouldBe(4);
                reader.ValueSequence.Length.ShouldBe(1);
                reader.ValueSequence.ToArray().ShouldBe(expectedBytes[i]);

                memory = memory.Slice((int)reader.TokenSequence.Length);
            }

            //reader.Read(Memory<byte>.Empty, true).ShouldBeTrue();
            //reader.TokenType.ShouldBe(RespTokenType.None);
            //reader.TokenSequence.Length.ShouldBe(0);
            //reader.ValueSequence.Length.ShouldBe(0);
        }

        [Theory(Skip = "Currently causing infinite loop.")]
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

        [Theory(Skip = "Currently causing infinite loop.")]
        [InlineData("+\r\n", "")]
        [InlineData("+OK\r\n", "OK")]
        [InlineData("+Hello World!\r\n", "Hello World!")]
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
            // Assert.False(reader.TokenSequence.IsSingleSegment);
            // Assert.False(reader.ValueSequence.IsSingleSegment);
            // Assert.Equal(memory.ToArray(), reader.TokenSequence.First.ToArray());
            // Assert.Equal(expectedBytes.ToArray(), reader.ValueSequence.First.ToArray());
        }
    }
}

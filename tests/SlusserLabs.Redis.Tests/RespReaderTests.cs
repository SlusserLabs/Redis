using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SlusserLabs.Redis.Resp.Tests
{
    [ExcludeFromCodeCoverage]
    public class RespReaderTests
    {
        [Fact]
        public void Ctor_RespReader_ShouldHaveInitialState()
        {
            var reader = new RespReader();

            Assert.Equal(RespTokenType.None, reader.TokenType);
            //Assert.Equal(0, reader.TokenSequence.Length);
            //Assert.Equal(0, reader.ValueSequence.Length);
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
    }
}

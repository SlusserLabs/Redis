using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Xunit;

namespace SlusserLabs.Redis.Resp.Tests
{
    [ExcludeFromCodeCoverage]
    public class RespWriterTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_WithNulls_ThrowsArgumentNullException(bool skipValidation)
        {
            var options = new RespWriterOptions { SkipValidation = skipValidation };

            Assert.Throws<ArgumentNullException>(() => new RespWriter((Stream)null));
            Assert.Throws<ArgumentNullException>(() => new RespWriter((IBufferWriter<byte>)null));
            Assert.Throws<ArgumentNullException>(() => new RespWriter((Stream)null, options));
            Assert.Throws<ArgumentNullException>(() => new RespWriter((IBufferWriter<byte>)null, options));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_WithNonWritableStream_ThrowsArgumentException(bool skipValidation)
        {
            var options = new RespWriterOptions { SkipValidation = skipValidation };
            var stream = new MemoryStream();
            stream.Dispose();

            Assert.Throws<ArgumentException>(() => new RespWriter(stream));
            Assert.Throws<ArgumentException>(() => new RespWriter(stream, options));
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WriteBulkString_ToStream(bool skipValidation)
        {
            var expectedBytes = GetManyBulkStrings(1_000);
            var options = new RespWriterOptions { SkipValidation = skipValidation };

            using var stream = new MemoryStream();
            var writer = new RespWriter(stream, options);
            WriteManyBulkStrings(writer, 1_000);
            writer.Flush();

            Assert.Equal(0, writer.BytesPending);
            Assert.Equal(expectedBytes.Length, stream.Position);

            Assert.Equal(expectedBytes.ToArray(), stream.ToArray());
        }

        [Theory]
        [InlineData(0, ":0\r\n")]
        [InlineData(1, ":1\r\n")]
        [InlineData(-1, ":-1\r\n")]
        [InlineData(1_000, ":1000\r\n")]
        [InlineData(-1_000, ":-1000\r\n")]
        [InlineData(long.MinValue, ":-9223372036854775808\r\n")]
        [InlineData(long.MaxValue, ":9223372036854775807\r\n")]
        public void WriteInteger_ToStream(long value, string expected)
        {
            var expectedBytes = Encoding.ASCII.GetBytes(expected);
            using var stream = new MemoryStream();
            var writer = new RespWriter(stream);

            writer.WriteInteger(value);
            writer.Flush();

            Assert.Equal(0, writer.BytesPending);
            Assert.Equal(expectedBytes.Length, stream.Position);
            Assert.Equal(expectedBytes, stream.ToArray());
        }

        [Theory]
        [InlineData(0, ":0\r\n")]
        [InlineData(1, ":1\r\n")]
        [InlineData(-1, ":-1\r\n")]
        [InlineData(1_000, ":1000\r\n")]
        [InlineData(-1_000, ":-1000\r\n")]
        [InlineData(long.MinValue, ":-9223372036854775808\r\n")]
        [InlineData(long.MaxValue, ":9223372036854775807\r\n")]
        public void WriteInteger_ToBufferWriter(long value, string expected)
        {
            var expectedBytes = Encoding.ASCII.GetBytes(expected);
            var bufferWriter = new ArrayBufferWriter<byte>();
            var writer = new RespWriter(bufferWriter);

            writer.WriteInteger(value);
            writer.Flush();

            Assert.Equal(0, writer.BytesPending);
            Assert.Equal(expectedBytes.Length, bufferWriter.WrittenCount);
            Assert.Equal(expectedBytes, bufferWriter.WrittenSpan.ToArray());
        }



        //public static IEnumerable<object[]> RespEncodedBulkStrings
        //{
        //    get
        //    {
        //        return new List<object[]>
        //        {
        //            new object[] { "", "$0\r\n\r\n" },
        //            new object[] { "foobar", "$6\r\nfoobar\r\n" },
        //            new object[] { "hello, \"world\"", "\"mess\\u0022age\"" },
        //        };
        //    }
        //}

        private static void WriteManyBulkStrings(RespWriter writer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                var bytes = Encoding.UTF8.GetBytes($"some string value {i}");
                writer.WriteBulkString(bytes);
            }
        }

        private static ReadOnlySpan<byte> GetManyBulkStrings(int length)
        {
            var arrayBufferWriter = new ArrayBufferWriter<byte>();
            for (int i = 0; i < length; i++)
            {
                var str = $"some string value {i}";
                var bytes = Encoding.UTF8.GetBytes($"${str.Length}\r\n{str}\r\n");
                arrayBufferWriter.Write(bytes);
            }

            return arrayBufferWriter.WrittenSpan;
        }
    }
}

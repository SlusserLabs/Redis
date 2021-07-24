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

namespace SlusserLabs.Redis.Tests
{
    [ExcludeFromCodeCoverage]
    public class RespParserTests
    {
        [Theory]
        [InlineData("-1", -1)]
        [InlineData("0", 0)]
        [InlineData("1", 1)]
        [InlineData("123", 123)]
        [InlineData("12345", 12345)]
        [InlineData("1234567890", 1234567890)]
        [InlineData("9876543210", 9876543210)]
        [InlineData("9223372036854775807", long.MaxValue)]
        public void TryParsePrefixedLength_WithValidPrefixedLengths_Succeeds(string input, long expected)
        {
            var bytes = Encoding.ASCII.GetBytes(input);
            var sequence = new ReadOnlySequence<byte>(bytes);
            var reader = new SequenceReader<byte>(sequence);

            RespParser.TryParsePrefixedLength(ref reader, out var value).ShouldBeTrue();
            reader.Remaining.ShouldBe(0);
            reader.End.ShouldBeTrue();
            value.ShouldBe(expected);
        }

        [Theory]
        [InlineData("-", 1)]
        [InlineData("-2", 1)]
        [InlineData("00", 0)]
        [InlineData("+123", 0)]
        [InlineData("abc", 0)]
        [InlineData("123abc", 3)]
        public void TryParsePrefixedLength_WithInvalidPrefixedLengths_Fails(string input, int consumed)
        {
            var bytes = Encoding.ASCII.GetBytes(input);
            var sequence = new ReadOnlySequence<byte>(bytes);
            var reader = new SequenceReader<byte>(sequence);

            RespParser.TryParsePrefixedLength(ref reader, out var value).ShouldBeFalse();
            reader.Consumed.ShouldBe(consumed);
            value.ShouldBe(0);
        }
    }
}

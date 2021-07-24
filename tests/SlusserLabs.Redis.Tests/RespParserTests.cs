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
    }
}

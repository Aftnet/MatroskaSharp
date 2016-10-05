/* Copyright (c) 2011 Oleg Zee

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * */

using System;
using Xunit;

namespace EbmlSharp.Test
{
	/// <summary>
	/// Unit tests for VInt class
	/// </summary>
	public class VIntTests
	{
        [Theory]
		[InlineData(0, 1, 0x80)]
		[InlineData(1, 1, 0x81)]
		[InlineData(126, 1, 0xfe)]
		[InlineData(127, 2, 0x407f)]
		[InlineData(128, 2, 0x4080)]
		[InlineData(0xdeffad, 4, 0x10deffad)]
		public void EncodeSize(int value, int expectedLength, ulong expectedResult)
		{
			var v = VInt.EncodeSize((ulong)value);
			Assert.Equal(expectedLength, v.Length);

            Assert.Equal(expectedResult, v.EncodedValue);
		}

        [Theory]
        [InlineData(0, 1, 0x80)]
		[InlineData(0, 2, 0x4000)]
		[InlineData(0, 3, 0x200000)]
		[InlineData(0, 4, 0x10000000)]
		[InlineData(127, 2, 0x407f)]
		public void EncodeSizeWithLength(int value, int length, ulong expectedResult)
		{
			var v = VInt.EncodeSize((ulong)value, length);
			Assert.Equal(length, v.Length);

            Assert.Equal(expectedResult, v.EncodedValue);
        }

        [Theory]
        [InlineData(127, 1, 0x407f)]
        public void EncodeSizeWithLengthException(int value, int length, ulong expectedResult)
        {
            Assert.Throws(typeof(ArgumentException), () =>
            {
                var v = VInt.EncodeSize((ulong)value, length);
                Assert.Equal(length, v.Length);

                Assert.Equal(expectedResult, v.EncodedValue);
            });            
        }

        [Theory]
		[InlineData(1, 0xffL)]
		[InlineData(2, 0x7fffL)]
		[InlineData(3, 0x3fffffL)]
		[InlineData(4, 0x1fffffffL)]
		[InlineData(5, 0x0fffffffffL)]
		[InlineData(6, 0x07ffffffffffL)]
		[InlineData(7, 0x03ffffffffffffL)]
		[InlineData(8, 0x01ffffffffffffffL)]
		public void CreatesReserved(int length, ulong expectedResult)
		{
			var size = VInt.UnknownSize(length);

			Assert.Equal(length, size.Length);
			Assert.True(size.IsReserved);

            Assert.Equal(expectedResult, size.EncodedValue);
		}

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(9)]
        public void CreatesReservedException(int length)
        {
            Assert.Throws(typeof(ArgumentOutOfRangeException), () =>
            {
                var size = VInt.UnknownSize(length);

                Assert.Equal(length, size.Length);
                Assert.True(size.IsReserved);

                var value = size.EncodedValue;
            });
        }

        [Theory]
		[InlineData(0x80ul, 0)]
		[InlineData(0xaful, 0x2f)]	
		[InlineData(0x40FFul, 0xFF)]
		[InlineData(0x2000FFul, 0xFF)]
		[InlineData(0x100000FFul, 0xFF)]
		[InlineData(0x1f1020FFul, 0xF1020FF)]
		public void CreatesFromEncodedValue(ulong encodedValue, ulong expectedResult)
		{
            Assert.Equal(expectedResult, VInt.FromEncoded(encodedValue).Value);
		}

        [Theory]
        [InlineData(0ul)]
        [InlineData(1ul)]
        [InlineData(0x40ul)]
        [InlineData(0x20ul)]
        [InlineData(0x10ul)]
        [InlineData(0x8000ul)]
        public void CreatesFromEncodedValueException(ulong encodedValue)
        {
            Assert.Throws(typeof(ArgumentException), () => VInt.FromEncoded(encodedValue).Value);
        }

        [Theory]
		[InlineData(0ul, 1)]
		[InlineData(126ul, 1)]
		[InlineData(127ul, 2)]
		[InlineData(128ul, 2)]
		[InlineData(0xFFFFul, 3)]
		[InlineData(0xFFffFFul, 4)]
		public void CreatesSizeOrIdFromEncodedValue(ulong value, int expectedLength)
		{
			var v = VInt.EncodeSize(value);
			Assert.False(v.IsReserved);
			Assert.Equal(value, v.Value);
			Assert.Equal(expectedLength, v.Length);
		}

        [Theory]
        [InlineData(0x80ul, true)]
		[InlineData(0x81ul, true)]
		[InlineData(0x4001ul, false)]
		[InlineData(0xfful, false)]
		[InlineData(0x7ffful, false)]
		public void ValidIdentifiers(ulong encodedValue, bool expectedResult)
		{
            Assert.Equal(expectedResult, VInt.FromEncoded(encodedValue).IsValidIdentifier);
		}

	}
}
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace EbmlSharp.Test
{
    public class EbmlWriterTests
    {
        #region Setup/teardown

        private Stream _stream;
        private static readonly VInt ElementId = VInt.MakeId(123);
        private EbmlWriter _writer;

        public EbmlWriterTests()
        {
            _stream = new MemoryStream();
            _writer = new EbmlWriter(_stream);
        }

        private EbmlReader StartRead()
        {
            _stream.Position = 0;
            var reader = new EbmlReader(_stream);
            Assert.True(reader.ReadNext());
            Assert.Equal(ElementId, reader.ElementId);

            return reader;
        }
        #endregion

        [Theory]
        [InlineData(0L)]
        [InlineData(123L)]
        [InlineData(12345678L)]
        [InlineData(-1L)]
        [InlineData(Int64.MinValue)]
        [InlineData(Int64.MaxValue)]
        public void ReadWriteInt64(Int64 value)
        {
            _writer.Write(ElementId, value);

            var reader = StartRead();
            Assert.Equal(value, reader.ReadInt());
        }

        [Theory]
        [InlineData(0ul)]
        [InlineData(123ul)]
        [InlineData(12345678ul)]
        [InlineData(UInt64.MinValue)]
        [InlineData(UInt64.MaxValue)]
        public void ReadWriteUInt64(UInt64 value)
        {
            _writer.Write(ElementId, value);

            var reader = StartRead();
            Assert.Equal(value, reader.ReadUInt());
            Assert.Equal(_stream.Length, _stream.Position);
        }

        [Theory]
        [MemberData(nameof(ReadWriteDateTimeData))]
        public void ReadWriteDateTime(DateTime value)
        {
            _writer.Write(ElementId, value);

            var reader = StartRead();
            var result = reader.ReadDate();
            Assert.Equal(value, result);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1f)]
        [InlineData(1f)]
        [InlineData(1.12345f)]
        [InlineData(-1.12345e+23f)]
        [InlineData(float.MinValue)]
        [InlineData(float.MaxValue)]
        [InlineData(float.NaN)]
        [InlineData(float.NegativeInfinity)]
        public void ReadWriteFloat(float value)
        {
            _writer.Write(ElementId, value);

            var reader = StartRead();
            Assert.Equal(value, reader.ReadFloat());
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        [InlineData(1.0)]
        [InlineData(1.12345)]
        [InlineData(-1.12345e+23)]
        [InlineData(double.MinValue)]
        [InlineData(double.MaxValue)]
        [InlineData(double.NaN)]
        [InlineData(double.NegativeInfinity)]
        public void ReadWriteDouble(double value)
        {
            _writer.Write(ElementId, value);

            var reader = StartRead();
            Assert.Equal(value, reader.ReadFloat());
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("")]
        [InlineData("1bcdefg")]
        [InlineData("Йцукенг12345Qwerty\u1fa8\u263a")]
        public void ReadWriteStringUtf(string value)
        {
            _writer.WriteUtf(ElementId, value);

            var reader = StartRead();
            Assert.Equal(value, reader.ReadUtf());
        }

        [Theory]
        [InlineData((string)null)]
        public void ReadWriteStringUtfException(string value)
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                _writer.WriteUtf(ElementId, value);

                var reader = StartRead();
                Assert.Equal(value, reader.ReadUtf());
            });
        }

        [Fact]
        public void ReadWriteContainer()
        {
            var innerdata = new MemoryStream();
            var container = new EbmlWriter(innerdata);
            container.WriteUtf(VInt.MakeId(1), "Hello");
            container.Write(VInt.MakeId(2), 12345);
            container.Write(VInt.MakeId(3), 123.45);

            _writer.Write(VInt.MakeId(5), innerdata.ToArray());
            _writer.WriteUtf(VInt.MakeId(6), "end");

            _stream.Position = 0;
            var reader = new EbmlReader(_stream);

            Assert.True(reader.ReadNext());
            Assert.Equal(VInt.MakeId(5), reader.ElementId);

            reader.EnterContainer();

            // reading inner data
            AssertRead(reader, 1, "Hello", r => r.ReadUtf());
            AssertRead(reader, 2, 12345, r => r.ReadInt());
            AssertRead(reader, 3, 123.45, r => r.ReadFloat());

            reader.LeaveContainer();

            // back to main stream
            AssertRead(reader, 6, "end", r => r.ReadUtf());
        }

        private static void AssertRead<T>(EbmlReader reader, uint elementId, T value, Func<EbmlReader, T> read)
        {
            Assert.True(reader.ReadNext());
            Assert.Equal(VInt.MakeId(elementId), reader.ElementId);
            Assert.Equal(value, read(reader));
        }

        private static DateTime[] readWriteDateTimeData = new DateTime[]
        {
            new DateTime(2001, 01, 01),
            new DateTime(2001, 01, 01, 12, 10, 05, 123),
            new DateTime(2001, 10, 10),
            new DateTime(2101, 10, 10),
            new DateTime(1812, 10, 10),
            new DateTime(2001, 01, 01) + new TimeSpan(long.MaxValue/100),
            new DateTime(2001, 01, 01) + new TimeSpan(long.MinValue/100)
        };

        public static IEnumerable<object[]> ReadWriteDateTimeData { get { return readWriteDateTimeData.Select(d => new object[] { d }); } }

    }
}
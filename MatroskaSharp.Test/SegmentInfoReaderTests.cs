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
using System.Diagnostics;
using System.IO;
using Xunit;
using EbmlSharp;

namespace MatroskaSharp.Tests
{
	public class SegmentInfoReaderTests
	{
		[Fact]
		public void ReadsSegmentInfo()
		{
			var stream = new MemoryStream();
			var writer = new EbmlWriter(stream);

			using (var segment = writer.StartMasterElement(MatroskaDtd.Segment.Identifier))
			{
				using (var segmentInfo = segment.StartMasterElement(MatroskaDtd.Segment.Info.Identifier))
				{
					segmentInfo.WriteUtf(MatroskaDtd.Segment.Info.Title.Identifier, "Test data");
					segmentInfo.WriteUtf(MatroskaDtd.Segment.Info.WritingApp.Identifier, "writing app");
					segmentInfo.WriteUtf(MatroskaDtd.Segment.Info.MuxingApp.Identifier, "mux app");
					segmentInfo.Write(MatroskaDtd.Segment.Info.Duration.Identifier, 1234f);
				}

				// write some dummy data
				segment.Write(VInt.MakeId(123), 0);
			}

			stream.Position = 0;
			var infoReader = new SegmentInfoUpdater();
			infoReader.Open(stream);

			Assert.Equal("Test data", infoReader.Title);
			Assert.Equal("writing app", infoReader.WritingApp);
			Assert.Equal("mux app", infoReader.MuxingApp);
			Assert.Equal(TimeSpan.FromMilliseconds(1234), infoReader.Duration);
		}

		[Fact]
		public void ReusesNextFiller()
		{
			var stream = new MemoryStream();
			var writer = new EbmlWriter(stream);

			writer.Write(VInt.MakeId(122), 321);
			using (var segment = writer.StartMasterElement(MatroskaDtd.Segment.Identifier))
			{
				using (var segmentInfo = segment.StartMasterElement(MatroskaDtd.Segment.Info.Identifier))
				{
					segmentInfo.WriteUtf(MatroskaDtd.Segment.Info.Title.Identifier, "Test data");
				}

				// write some dummy data
				segment.Write(StandardDtd.Void.Identifier, new byte[1000]);
				segment.Write(VInt.MakeId(123), 123);	// marker
			}

			stream.Position = 0;

			// act
			var infoReader = new SegmentInfoUpdater();
			infoReader.Open(stream);
			infoReader.Title = new string('a', 500);
			infoReader.Write();

			// verify the stream is correct
			stream.Position = 0;
			var reader = new EbmlReader(stream);

			reader.ReadNext();
			Assert.Equal(122U, reader.ElementId.Value);
			Assert.Equal(321, reader.ReadInt());

			reader.ReadNext();
			Assert.Equal(MatroskaDtd.Segment.Identifier, reader.ElementId);
			reader.EnterContainer();

			reader.ReadNext();
			Assert.Equal(MatroskaDtd.Segment.Info.Identifier, reader.ElementId);
			Assert.True(reader.ElementSize > 500);

			reader.ReadNext();
			Assert.Equal(StandardDtd.Void.Identifier, reader.ElementId);

			reader.ReadNext();
			Assert.Equal(123U, reader.ElementId.Value);
			Assert.Equal(123, reader.ReadInt());
		}

		[Fact]
		public void ReusesPriorFiller()
		{
			var stream = new MemoryStream();
			var writer = new EbmlWriter(stream);

			writer.Write(VInt.MakeId(122), 321);
			using (var segment = writer.StartMasterElement(MatroskaDtd.Segment.Identifier))
			{
				segment.Write(StandardDtd.Void.Identifier, new byte[1000]);

				using (var segmentInfo = segment.StartMasterElement(MatroskaDtd.Segment.Info.Identifier))
				{
					segmentInfo.WriteUtf(MatroskaDtd.Segment.Info.Title.Identifier, "Test data");
				}

				// write some dummy data
				segment.Write(VInt.MakeId(123), 123);	// marker
			}

			stream.Position = 0;

			// act
			var infoReader = new SegmentInfoUpdater();
			infoReader.Open(stream);
			infoReader.Title = new string('a', 500);
			infoReader.Write();

			// verify the stream is correct
			stream.Position = 0;
			var reader = new EbmlReader(stream);

			reader.ReadNext();
			Assert.Equal(122U, reader.ElementId.Value);
			Assert.Equal(321, reader.ReadInt());

			reader.ReadNext();
			Assert.Equal(MatroskaDtd.Segment.Identifier, reader.ElementId);
			reader.EnterContainer();

			reader.ReadNext();
			Assert.Equal(MatroskaDtd.Segment.Info.Identifier, reader.ElementId);
			Assert.True(reader.ElementSize > 500);

			reader.ReadNext();
			Assert.Equal(StandardDtd.Void.Identifier, reader.ElementId);

			reader.ReadNext();
			Assert.Equal(123U, reader.ElementId.Value);
			Assert.Equal(123, reader.ReadInt());
		}

	}
}
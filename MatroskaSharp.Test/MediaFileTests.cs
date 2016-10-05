using EbmlSharp;
using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace MatroskaSharp.Test
{
    public class MediaFileTests
	{
        const string SampleFilePath = "Sample.mkv";

        public MediaFileTests()
        {
            var src = new FileInfo(@"TestMedia\Sample.mkv");
            var dest = src.CopyTo(SampleFilePath, true);
        }

        [Fact]
        public void SegmentInfoReaderSimpleUseCase()
        {
            var file = new FileInfo(SampleFilePath);
            using (var fileStream = file.Open(FileMode.Open, FileAccess.ReadWrite))
            using (var segmentInfo = new SegmentInfoUpdater())
            {
                segmentInfo.Open(fileStream);

                Debug.Print("Duration:   {0}", segmentInfo.Duration);
                Debug.Print("MuxingApp:  {0}", segmentInfo.MuxingApp);
                Debug.Print("WritingApp: {0}", segmentInfo.WritingApp);
                Debug.Print("Title:      {0}", segmentInfo.Title);

                segmentInfo.Title = "s4e16 The Cohabitation Formulation";
                segmentInfo.WritingApp = "NEbml.Viewer 0.1";

                segmentInfo.Write();
            }
        }

        [Fact]
		public void GetInfoFromMkvFile()
		{
            var filePath = SampleFilePath;

            using (var dataStream = File.OpenRead(filePath))
			{
				var reader = new EbmlReader(dataStream);
				//reader.EnterContainer();

				var headDumper = MakeElementDumper(
					StandardDtd.EBMLDesc.EBMLVersion,
					StandardDtd.EBMLDesc.EBMLReadVersion,
					StandardDtd.EBMLDesc.DocTypeVersion,
					StandardDtd.EBMLDesc.DocType,
					StandardDtd.EBMLDesc.DocTypeReadVersion);

				var trackInfoDumper = MakeElementDumper(
					MatroskaDtd.Tracks.TrackEntry.TrackNumber,
					MatroskaDtd.Tracks.TrackEntry.Name,
					MatroskaDtd.Tracks.TrackEntry.Language,
					MatroskaDtd.Tracks.TrackEntry.TrackType,
					MatroskaDtd.Tracks.TrackEntry.CodecName
					);

				reader.ReadNext();
				Assert.Equal(StandardDtd.EBML.Identifier, reader.ElementId);

				headDumper(reader);

				if (reader.LocateElement(MatroskaDtd.Segment))
				{
					reader.EnterContainer();

					if (reader.LocateElement(MatroskaDtd.Tracks))
					{
						Console.WriteLine("Tracks");
						reader.EnterContainer();
						while (reader.ReadNext())
						{
							if (reader.ElementId == MatroskaDtd.Tracks.TrackEntry.Identifier)
							{
								trackInfoDumper(reader);
							}
							Console.WriteLine();
						}
						reader.LeaveContainer();
						Console.WriteLine("end of Tracks");
					}

					if (reader.LocateElement(MatroskaDtd.Segment.Cluster))
					{
						Console.WriteLine("Got first track");

						// TODO have to deal with interlaced track data
					}
					// reader.LeaveContainer();
				}

			}
		}

		private Action<EbmlReader> MakeElementDumper(params ElementDescriptor[] descriptors)
		{
			var dumpers = Array.ConvertAll(descriptors, MakeElementDumper);

			return reader =>
				{
					reader.EnterContainer();

					while (reader.ReadNext())
					{
						foreach (var dumper in dumpers)
						{
							dumper(reader);
						}
					}

					reader.LeaveContainer();
				};
		}

		private Action<EbmlReader> MakeElementDumper(ElementDescriptor element)
		{
			Func<EbmlReader,string> dump = null;
			switch (element.Type)
			{
				case ElementType.Binary:
					dump = _ => "binary data";
					break;
				case ElementType.Date:
					dump = r => r.ReadDate().ToString();
					break;
				case ElementType.Float:
					dump = r => r.ReadFloat().ToString();
					break;
				case ElementType.SignedInteger:
					dump = r => r.ReadInt().ToString();
					break;
				case ElementType.UnsignedInteger:
					dump = r => r.ReadUInt().ToString();
					break;
				case ElementType.Utf8String:
					dump = r => r.ReadUtf();
					break;
				default:
					dump = _ => string.Format("unknown (id:{0})", element.Type.ToString());
					break;
			}

			return reader =>
				{
					if (reader.ElementId == element.Identifier)
					{
						Console.WriteLine("{0}: {1}", element.Name, dump(reader));
					}
				};
		}
	}
}
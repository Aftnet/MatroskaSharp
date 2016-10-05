using EbmlSharp;

namespace MatroskaSharp
{
	public static class ReaderExtensions
	{
		public static bool LocateElement(this EbmlReader reader, ElementDescriptor descriptor)
		{
			while (reader.ReadNext())
			{
				var identifier = reader.ElementId;

				if (identifier == descriptor.Identifier)
				{
					return true;
				}
			}
			return false;
		}
	}
}
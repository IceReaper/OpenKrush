using System.IO;

namespace OpenRA.Mods.Kknd.FileFormats
{
	public class MobdPoint
	{
		public int Id;
		public int X;
		public int Y;
		public int Z;

		public MobdPoint() { }

		public MobdPoint(Stream stream)
		{
			Id = stream.ReadInt32();
			X = stream.ReadInt32() >> 8;
			Y = stream.ReadInt32() >> 8;
			Z = stream.ReadInt32();
		}
	}
}

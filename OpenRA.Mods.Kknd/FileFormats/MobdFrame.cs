using System.Collections.Generic;
using OpenRA.Mods.Kknd.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.Kknd.FileFormats
{
	public class MobdFrame
	{
		public readonly uint OriginX;
		public readonly uint OriginY;
		public readonly MobdRenderFlags RenderFlags;
		public MobdPoint[] Points;

		public MobdFrame(SegmentStream stream, Version version)
		{
			OriginX = stream.ReadUInt32();
			OriginY = stream.ReadUInt32();
			/*Unk1 = */stream.ReadUInt32();
			var renderFlagsOffset = stream.ReadUInt32();
			/*var boxListOffset = */stream.ReadUInt32(); // we do not read boxes (2 points)
			/*Unk2 = */stream.ReadUInt32();
			var pointListOffset = stream.ReadUInt32();


			if (pointListOffset > 0)
			{
				var points = new List<MobdPoint>();
				stream.Position = pointListOffset - stream.BaseOffset;

				while (true)
				{
					var boxId = stream.ReadUInt32();

					if (boxId == 0xffffffff)
						break;

					stream.Position -= 4;
					points.Add(new MobdPoint(stream));
				}

				Points = points.ToArray();
			}

			stream.Position = renderFlagsOffset - stream.BaseOffset;
			RenderFlags = new MobdRenderFlags(stream, version);
		}
	}
}

using OpenRA.Mods.Kknd.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.Kknd.FileFormats
{
	public class MobdRenderFlags
	{
		public readonly MobdImage Image;
		public readonly uint[] Palette;

		public MobdRenderFlags(SegmentStream stream, Version version)
		{
			/*Type = */stream.ReadASCII(4);
			var flags = stream.ReadUInt32();

			if (version == Version.KKND2)
			{
				var paletteOffset = stream.ReadUInt32() - stream.BaseOffset;

				var returnPos = stream.Position;
				stream.Position = paletteOffset;
				stream.ReadUInt32(); // 00 00 00 80
				stream.ReadUInt32(); // 00 00 00 80
				stream.ReadUInt32(); // 00 00 00 80
				var numColors = stream.ReadUInt16();
				Palette = new uint[256];

				for (var i = 0; i < numColors; i++)
				{
					var color16 = stream.ReadUInt16(); // aRRRRRGGGGGBBBBB
					var r = ((color16 & 0x7c00) >> 7) & 0xff;
					var g = ((color16 & 0x03e0) >> 2) & 0xff;
					var b = ((color16 & 0x001f) << 3) & 0xff;
					Palette[i] = (uint) ((0xff << 24) | (r << 16) | (g << 8) | b);
				}

				stream.Position = returnPos;
			}

			var imageOffset = stream.ReadUInt32() - stream.BaseOffset;

			stream.Position = imageOffset;
			Image = new MobdImage(stream, flags, version);
		}
	}
}

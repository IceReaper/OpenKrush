#region Copyright & License Information

/*
 * Copyright 2007-2021 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Assets.FileFormats
{
	public class MobdRenderFlags
	{
		public readonly MobdImage Image;
		public readonly uint[]? Palette;

		public MobdRenderFlags(Stream stream)
		{
			var type = new string(stream.ReadASCII(4).Reverse().ToArray());
			var flags = stream.ReadUInt32();

			var generation = type switch
			{
				"SPRT" => GameFormat.Gen1,
				"SPNS" => GameFormat.Gen2,
				"SPRC" => GameFormat.Gen2,
				_ => GameFormat.Unknown
			};

			if (generation == GameFormat.Gen2)
			{
				var paletteOffset = stream.ReadUInt32();

				var returnPos = stream.Position;
				stream.Position = paletteOffset;
				stream.ReadUInt32(); // TODO 00 00 00 80
				stream.ReadUInt32(); // TODO 00 00 00 80
				stream.ReadUInt32(); // TODO 00 00 00 80
				var numColors = stream.ReadUInt16();
				this.Palette = new uint[256];

				for (var i = 0; i < numColors; i++)
				{
					var color16 = stream.ReadUInt16();
					var r = ((color16 & 0x7c00) >> 7) & 0xff;
					var g = ((color16 & 0x03e0) >> 2) & 0xff;
					var b = ((color16 & 0x001f) << 3) & 0xff;
					this.Palette[i] = (uint)((0xff << 24) | (r << 16) | (g << 8) | b);
				}

				stream.Position = returnPos;
			}

			var imageOffset = stream.ReadUInt32();

			stream.Position = imageOffset;
			this.Image = new(stream, flags, generation);
		}
	}
}

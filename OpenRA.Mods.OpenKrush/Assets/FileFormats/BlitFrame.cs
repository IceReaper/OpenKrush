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
	public class BlitFrame
	{
		public readonly int Width;
		public readonly int Height;
		public readonly int2 Offset;
		public readonly byte[] Pixels;

		public BlitFrame(Stream stream, byte[] palette)
		{
			this.Width = stream.ReadInt32();
			this.Height = stream.ReadInt32();
			this.Offset = new(stream.ReadInt32(), stream.ReadInt32());
			this.Pixels = stream.ReadBytes(this.Width * this.Height).SelectMany(index => palette.Skip(index * 4).Take(4)).ToArray();
		}
	}
}

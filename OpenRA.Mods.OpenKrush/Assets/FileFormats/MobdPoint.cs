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
	using System.IO;

	public class MobdPoint
	{
		public readonly int Id;
		public readonly int X;
		public readonly int Y;

		public MobdPoint(Stream stream)
		{
			this.Id = stream.ReadInt32();
			this.X = stream.ReadInt32() >> 8;
			this.Y = stream.ReadInt32() >> 8;
			stream.ReadInt32(); // TODO Unk - Likely Z
		}
	}
}

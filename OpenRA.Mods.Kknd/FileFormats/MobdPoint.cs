#region Copyright & License Information

/*
 * Copyright 2016-2018 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

using System.IO;

namespace OpenRA.Mods.Kknd.FileFormats
{
	public class MobdPoint
	{
		public int Id;
		public int X;
		public int Y;
		public int Z;

		public MobdPoint()
		{
		}

		public MobdPoint(Stream stream)
		{
			Id = stream.ReadInt32();
			X = stream.ReadInt32() >> 8;
			Y = stream.ReadInt32() >> 8;
			Z = stream.ReadInt32();
		}
	}
}

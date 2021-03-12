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

using System.Collections.Generic;
using OpenRA.Primitives;

namespace OpenRA.Mods.OpenKrush.FileFormats
{
	public class MobdFrame
	{
		public readonly uint OffsetX;
		public readonly uint OffsetY;
		public readonly MobdRenderFlags RenderFlags;
		public readonly MobdPoint[] Points;

		public MobdFrame(SegmentStream stream)
		{
			OffsetX = stream.ReadUInt32();
			OffsetY = stream.ReadUInt32();
			/*Unk1 =*/ stream.ReadUInt32();
			var renderFlagsOffset = stream.ReadUInt32();
			/*var boxListOffset =*/ stream.ReadUInt32(); // we do not read boxes (2 points, min and max)
			/*Unk2 =*/ stream.ReadUInt32();
			var pointListOffset = stream.ReadUInt32();

			// Theoretically we could also read the boxes here.
			// However they contain info about hitshaped etc. We define them in yaml to be able to tweak them.
			// But the points are required for turrets, muzzles and projectile launch offsets.
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
			RenderFlags = new MobdRenderFlags(stream);
		}
	}
}

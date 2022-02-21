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

namespace OpenRA.Mods.OpenKrush.Assets.FileFormats;

public class MobdFrame
{
	public readonly uint OffsetX;
	public readonly uint OffsetY;
	public readonly MobdRenderFlags RenderFlags;
	public readonly MobdPoint[]? Points;

	public MobdFrame(Stream stream)
	{
		this.OffsetX = stream.ReadUInt32();
		this.OffsetY = stream.ReadUInt32();
		stream.ReadUInt32(); // TODO Unk
		var renderFlagsOffset = stream.ReadUInt32();
		stream.ReadUInt32(); // TODO boxListOffset - we do not read boxes (2 points, min and max)
		stream.ReadUInt32(); // TODO Unk
		var pointListOffset = stream.ReadUInt32();

		// Theoretically we could also read the boxes here.
		// However they contain info about hitshapes etc. We define them in yaml to be able to tweak them.
		// But the points are required for turrets, muzzles and projectile launch offsets.
		if (pointListOffset > 0)
		{
			var points = new List<MobdPoint>();
			stream.Position = pointListOffset;

			while (true)
			{
				var boxId = stream.ReadUInt32();

				if (boxId == 0xffffffff)
					break;

				stream.Position -= 4;
				points.Add(new(stream));
			}

			this.Points = points.ToArray();
		}

		stream.Position = renderFlagsOffset;
		this.RenderFlags = new(stream);
	}
}

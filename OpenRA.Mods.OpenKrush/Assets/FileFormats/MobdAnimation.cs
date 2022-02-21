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

public class MobdAnimation
{
	public readonly MobdFrame[] Frames;

	public MobdAnimation(Stream stream)
	{
		// OpenRA needs the same amount of frames per facing, filling up missing frames:
		var missingFrameWorkaround = 0;

		// Beetle => 10,10,10,8,10,10,10,10,10,10,10,10,10,10,10,10
		missingFrameWorkaround += stream.Position == 174278 ? 2 : 0;

		// Flame => 9,9,9,9,8,8,9,9,9,9,9,9,9,9,9,9
		missingFrameWorkaround += stream.Position is 2010426 or 2010466 ? 1 : 0;

		// Gort => 10,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11
		missingFrameWorkaround += stream.Position == 2094122 ? 1 : 0;

		// TODO add gen2 ones here! (Worm projectile, ...)

		// TODO this is likely the animation speed.
		//      Pattern is 0x00aabbcc
		//      0x00000010
		//      0x00aaaa2a
		//      flipping the bytes to 0xccbbaa00 makes more sence:
		//      0x10000000
		//      0x2aaaaa00
		//      Notes:
		//      0x10000000 is the most common value
		//      cc is never 00
		//      aa and bb often consist of the same value: 0000 1111 8888 aaaa ...
		stream.ReadUInt32(); // TODO Unk

		var frames = new List<MobdFrame>();

		while (true)
		{
			var value = stream.ReadInt32();

			if (value is 0 or -1)
				break; // TODO 0 might mean "repeat", -1 might mean "do not repeat"

			var returnPosition = stream.Position;
			stream.Position = value;
			var frame = new MobdFrame(stream);
			frames.Add(frame);

			if (missingFrameWorkaround-- > 0)
				frames.Add(frame);

			stream.Position = returnPosition;
		}

		this.Frames = frames.ToArray();
	}
}

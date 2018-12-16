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

using System.Collections.Generic;
using OpenRA.Mods.Kknd.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.Kknd.FileFormats
{
	public class MobdAnimation
	{
		public MobdFrame[] Frames;

		public MobdAnimation(SegmentStream stream, Version version)
		{
			// This are actually broken / bugged files. As we can not fix the containers, we have them hardcoded here:
			var missingFrameWorkaround = 0;

			// Beetle => 10,10,10,8,10,10,10,10,10,10,10,10,10,10,10,10
			missingFrameWorkaround += stream.BaseStream.Position == 174278 ? 2 : 0;

			// Flame => 9,9,9,9,8,8,9,9,9,9,9,9,9,9,9,9
			missingFrameWorkaround += stream.BaseStream.Position == 2010426 || stream.BaseStream.Position == 2010466 ? 1 : 0;

			// Gort => 10,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11
			missingFrameWorkaround += stream.BaseStream.Position == 2094122 ? 1 : 0;

			// TODO add kknd2 bugged ones here! (Worm projectile, ...)

			/*Unk1 = */stream.ReadUInt32(); // TODO this is likely the animation speed

			var frames = new List<MobdFrame>();

			while (true)
			{
				var value = stream.ReadInt32();

				if (value == 0 || value == -1)
					break; // TODO this is most likely repeat pattern. 1,2,3,1,2,3 or 1,2,3,2,1,2,3

				var returnPosition = stream.Position;
				stream.Position = value - stream.BaseOffset;
				var frame = new MobdFrame(stream, version);
				frames.Add(frame);

				if (missingFrameWorkaround-- > 0)
					frames.Add(frame);

				stream.Position = returnPosition;
			}

			Frames = frames.ToArray();

			if (version == Version.KKND2)
			{
				// KKnD 2 uses offsets only for frames they are used on instead of the whole animation.
				var points = new List<MobdPoint>();

				foreach (var frame in Frames)
				{
					if (frame.Points == null)
						continue;

					foreach (var point in frame.Points)
					{
						if (point.Id == 0)
							points.Add(new MobdPoint { X = point.X, Y = point.Y, Z = point.Z, Id = points.Count });
					}
				}

				foreach (var frame in Frames)
					frame.Points = points.ToArray();
			}
		}
	}
}

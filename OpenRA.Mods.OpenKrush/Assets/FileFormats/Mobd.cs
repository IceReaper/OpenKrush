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
	using System;
	using System.Collections.Generic;
	using System.IO;

	public class Mobd
	{
		public readonly MobdAnimation[] RotationalAnimations;
		public readonly MobdAnimation[] SimpleAnimations;

		public Mobd(Stream stream)
		{
			var fileStart = (uint)stream.Position;
			var firstFrameStart = stream.Length;

			var animationOffsets = new List<uint>();
			var rotationalAnimations = new List<MobdAnimation>();
			var simpleAnimations = new List<MobdAnimation>();

			while (stream.Position < firstFrameStart)
			{
				var value = stream.ReadInt32();

				if (value == 0 || (value < stream.Position && value >= fileStart))
				{
					stream.Position -= 4;

					break;
				}

				animationOffsets.Add((uint)(stream.Position - 4));

				while (true)
				{
					value = stream.ReadInt32();

					if (value is -1 or 0)
						break;

					firstFrameStart = Math.Min(firstFrameStart, value);
				}
			}

			while (stream.Position < firstFrameStart)
			{
				var value = stream.ReadUInt32();

				if (value == 0)
					continue;

				animationOffsets.Remove(value);
				var returnPosition = stream.Position;
				stream.Position = value;
				rotationalAnimations.Add(new(stream));
				stream.Position = returnPosition;
			}

			foreach (var animationOffset in animationOffsets)
			{
				stream.Position = animationOffset;
				simpleAnimations.Add(new(stream));
			}

			this.RotationalAnimations = rotationalAnimations.ToArray();
			this.SimpleAnimations = simpleAnimations.ToArray();
		}
	}
}

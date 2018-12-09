using System;
using System.Collections.Generic;
using OpenRA.Primitives;
using Version = OpenRA.Mods.Kknd.FileSystem.Version;

namespace OpenRA.Mods.Kknd.FileFormats
{
	public class Mobd
	{
		public readonly MobdAnimation[] Animations;
		public readonly MobdAnimation[] HardcodedAnimations;

		public Mobd(SegmentStream stream, Version version)
		{
			var fileOffset = (uint)stream.BaseOffset;
			var firstFrameStart = stream.Length;
			var justReadFrameOffset = false;

			var animationOffsets = new List<uint>();
			var animations = new List<MobdAnimation>();
			var hardcodedAnimations = new List<MobdAnimation>();

			while (stream.Position < firstFrameStart)
			{
				var value = stream.ReadUInt32();

				// This parsing method is trash, because animation offsets are hardcoded in .exe but it seems to work.
				if ((value == 0xffffffff || value == 0x00000000) && justReadFrameOffset)
				{
					// terminator
					justReadFrameOffset = false;
				}
				else if (value - fileOffset > stream.Position && value - fileOffset < stream.Length)
				{
					// frame
					justReadFrameOffset = true;
					firstFrameStart = Math.Min(firstFrameStart, value - fileOffset);
				}
				else if (value - fileOffset < stream.Position && value >= fileOffset)
				{
					// animation pointer
					animationOffsets.Remove(value - fileOffset);
					var returnPosition = stream.Position;
					stream.Position = value - fileOffset;
					animations.Add(new MobdAnimation(stream, version));
					stream.Position = returnPosition;
				}
				else if (value == 0)
				{
					// TODO filler ? Sprite Invisible?
				}
				else
					animationOffsets.Add((uint)(stream.Position - 4));
			}

			foreach (var animationOffset in animationOffsets)
			{
				try
				{
					stream.Position = animationOffset;
					hardcodedAnimations.Add(new MobdAnimation(stream, version));
				}
				catch (Exception)
				{
					// TODO crashes on kknd2, fix me!
				}
			}

			Animations = animations.ToArray();
			HardcodedAnimations = hardcodedAnimations.ToArray();
		}
	}
}

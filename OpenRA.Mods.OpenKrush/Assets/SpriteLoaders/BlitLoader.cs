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

namespace OpenRA.Mods.OpenKrush.Assets.SpriteLoaders;

using FileFormats;
using Graphics;
using JetBrains.Annotations;
using Primitives;

[UsedImplicitly]
public class BlitLoader : ISpriteLoader
{
	private class BlitSpriteFrame : ISpriteFrame
	{
		public SpriteFrameType Type => SpriteFrameType.Bgra32;
		public Size Size { get; }
		public Size FrameSize { get; }
		public float2 Offset { get; }
		public byte[] Data { get; }

		public bool DisableExportPadding => true;

		public BlitSpriteFrame(BlitFrame blitFrame)
		{
			this.FrameSize = this.Size = new(blitFrame.Width, blitFrame.Height);
			this.Offset = new int2(blitFrame.Width / 2, blitFrame.Height / 2) - blitFrame.Offset;
			this.Data = blitFrame.Pixels;
		}
	}

	public bool TryParseSprite(Stream stream, string filename, out ISpriteFrame[]? frames, out TypeDictionary? metadata)
	{
		metadata = null;

		if (!filename.EndsWith(".blit") || stream is not SegmentStream segmentStream)
		{
			frames = null;

			return false;
		}

		// This is damn ugly, but BLIT uses offsets from LVL start.
		segmentStream.BaseStream.Position = segmentStream.BaseOffset;
		frames = new Blit(segmentStream.BaseStream).Frames.Select(blitFrame => new BlitSpriteFrame(blitFrame) as ISpriteFrame).ToArray();

		return true;
	}
}

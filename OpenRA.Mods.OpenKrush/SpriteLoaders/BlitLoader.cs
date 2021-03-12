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

using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.OpenKrush.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Mods.OpenKrush.SpriteLoaders
{
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
				var width = blitFrame.Width;
				var height = blitFrame.Height;
				var x = blitFrame.OffsetX;
				var y = blitFrame.OffsetY;

				Size = new Size(width, height);
				FrameSize = new Size(width, height);
				Offset = new int2(width / 2 - x, height / 2 - y);
				Data = blitFrame.Pixels;
			}
		}

		public bool TryParseSprite(Stream stream, string filename, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			if (!filename.EndsWith(".blit"))
			{
				metadata = null;
				frames = null;
				return false;
			}

			frames = new Blit(stream as SegmentStream).Frames.Select(blitFrame => new BlitSpriteFrame(blitFrame)).ToArray();
			metadata = null;

			return true;
		}
	}
}

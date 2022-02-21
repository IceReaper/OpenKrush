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

using Common.Graphics;
using FileFormats;
using Graphics;
using JetBrains.Annotations;
using Mechanics.DataFromAssets.Graphics;
using Primitives;

[UsedImplicitly]
public class MobdLoader : ISpriteLoader
{
	private class MobdSpriteFrame : ISpriteFrame
	{
		public SpriteFrameType Type => SpriteFrameType.Indexed8;
		public Size Size { get; }
		public Size FrameSize { get; }
		public float2 Offset { get; }
		public byte[] Data { get; }
		public readonly uint[]? Palette;
		public readonly MobdPoint[]? Points;

		public bool DisableExportPadding => true;

		public MobdSpriteFrame(MobdFrame mobdFrame)
		{
			var width = mobdFrame.RenderFlags.Image.Width;
			var height = mobdFrame.RenderFlags.Image.Height;
			var x = mobdFrame.OffsetX;
			var y = mobdFrame.OffsetY;

			this.Size = new(width, height);
			this.FrameSize = new(width, height);
			this.Offset = new int2((int)(width / 2 - x), (int)(height / 2 - y));
			this.Data = mobdFrame.RenderFlags.Image.Pixels;
			this.Palette = mobdFrame.RenderFlags.Palette;
			this.Points = mobdFrame.Points;
		}
	}

	public bool TryParseSprite(Stream stream, string filename, out ISpriteFrame[]? frames, out TypeDictionary? metadata)
	{
		if (!filename.EndsWith(".mobd") || stream is not SegmentStream segmentStream)
		{
			metadata = null;
			frames = null;

			return false;
		}

		// This is damn ugly, but MOBD uses offsets from LVL start.
		segmentStream.BaseStream.Position = segmentStream.BaseOffset;
		var mobd = new Mobd(segmentStream.BaseStream);
		var tmp = new List<MobdSpriteFrame>();

		tmp.AddRange(mobd.RotationalAnimations.SelectMany(mobdAnimation => mobdAnimation.Frames, (_, mobdFrame) => new MobdSpriteFrame(mobdFrame)));
		tmp.AddRange(mobd.SimpleAnimations.SelectMany(mobdAnimation => mobdAnimation.Frames, (_, mobdFrame) => new MobdSpriteFrame(mobdFrame)));

		uint[]? palette = null;
		var points = new Dictionary<int, Offset[]>();

		for (var i = 0; i < tmp.Count; i++)
		{
			if (tmp[i].Points != null)
			{
				var mobdPoints = tmp[i].Points;

				if (mobdPoints != null)
					points.Add(i, mobdPoints.Select(point => new Offset(point.Id, point.X, point.Y)).ToArray());
			}

			if (tmp[i].Palette != null)
				palette = tmp[i].Palette;
		}

		frames = tmp.Select(e => e as ISpriteFrame).ToArray();

		metadata = new() { new EmbeddedSpriteOffsets(points) };

		if (palette != null)
			metadata.Add(new EmbeddedSpritePalette(palette));

		return true;
	}
}

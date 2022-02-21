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
public class MapdLoader : ISpriteLoader
{
	private class MapdSpriteFrame : ISpriteFrame
	{
		public SpriteFrameType Type => SpriteFrameType.Rgba32;

		public Size Size { get; }

		public Size FrameSize { get; }

		public float2 Offset => float2.Zero;

		public byte[] Data { get; }

		public bool DisableExportPadding => true;

		public MapdSpriteFrame(MapdLayer layer)
		{
			this.Size = new(layer.Width, layer.Height);
			this.FrameSize = new(layer.Width, layer.Height);
			this.Data = layer.Pixels;
		}
	}

	public bool TryParseSprite(Stream stream, string filename, out ISpriteFrame[]? frames, out TypeDictionary? metadata)
	{
		metadata = null;

		if (!filename.EndsWith(".mapd") || stream is not SegmentStream segmentStream)
		{
			frames = null;

			return false;
		}

		// This is damn ugly, but MAPD uses offsets from LVL start.
		segmentStream.BaseStream.Position = segmentStream.BaseOffset;
		frames = new Mapd(segmentStream.BaseStream).Layers.Select(layer => new MapdSpriteFrame(layer) as ISpriteFrame).ToArray();

		return true;
	}
}

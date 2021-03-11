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
                Size = new Size(layer.Width, layer.Height);
                FrameSize = new Size(layer.Width, layer.Height);
                Data = layer.Pixels;
            }
        }

        public bool TryParseSprite(Stream stream, string filename, out ISpriteFrame[] frames, out TypeDictionary metadata)
        {
            if (!filename.EndsWith(".mapd"))
            {
                metadata = null;
                frames = null;
                return false;
            }

            frames = new Mapd(stream as SegmentStream).Layers.Select(layer => new MapdSpriteFrame(layer)).ToArray();
            metadata = null;

            return true;
        }
    }
}

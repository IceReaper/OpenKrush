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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.OpenKrush.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Mods.OpenKrush.FileFormats
{
    public class Mapd
    {
        public readonly MapdLayer[] Layers;

        public Mapd(SegmentStream stream)
        {
            // This is damn ugly, but it seems MAPD uses offsets from lvl start.
            var basePosition = ((SegmentStream)stream.BaseStream).BaseStream.Position - 8;

            var test = stream.ReadInt32();
            stream.Position += test * 4;
            var generation = stream.ReadInt32() == 256 ? Generation.Gen1 : Generation.Gen2;
            stream.Position -= (test + 2) * 4;

            if (generation == Generation.Gen2)
                stream.ReadInt32(); // Unk

            var layerOffsets = new int[stream.ReadInt32()];
            Layers = new MapdLayer[layerOffsets.Length];

            for (var i = 0; i < layerOffsets.Length; i++)
                layerOffsets[i] = stream.ReadInt32();

            var palette = new byte[stream.ReadInt32() * 4];

            if (generation == Generation.Gen2)
            {
                for (var i = 0; i < palette.Length;)
                {
                    var color16 = stream.ReadUInt16(); // aRRRRRGGGGGBBBBB
                    palette[i++] = (byte)(((color16 & 0x7c00) >> 7) & 0xff);
                    palette[i++] = (byte)(((color16 & 0x03e0) >> 2) & 0xff);
                    palette[i++] = (byte)(((color16 & 0x001f) << 3) & 0xff);
                    palette[i++] = 0xff;
                }
            }
            else
            {
                stream.Read(palette);

                for (var i = 0; i < palette.Length / 4; i++)
                    palette[i * 4 + 3] = 0xff;
            }

            for (var i = 0; i < Layers.Length; i++)
            {
                stream.Position = layerOffsets[i] - basePosition;

                var type = new string(stream.ReadASCII(4).Reverse().ToArray());

                if (type != "SCRL")
                    throw new Exception("Unknown type.");

                var tileWidth = stream.ReadInt32();
                var tileHeight = stream.ReadInt32();
                var tilesX = stream.ReadInt32();
                var tilesY = stream.ReadInt32();

                if (generation == Generation.Gen2)
                {
                    stream.ReadInt32(); // Unk
                    stream.ReadInt32(); // Unk
                    stream.ReadInt32(); // Unk
                }

                var tilePixels = new Dictionary<int, byte[]>();
                var tiles = new List<int>();

                for (var y = 0; y < tilesY; y++)
                for (var x = 0; x < tilesX; x++)
                {
                    var tile = stream.ReadInt32();

                    if (generation == Generation.Gen2)
                        tile -= tile % 4;

                    tiles.Add(tile);

                    if (tile != 0 && !tilePixels.ContainsKey(tile))
                        tilePixels.Add(tile, new byte[tileWidth * tileHeight * 4]);
                }

                foreach (var (offset, pixels) in tilePixels)
                {
                    stream.Position = offset - basePosition;

                    if (generation == Generation.Gen1)
                        stream.ReadInt32(); // Unk

                    for (var y = 0; y < tileHeight; y++)
                    for (var x = 0; x < tileWidth; x++)
                    {
                        var index = stream.ReadByte();

                        if (index == 0 && i != 0)
                            continue;

                        Array.Copy(palette, index * 4, pixels, (y * tileWidth + x) * 4, 4);
                    }
                }

                var layer = new MapdLayer(tilesX * tileWidth, tilesY * tileHeight);
                Layers[i] = layer;

                for (var y = 0; y < tilesY; y++)
                for (var x = 0; x < tilesX; x++)
                {
                    var tile = tiles[y * tilesX + x];

                    if (tile == 0)
                        continue;

                    var pixels = tilePixels[tile];
                    var offset = (y * tileHeight * tilesX + x) * tileWidth;

                    for (var row = 0; row < tileHeight; row++)
                        Array.Copy(pixels, row * tileWidth * 4, layer.Pixels, (offset + row * layer.Width) * 4, tileWidth * 4);
                }
            }
        }
    }
}

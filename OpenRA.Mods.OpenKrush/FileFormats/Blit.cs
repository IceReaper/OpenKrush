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
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Mods.OpenKrush.FileFormats
{
    public class Blit
    {
        public readonly BlitFrame[] Frames;

        public Blit(SegmentStream stream)
        {
            // This is damn ugly, but it seems BLIT uses offsets from lvl start.
            var basePosition = (int)((SegmentStream)stream.BaseStream).BaseStream.Position - 8;

            Frames = new BlitFrame[stream.ReadInt32()];
            var frameOffsets = new int[Frames.Length];

            stream.ReadInt32(); // Unk
            var paletteOffset = stream.ReadInt32() - basePosition;
            var identifier = new string(stream.ReadASCII(4).Reverse().ToArray());

            if (identifier != "BLT8")
                throw new Exception("Unknwon blit type.");
            for (var i = 0; i < Frames.Length; i++)
                frameOffsets[i] = stream.ReadInt32() - basePosition;

            stream.Position = paletteOffset;

            var palette = new byte[256 * 4];

            for (var i = 0; i < palette.Length;)
            {
                var color16 = stream.ReadUInt16(); // aRRRRRGGGGGBBBBB
                palette[i++] = (byte)(((color16 & 0x001f) << 3) & 0xff);
                palette[i++] = (byte)(((color16 & 0x03e0) >> 2) & 0xff);
                palette[i++] = (byte)(((color16 & 0x7c00) >> 7) & 0xff);
                palette[i++] = 0xff;
            }

            for (var i = 0; i < Frames.Length; i++)
            {
                stream.Position = frameOffsets[i];
                Frames[i] = new BlitFrame(stream, palette);
            }
        }
    }
}

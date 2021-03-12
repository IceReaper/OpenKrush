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
using System.IO;

namespace OpenRA.Mods.OpenKrush.FileFormats
{
    public class BlitFrame
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int OffsetX;
        public readonly int OffsetY;
        public readonly byte[] Pixels;

        public BlitFrame(Stream stream, byte[] palette)
        {
            Width = stream.ReadInt32();
            Height = stream.ReadInt32();
            OffsetX = stream.ReadInt32();
            OffsetY = stream.ReadInt32();
            Pixels = new byte[Width * Height * 4];

            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                Array.Copy(palette, stream.ReadByte() * 4, Pixels, (y * Width + x) * 4, 4);
        }
    }
}

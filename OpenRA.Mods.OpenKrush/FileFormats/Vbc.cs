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
using System.Linq;
using OpenRA.Video;

namespace OpenRA.Mods.OpenKrush.FileFormats
{
    public class Vbc : IVideo
    {
        public ushort Frames { get; }
        public byte Framerate { get; }
        public ushort Width { get; }
        public ushort Height { get; }
        public uint[,] FrameData { get; private set; }
        public string TextData { get; private set; }
        public int CurrentFrame { get; private set; }
        public bool HasAudio => true;
        public byte[] AudioData { get; }
        public int AudioChannels => 1;
        public int SampleBits { get; }
        public int SampleRate { get; }

        private readonly VbcFrame[] frames;
        private uint[,] frame;
        private uint[] palette;
        private int stride = 1;

        public Vbc(Stream stream)
        {
            if (stream.ReadASCII(4) != "SIFF")
                throw new InvalidDataException("Invalid vbc (invalid SIFF section)");

            stream.ReadUInt32(); // Length

            if (stream.ReadASCII(4) != "VBV1")
                throw new InvalidDataException("Invalid vbc (not VBV1)");

            if (stream.ReadASCII(4) != "VBHD")
                throw new InvalidDataException("Invalid vbc (not VBHD)");

            stream.ReadUInt32(); // Length
            stream.ReadUInt16(); // Version
            Width = stream.ReadUInt16();
            Height = stream.ReadUInt16();
            stream.ReadUInt32(); // 0
            Frames = stream.ReadUInt16();
            SampleBits = stream.ReadUInt16();
            SampleRate = stream.ReadUInt16();
            stream.ReadUInt32(); // 0
            stream.ReadUInt32(); // 0
            stream.ReadUInt32(); // 0
            stream.ReadUInt32(); // 0

            if (Width == 640 && Height == 240)
            {
                Height = 480;
                stride = 2;
            }

            if (stream.ReadASCII(4) != "BODY")
                throw new InvalidDataException("Invalid vbc (not BODY)");

            stream.ReadUInt32(); // Length

            frames = new VbcFrame[Frames];

            for (var i = 0; i < Frames; i++)
                frames[i] = new VbcFrame(stream);

            var audio = new MemoryStream();

            foreach (var frame in frames.Where(f => f.Audio != null))
                audio.WriteArray(frame.Audio);

            AudioData = audio.ToArray();
            var a = audio.Length / (SampleRate * 1 * (SampleBits / 8));
            Framerate = (byte)(Frames / a);

            Reset();
        }

        public void AdvanceFrame()
        {
            CurrentFrame++;
            LoadFrame();
        }

        public void Reset()
        {
            CurrentFrame = 0;
            LoadFrame();
        }

        private void LoadFrame()
        {
            var textData = "";

            if (CurrentFrame == 0)
            {
                frame = new uint[Height / stride, Width];
                palette = new uint[256];
                TextData = "";
            }
            else
                frame = frames[CurrentFrame - 1].ApplyFrame(frame, ref palette, ref textData);

            TextData += textData;

            // TODO for better performance, we should get rid of this copying as soon we can use non-power-of-2 textures
            FrameData = new uint[Exts.NextPowerOf2(Height), Exts.NextPowerOf2(Width)];

            for (var y = 0; y < Height / stride; y++)
            for (var i = 0; i < stride; i++)
                Buffer.BlockCopy(frame, y * Width * 4, FrameData, (y * stride + i) * FrameData.GetLength(1) * 4, Width * 4);
        }
    }
}

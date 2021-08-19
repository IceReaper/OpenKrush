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

namespace OpenRA.Mods.OpenKrush.Assets.FileFormats
{
	using System;
	using System.IO;
	using System.Linq;
	using Video;

	public class Vbc : IVideo
	{
		public ushort Frames { get; }
		public byte Framerate { get; }
		public ushort Width { get; }
		public ushort Height { get; }
		public uint[,]? FrameData { get; private set; }
		public string? TextData { get; private set; }
		public int CurrentFrame { get; private set; }
		public bool HasAudio => true;
		public byte[] AudioData { get; }
		public int AudioChannels => 1;
		public int SampleBits { get; }
		public int SampleRate { get; }

		private readonly VbcFrame[] frames;
		private uint[,] currentFrame;
		private uint[] palette;
		private readonly int stride;

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
			this.Width = stream.ReadUInt16();
			this.Height = stream.ReadUInt16();
			stream.ReadUInt32(); // 0
			this.Frames = stream.ReadUInt16();
			this.SampleBits = stream.ReadUInt16();
			this.SampleRate = stream.ReadUInt16();
			stream.ReadUInt32(); // 0
			stream.ReadUInt32(); // 0
			stream.ReadUInt32(); // 0
			stream.ReadUInt32(); // 0

			if (this.Width == 640 && this.Height == 240)
			{
				this.Height = 480;
				this.stride = 2;
			}
			else
				this.stride = 1;

			this.currentFrame = new uint[this.Height / this.stride, this.Width];
			this.palette = new uint[256];

			if (stream.ReadASCII(4) != "BODY")
				throw new InvalidDataException("Invalid vbc (not BODY)");

			stream.ReadUInt32(); // Length

			this.frames = new VbcFrame[this.Frames];

			for (var i = 0; i < this.Frames; i++)
				this.frames[i] = new(stream);

			var audio = new MemoryStream();

			foreach (var frame in this.frames.Where(f => f.Audio != null))
				audio.WriteArray(frame.Audio);

			this.AudioData = audio.ToArray();
			var a = audio.Length / (this.SampleRate * 1 * (this.SampleBits / 8));
			this.Framerate = (byte)(this.Frames / a);

			this.Reset();
		}

		public void AdvanceFrame()
		{
			this.CurrentFrame++;
			this.LoadFrame();
		}

		public void Reset()
		{
			this.CurrentFrame = 0;
			this.LoadFrame();
		}

		private void LoadFrame()
		{
			if (this.CurrentFrame == 0)
			{
				this.currentFrame = new uint[this.Height / this.stride, this.Width];
				this.palette = new uint[256];
				this.TextData = "";
			}
			else
			{
				var nextFrame = this.frames[this.CurrentFrame - 1];

				this.currentFrame = nextFrame.ApplyFrame(this.currentFrame, ref this.palette);

				if (nextFrame.Text != null)
					this.TextData += nextFrame.Text;
			}

			// TODO for better performance, we should get rid of this copying as soon we can use non-power-of-2 textures
			this.FrameData = new uint[Exts.NextPowerOf2(this.Height), Exts.NextPowerOf2(this.Width)];

			for (var y = 0; y < this.Height / this.stride; y++)
			for (var i = 0; i < this.stride; i++)
			{
				Buffer.BlockCopy(
					this.currentFrame,
					y * this.Width * 4,
					this.FrameData,
					(y * this.stride + i) * this.FrameData.GetLength(1) * 4,
					this.Width * 4
				);
			}
		}
	}
}

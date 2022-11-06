#region Copyright & License Information

/*
 * Copyright 2007-2022 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Assets.FileFormats;

using Video;

public class Vbc : IVideo
{
	public ushort FrameCount { get; }
	public byte Framerate { get; }
	public ushort Width { get; }
	public ushort Height { get; }
	public byte[]? CurrentFrameData { get; private set; }
	public string? TextData { get; private set; }
	public int CurrentFrameIndex { get; private set; }
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
		this.FrameCount = stream.ReadUInt16();
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

		this.frames = new VbcFrame[this.FrameCount];

		for (var i = 0; i < this.FrameCount; i++)
			this.frames[i] = new(stream);

		var audio = new MemoryStream();

		foreach (var frame in this.frames.Where(f => f.Audio != null))
			audio.WriteArray(frame.Audio);

		this.AudioData = audio.ToArray();
		var a = audio.Length / (this.SampleRate * 1 * (this.SampleBits / 8));
		this.Framerate = (byte)(this.FrameCount / a);

		this.Reset();
	}

	public void AdvanceFrame()
	{
		this.CurrentFrameIndex++;
		this.LoadFrame();
	}

	public void Reset()
	{
		this.CurrentFrameIndex = 0;
		this.LoadFrame();
	}

	private void LoadFrame()
	{
		if (this.CurrentFrameIndex == 0)
		{
			this.currentFrame = new uint[this.Height / this.stride, this.Width];
			this.palette = new uint[256];
			this.TextData = "";
		}
		else
		{
			var nextFrame = this.frames[this.CurrentFrameIndex - 1];

			this.currentFrame = nextFrame.ApplyFrame(this.currentFrame, ref this.palette);

			if (nextFrame.Text != null)
				this.TextData += nextFrame.Text;
		}

		// TODO for better performance, we should get rid of this copying as soon we can use non-power-of-2 textures
		this.CurrentFrameData = new byte[Exts.NextPowerOf2(this.Height) * Exts.NextPowerOf2(this.Width) * 4];

		for (var y = 0; y < this.Height / this.stride; y++)
		for (var i = 0; i < this.stride; i++)
		{
			Buffer.BlockCopy(
				this.currentFrame,
				y * this.Width * 4,
				this.CurrentFrameData,
				(y * this.stride + i) * Exts.NextPowerOf2(this.Width) * 4,
				this.Width * 4
			);
		}
	}
}

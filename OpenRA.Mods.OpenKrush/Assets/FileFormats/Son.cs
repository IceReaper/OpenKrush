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

	public class Son : ISoundFormat
	{
		private readonly byte[] data;

		public int Channels { get; }
		public int SampleBits { get; }
		public int SampleRate { get; }
		public float LengthInSeconds => (float)this.data.Length / (this.Channels * this.SampleRate * this.SampleBits);

		public Son(Stream stream)
		{
			stream.ReadASCII(4); // SIFF
			stream.ReadInt32(); // fileSize - BE
			stream.ReadASCII(8); // SOUNSHDR
			stream.ReadInt16(); // 0
			stream.ReadInt16(); // 8
			stream.ReadInt32(); // 1/4 of the size?
			this.SampleRate = stream.ReadUInt16();
			this.SampleBits = stream.ReadByte();
			this.Channels = stream.ReadByte() == 1 ? 2 : 1;
			stream.ReadASCII(4); // BODY
			var size = (stream.ReadByte() << 24) | (stream.ReadByte() << 16) | (stream.ReadByte() << 8) | stream.ReadByte();
			this.data = stream.ReadBytes(size);
		}

		public Stream GetPCMInputStream()
		{
			return new MemoryStream(this.data);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}
	}
}

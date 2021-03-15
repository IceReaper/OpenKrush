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

namespace OpenRA.Mods.OpenKrush.FileFormats
{
	using System.IO;

	public class Son : ISoundFormat
	{
		private readonly byte[] data;

		public int Channels { get; }
		public int SampleBits { get; }
		public int SampleRate { get; }
		public float LengthInSeconds => (float)data.Length / (Channels * SampleRate * SampleBits);

		public Son(Stream stream)
		{
			stream.ReadASCII(4); // SIFF
			stream.ReadInt32(); // fileSize - BE
			stream.ReadASCII(8); // SOUNSHDR
			stream.ReadInt16(); // 0
			stream.ReadInt16(); // 8
			stream.ReadInt32(); // 1/4 of the size?
			SampleRate = stream.ReadUInt16();
			SampleBits = stream.ReadByte();
			Channels = stream.ReadByte() == 1 ? 2 : 1;
			stream.ReadASCII(4); // BODY
			var size = stream.ReadByte() << 24 | stream.ReadByte() << 16 | stream.ReadByte() << 8 | stream.ReadByte();
			data = stream.ReadBytes(size);
		}

		public Stream GetPCMInputStream()
		{
			return new MemoryStream(data);
		}

		public void Dispose()
		{
		}
	}
}

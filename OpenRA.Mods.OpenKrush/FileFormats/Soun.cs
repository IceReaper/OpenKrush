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

namespace OpenRA.Mods.OpenKrush.FileFormats
{
	public class Soun : ISoundFormat
	{
		private readonly byte[] data;

		public int Channels { get; }
		public int SampleBits { get; }
		public int SampleRate { get; }
		public float LengthInSeconds => (float)data.Length / (Channels * SampleRate * SampleBits);

		public Soun(Stream stream)
		{
			var size = stream.ReadInt32();
			SampleRate = stream.ReadInt32();
			SampleBits = stream.ReadInt32();
			Channels = stream.ReadInt32();
			stream.ReadUInt32(); // unk
			stream.Position += 32; // Empty
			stream.ReadBytes(20); // Filename
			data = stream.ReadBytes(size * Channels);
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

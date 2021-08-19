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

	public class Soun : ISoundFormat
	{
		private readonly byte[] data;

		public int Channels { get; }
		public int SampleBits { get; }
		public int SampleRate { get; }
		public float LengthInSeconds => (float)this.data.Length / (this.Channels * this.SampleRate * this.SampleBits);

		public Soun(Stream stream)
		{
			var size = stream.ReadInt32();
			this.SampleRate = stream.ReadInt32();
			this.SampleBits = stream.ReadInt32();
			this.Channels = stream.ReadInt32();
			stream.ReadUInt32(); // unk
			stream.Position += 32; // Empty
			stream.ReadBytes(20); // Filename
			this.data = stream.ReadBytes(size * this.Channels);
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

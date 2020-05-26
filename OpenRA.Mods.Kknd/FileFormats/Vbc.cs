#region Copyright & License Information
/*
 * Copyright 2016-2018 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.IO;
using System.Linq;

namespace OpenRA.Mods.Kknd.FileFormats
{
	public class Vbc
	{
		private VbcFrame[] frames;
		public int2 Size;

		public ushort Frames;
		public ushort SampleBits;
		public ushort SampleRate;

		public Vbc(Stream stream)
		{
			if (stream.ReadASCII(4) != "SIFF")
				throw new InvalidDataException("Invalid vbc (invalid SIFF section)");

			/*var length = */stream.ReadUInt32();

			if (stream.ReadASCII(4) != "VBV1")
				throw new InvalidDataException("Invalid vbc (not VBV1)");

			if (stream.ReadASCII(4) != "VBHD")
				throw new InvalidDataException("Invalid vbc (not VBHD)");

			/*var length = */stream.ReadUInt32();
			stream.ReadUInt16(); // 1 Version?
			Size = new int2(stream.ReadUInt16(), stream.ReadUInt16());
			stream.ReadUInt32(); // 0
			Frames = stream.ReadUInt16();
			SampleBits = stream.ReadUInt16();
			SampleRate = stream.ReadUInt16();
			stream.ReadUInt32(); // 0
			stream.ReadUInt32(); // 0
			stream.ReadUInt32(); // 0
			stream.ReadUInt32(); // 0

			if (stream.ReadASCII(4) != "BODY")
				throw new InvalidDataException("Invalid vbc (not BODY)");

			/*var length = */stream.ReadUInt32();

			frames = new VbcFrame[Frames];

			for (var i = 0; i < Frames; i++)
				frames[i] = new VbcFrame(stream);
		}

		public byte[] GetAudio()
		{
			var audio = new MemoryStream();

			foreach (var frame in frames.Where(f => f.Audio != null))
				audio.WriteArray(frame.Audio);

			return audio.ToArray();
		}

		public byte[] ApplyFrame(int frameId, byte[] frame, uint[] palette)
		{
			return frames[frameId].ApplyFrame(frame, palette, Size);
		}
	}
}

#region Copyright & License Information
/*
 * Copyright 2007-2021 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using Version = OpenRA.Mods.Kknd.FileSystem.Version;

namespace OpenRA.Mods.Kknd.FileFormats
{
	public class MobdImage
	{
		public readonly int Width;
		public readonly int Height;
		public readonly byte[] Pixels;

		public MobdImage(Stream stream, uint flags, Version version)
		{
			bool flipped;
			Width = stream.ReadInt32();
			Height = stream.ReadInt32();
			Pixels = new byte[Width * Height];

			if (version == Version.KKND1)
			{
				flipped = (flags & 0x1) == 1;

				var isCompressed = stream.ReadUInt8() == 2;

				if (isCompressed)
					DecompressKknd1(stream);
				else
					stream.ReadBytes(Pixels, 0, Pixels.Length);
			}
			else
			{
				flipped = ((flags >> 31) & 0x1) == 1;
				var isCompressed = ((flags >> 27) & 0x1) == 1;
				var has256Colors = ((flags >> 26) & 0x1) == 1;

				if (isCompressed)
					DecompressKknd2(stream, has256Colors);
				else
					stream.ReadBytes(Pixels, 0, Pixels.Length);
			}

			if (!flipped)
				return;

			for (var i = 0; i < Height; i++)
				Array.Reverse(Pixels, i * Width, Width);
		}

		private void DecompressKknd1(Stream compressed)
		{
			var decompressed = new MemoryStream(Pixels);

			while (decompressed.Position < decompressed.Capacity)
			{
				var compressedSize = compressed.ReadUInt8() - 1;
				var lineEndOffset = compressed.Position + compressedSize;
				var isSkipMode = true;

				while (compressed.Position < lineEndOffset)
				{
					var chunkSize = compressed.ReadUInt8();

					if (isSkipMode)
						decompressed.Position += chunkSize;
					else
						decompressed.WriteArray(compressed.ReadBytes(chunkSize));

					isSkipMode = !isSkipMode;
				}

				decompressed.Position += (Width - decompressed.Position % Width) % Width;
			}
		}

		private void DecompressKknd2(Stream compressed, bool has256Colors)
		{
			var decompressed = new MemoryStream(Pixels);

			while (decompressed.Position < decompressed.Capacity)
			{
				int compressedSize = has256Colors ? compressed.ReadUInt16() : compressed.ReadUInt8();

				if (compressedSize == 0)
					decompressed.Position += Width;
				else if (!has256Colors && compressedSize > 0x80)
				{
					var pixelCount = compressedSize - 0x80;

					for (var i = 0; i < pixelCount; i++)
					{
						var twoPixels = compressed.ReadUInt8();
						decompressed.WriteByte((byte)((twoPixels & 0xF0) >> 4));

						if (decompressed.Position % Width != 0)
							decompressed.WriteByte((byte)(twoPixels & 0x0F));
					}
				}
				else
				{
					var lineEndOffset = compressed.Position + compressedSize;

					while (compressed.Position < lineEndOffset)
					{
						var chunkSize = compressed.ReadUInt8();

						if (chunkSize < 0x80)
							decompressed.Position += chunkSize;
						else
						{
							var pixelCount = chunkSize - 0x80;

							if (has256Colors)
								decompressed.WriteArray(compressed.ReadBytes(pixelCount));
							else
							{
								var size = pixelCount / 2 + pixelCount % 2;

								for (var j = 0; j < size; j++)
								{
									var twoPixels = compressed.ReadUInt8();
									decompressed.WriteByte((byte)((twoPixels & 0xF0) >> 4));

									if (j + 1 < size || pixelCount % 2 == 0)
										decompressed.WriteByte((byte)(twoPixels & 0x0F));
								}
							}
						}
					}
				}

				decompressed.Position += (Width - decompressed.Position % Width) % Width;
			}
		}
	}
}

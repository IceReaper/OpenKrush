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

namespace OpenRA.Mods.OpenKrush.Utils
{
	using System;
	using System.IO;
	using System.Text;

	public static class Decompressor
	{
		public static Stream Decompress(Stream compressed)
		{
			compressed.ReadUInt32(); // version
			compressed.ReadUInt32(); // timestamp
			var uncompressedSizeData = (compressed.ReadByte() << 24) | (compressed.ReadByte() << 16) | (compressed.ReadByte() << 8) | compressed.ReadByte();
			compressed.Position += 4; // RRLC length.

			var uncompressedData = new byte[8 + uncompressedSizeData];
			var uncompressedStream = new MemoryStream(uncompressedData);

			uncompressedStream.WriteArray(Encoding.ASCII.GetBytes("DATA"));
			var tmp = BitConverter.GetBytes(uncompressedSizeData);
			Array.Reverse(tmp);
			uncompressedStream.WriteArray(tmp);
			Decompressor.Decompress(compressed, uncompressedStream, uncompressedSizeData);

			uncompressedStream.Position = 0;

			return uncompressedStream;
		}

		private static void Decompress(Stream compressed, Stream uncompressed, int length)
		{
			// TODO figure out which compression this is.
			while (uncompressed.Position < length)
			{
				var uncompressedSize = compressed.ReadUInt32();
				var compressedSize = compressed.ReadUInt32();

				if (compressedSize == uncompressedSize)
					uncompressed.WriteArray(compressed.ReadBytes((int)compressedSize));
				else
				{
					var chunkEndOffset = compressed.Position + compressedSize;

					while (compressed.Position < chunkEndOffset)
					{
						var bitmasks = compressed.ReadBytes(2);

						for (var i = 0; i < 16; i++)
						{
							if ((bitmasks[i / 8] & 1 << i % 8) == 0)
								uncompressed.WriteByte(compressed.ReadUInt8());
							else
							{
								var metaBytes = compressed.ReadBytes(2);
								var total = 1 + (metaBytes[0] & 0x000F);
								var offset = (metaBytes[0] & 0x00F0) << 4 | metaBytes[1];

								for (var bytesLeft = total; bytesLeft > 0;)
								{
									var amount = Math.Min(bytesLeft, offset);

									uncompressed.Position -= offset;
									var data = uncompressed.ReadBytes(amount);

									uncompressed.Position += offset - amount;
									uncompressed.WriteArray(data);

									bytesLeft -= offset;
								}
							}

							if (compressed.Position == chunkEndOffset)
								break;
						}
					}
				}
			}
		}
	}
}

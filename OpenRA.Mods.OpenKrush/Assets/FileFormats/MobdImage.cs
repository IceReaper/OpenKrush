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

	public class MobdImage
	{
		public readonly int Width;
		public readonly int Height;
		public readonly byte[] Pixels;

		public MobdImage(Stream stream, uint flags, GameFormat gameFormat)
		{
			bool flipped;
			this.Width = stream.ReadInt32();
			this.Height = stream.ReadInt32();
			this.Pixels = new byte[this.Width * this.Height];

			if (gameFormat == GameFormat.Gen1)
			{
				flipped = (flags & 0x1) == 1;

				var isCompressed = stream.ReadUInt8() == 2;

				if (isCompressed)
					this.DecompressGen1(stream);
				else
					stream.ReadBytes(this.Pixels, 0, this.Pixels.Length);
			}
			else
			{
				flipped = ((flags >> 31) & 0x1) == 1;
				var isCompressed = ((flags >> 27) & 0x1) == 1;
				var has256Colors = ((flags >> 26) & 0x1) == 1;

				if (isCompressed)
					this.DecompressGen2(stream, has256Colors);
				else
					stream.ReadBytes(this.Pixels, 0, this.Pixels.Length);
			}

			if (!flipped)
				return;

			for (var i = 0; i < this.Height; i++)
				Array.Reverse(this.Pixels, i * this.Width, this.Width);
		}

		private void DecompressGen1(Stream compressed)
		{
			var decompressed = new MemoryStream(this.Pixels);

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

				decompressed.Position += (this.Width - decompressed.Position % this.Width) % this.Width;
			}
		}

		private void DecompressGen2(Stream compressed, bool has256Colors)
		{
			var decompressed = new MemoryStream(this.Pixels);

			while (decompressed.Position < decompressed.Capacity)
			{
				int compressedSize = has256Colors ? compressed.ReadUInt16() : compressed.ReadUInt8();

				if (compressedSize == 0)
					decompressed.Position += this.Width;
				else if (!has256Colors && compressedSize > 0x80)
				{
					var pixelCount = compressedSize - 0x80;

					for (var i = 0; i < pixelCount; i++)
					{
						var twoPixels = compressed.ReadUInt8();
						decompressed.WriteByte((byte)((twoPixels & 0xF0) >> 4));

						if (decompressed.Position % this.Width != 0)
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

				decompressed.Position += (this.Width - decompressed.Position % this.Width) % this.Width;
			}
		}
	}
}

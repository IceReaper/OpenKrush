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

using System;
using System.IO;
using System.Text;

namespace OpenRA.Mods.Kknd.FileSystem
{
	public class Crypter
	{
		public static Stream Decrypt(Stream encryptedStream)
		{
			encryptedStream.ReadUInt32(); // version
			encryptedStream.ReadUInt32(); // timestamp

			var tmp = encryptedStream.ReadBytes(4);
			var decryptedSizeData = (tmp[0] << 24) | (tmp[1] << 16) | (tmp[2] << 8) | tmp[3];

			/*// RRLC is for memory relocation when loading archive into memory. We do not need this at all.
			var metaOffset = encryptedStream.ReadUInt32() + 16;
			encryptedStream.Position = metaOffset;
			var decryptedSizeMeta = encryptedStream.ReadUInt32();*/

			var decryptedData = new byte[8 + decryptedSizeData/* + 8 + decryptedSizeMeta*/];
			var decryptedStream = new MemoryStream(decryptedData);

			decryptedStream.WriteArray(Encoding.ASCII.GetBytes("DAT2"));
			tmp = BitConverter.GetBytes(decryptedSizeData);
			Array.Reverse(tmp);
			decryptedStream.WriteArray(tmp);
			encryptedStream.Position = 16;
			ParseBody(encryptedStream, decryptedStream, decryptedSizeData);

			/*decryptedStream.WriteArray(Encoding.ASCII.GetBytes("RRLC"));
			decryptedStream.WriteArray(BitConverter.GetBytes(decryptedSizeMeta));
			encryptedStream.Position = metaOffset + 8;
			ParseBody(encryptedStream, decryptedStream, (int)decryptedSizeMeta);*/

			decryptedStream.Position = 0;

			return decryptedStream;
		}

		static void ParseBody(Stream encryptedStream, Stream decryptedStream, int length)
		{
			while (decryptedStream.Position < length)
			{
				var chunkDecryptedSize = encryptedStream.ReadUInt32();
				var chunkEncryptedSize = encryptedStream.ReadUInt32();

				if (chunkEncryptedSize == chunkDecryptedSize)
					decryptedStream.WriteArray(encryptedStream.ReadBytes((int)chunkEncryptedSize));
				else
				{
					var chunkEndOffset = encryptedStream.Position + chunkEncryptedSize;

					while (encryptedStream.Position < chunkEndOffset)
					{
						var bitmasks = encryptedStream.ReadBytes(2);

						for (var i = 0; i < 16; i++)
						{
							if ((bitmasks[i / 8] & 1 << i % 8) == 0)
								decryptedStream.WriteArray(encryptedStream.ReadBytes(1));
							else
							{
								var metaBytes = encryptedStream.ReadBytes(2);
								var readSize = 1 + (metaBytes[0] & 0x000F);
								var readOffset = (metaBytes[0] & 0x00F0) << 4 | metaBytes[1];
								var substitutes = new byte[readSize];
								var returnPosition = decryptedStream.Position;

								for (var j = 0; j < readSize; j++)
								{
									decryptedStream.Position = returnPosition - readOffset + j % readOffset;
									substitutes[j] = decryptedStream.ReadUInt8();
								}

								decryptedStream.Position = returnPosition;
								decryptedStream.WriteArray(substitutes);
							}

							if (encryptedStream.Position == chunkEndOffset)
								break;
						}
					}
				}
			}
		}
	}
}

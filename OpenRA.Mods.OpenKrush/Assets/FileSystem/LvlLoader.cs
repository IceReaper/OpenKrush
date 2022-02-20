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

namespace OpenRA.Mods.OpenKrush.Assets.FileSystem
{
	using FileFormats;
	using JetBrains.Annotations;
	using OpenRA.FileSystem;
	using Primitives;

	[UsedImplicitly]
	public class LvlPackageLoader : IPackageLoader
	{
		public bool TryParsePackage(Stream s, string filename, FileSystem context, out IReadOnlyPackage? package)
		{
			if (filename.EndsWith(".lpk") // Spritesheet container
				|| filename.EndsWith(".bpk") // Image container
				|| filename.EndsWith(".spk") // Sound set
				|| filename.EndsWith(".lps") // Singleplayer map
				|| filename.EndsWith(".lpm") // Multiplayer map
				|| filename.EndsWith(".mpk")) // Matrix set (destroyable map part, tile replacements)
				s = Decompressor.Decompress(s);

			if (s.Position + 4 <= s.Length)
			{
				var signature = s.ReadASCII(4);
				s.Position -= 4;

				if (signature.Equals("DATA"))
				{
					package = new Lvl(
						new SegmentStream(s, 8, (s.ReadByte() << 24) | (s.ReadByte() << 16) | (s.ReadByte() << 8) | s.ReadByte()),
						filename,
						context
					);

					return true;
				}
			}

			package = null;

			return false;
		}
	}
}

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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.OpenKrush.FileSystem
{
	public enum Generation
	{
		Unknown,
		Gen1,
		Gen2
	}

	// TODO We need this weird hack, as some assets are using offsets relative to the container beginning!
	public class NonDisposingSegmentStream : SegmentStream
	{
		public NonDisposingSegmentStream(Stream stream, long offset, long count)
			: base(stream, offset, count)
		{
		}

		protected override void Dispose(bool disposing)
		{
		}
	}

	public class LvlPackageLoader : IPackageLoader
	{
		private class LvlPackage : IReadOnlyPackage
		{
			public string Name { get; }

			public IEnumerable<string> Contents => index.Keys;

			private readonly Dictionary<string, int[]> index = new Dictionary<string, int[]>();
			private readonly Stream stream;

			public LvlPackage(Stream s, string filename, IReadOnlyFileSystem context)
			{
				stream = s;
				Name = filename;

				var lvlLookup = new Dictionary<string, string>();
				var lookupPath = $"archives/{Path.GetFileName(filename)}.yaml";

				Stream s2;
				if (context.TryOpen(lookupPath, out s2))
					lvlLookup = MiniYaml.FromStream(s2).ToDictionary(x => x.Key, x => x.Value.Value);

				var fileTypeListOffset = s.ReadInt32();
				s.Position = fileTypeListOffset;

				int firstFileListOffset = 0;

				for (var i = 0; s.Position < s.Length; i++)
				{
					s.Position = fileTypeListOffset + i * 8;

					var fileType = s.ReadASCII(4);
					var fileListOffset = s.ReadInt32();

					// List terminator reached.
					if (fileListOffset == 0)
						break;

					// We need this to calculate the last fileLength.
					if (firstFileListOffset == 0)
						firstFileListOffset = fileListOffset;

					// To determine when this list ends, check the next entry
					s.Position += 4;
					var fileListEndOffset = s.ReadInt32();

					// List terminator reached, so assume the list goes on till the fileTypeList starts.
					if (fileListEndOffset == 0)
						fileListEndOffset = fileTypeListOffset;

					s.Position = fileListOffset;

					for (var j = 0; s.Position < fileListEndOffset; j++)
					{
						var fileOffset = s.ReadInt32();

						// Removed file, still increments fileId.
						if (fileOffset == 0)
							continue;

						// As the fileLength is nowhere stored, but files always follow in order, calculate the previous fileLength.
						if (index.Count > 0)
						{
							var entry = index.ElementAt(index.Count - 1).Value;
							entry[1] = fileOffset - entry[0];
						}

						var assetFileName = $"{j}.{fileType.ToLower()}";

						// Lookup assumed original filename for better readability in yaml files.
						if (lvlLookup.ContainsKey(assetFileName))
							assetFileName = lvlLookup[assetFileName];
						else
						{
							lvlLookup.Add(assetFileName, assetFileName);
						}

						index.Add(assetFileName, new int[] { fileOffset, 0 });
					}
				}

				if (index.Count <= 0)
					return;

				// Calculate the last fileLength.
				var lastEntry = index.ElementAt(index.Count - 1).Value;
				lastEntry[1] = firstFileListOffset - lastEntry[0];
			}

			public Stream GetStream(string filename)
			{
				return !index.TryGetValue(filename, out var entry) ? null : new NonDisposingSegmentStream(stream, entry[0], entry[1]);
			}

			public IReadOnlyPackage OpenPackage(string filename, FS context)
			{
				// Not implemented
				return null;
			}

			public bool Contains(string filename)
			{
				return index.ContainsKey(filename);
			}

			public void Dispose()
			{
				stream.Dispose();
			}
		}

		public bool TryParsePackage(Stream s, string filename, FS context, out IReadOnlyPackage package)
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
					package = new LvlPackage(new SegmentStream(s, 8, (s.ReadByte() << 24) | (s.ReadByte() << 16) | (s.ReadByte() << 8) | s.ReadByte()), filename, context);

					return true;
				}
			}

			package = null;
			return false;
		}
	}
}

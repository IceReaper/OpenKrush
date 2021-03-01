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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Kknd.FileSystem
{
	public enum Version
	{
		UNKNOWN,
		KKND1,
		KKND2
	}

	// TODO try to get rid of this, but something is disposing the stream!
	class NonDisposingSegmentStream : SegmentStream
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
		public sealed class LvlPackage : IReadOnlyPackage
		{
			public string Name { get; private set; }

			public IEnumerable<string> Contents
			{
				get { return index.Keys; }
			}

			private readonly Dictionary<string, uint[]> index = new Dictionary<string, uint[]>();
			private readonly Stream stream;

			public LvlPackage(Stream s, string filename, FS context)
			{
				stream = s;
				Name = filename;

				var lvlLookup = new Dictionary<string, string>();
				var updateLookup = false;

				Stream s2;
				if (context.TryOpen(filename + ".yaml", out s2))
					lvlLookup = MiniYaml.FromStream(s2).ToDictionary(x => x.Key, x => x.Value.Value);

				var fileTypeListOffset = s.ReadUInt32();
				s.Position = fileTypeListOffset;

				uint firstFileListOffset = 0;

				for (var i = 0; s.Position < s.Length; i++)
				{
					s.Position = fileTypeListOffset + i * 8;

					var fileType = s.ReadASCII(4);
					var fileListOffset = s.ReadUInt32();

					// List terminator reached.
					if (fileListOffset == 0)
						break;

					// We need this to calculate the last fileLength.
					if (firstFileListOffset == 0)
						firstFileListOffset = fileListOffset;

					// To determine when this list ends, check the next entry
					s.Position += 4;
					var fileListEndOffset = s.ReadUInt32();

					// List terminator reached, so assume the list goes on till the fileTypeList starts.
					if (fileListEndOffset == 0)
						fileListEndOffset = fileTypeListOffset;

					s.Position = fileListOffset;

					for (var j = 0; s.Position < fileListEndOffset; j++)
					{
						var fileOffset = s.ReadUInt32();

						// Removed file, still increments fileId.
						if (fileOffset == 0)
							continue;

						// As the fileLength is nowhere stored, but files always follow in order, calculate the previous fileLength.
						if (index.Count > 0)
						{
							var entry = index.ElementAt(index.Count - 1).Value;
							entry[1] = fileOffset - entry[0];
						}

						var assetFileName = j + "." + fileType.ToLower();

						// Lookup assumed original filename for better readability in yaml files.
						if (lvlLookup.ContainsKey(assetFileName))
							assetFileName = lvlLookup[assetFileName];
						else
						{
							lvlLookup.Add(assetFileName, assetFileName);
							updateLookup = true;
						}

						index.Add(assetFileName, new uint[] { fileOffset, 0 });
					}
				}

				// Calculate the last fileLength.
				if (index.Count > 0)
				{
					var entry = index.ElementAt(index.Count - 1).Value;
					entry[1] = firstFileListOffset - entry[0];
				}

				if (updateLookup)
					File.WriteAllText(filename + ".yaml",
						lvlLookup.Select(e => e.Key + ": " + e.Value).JoinWith("\n") + "\n");

				/*if (!Directory.Exists("XTRACT/" + filename))
					Directory.CreateDirectory("XTRACT/" + filename);

				foreach (var entry in index)
				{
					stream.Position = entry.Value[0];
					File.WriteAllBytes("XTRACT/" + filename + "/" + entry.Key, stream.ReadBytes((int)entry.Value[1]));
				}*/
			}

			public Stream GetStream(string filename)
			{
				uint[] entry;

				return !index.TryGetValue(filename, out entry) ? null : new NonDisposingSegmentStream(stream, entry[0], entry[1]);
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
				s = Crypter.Decrypt(s);

			var signature = s.ReadASCII(4);

			var version = Version.UNKNOWN;

			if (signature.Equals("DATA"))
				version = Version.KKND1;

			if (signature.Equals("DAT2"))
				version = Version.KKND2;

			if (version == Version.UNKNOWN)
			{
				s.Position -= 4;
				package = null;
				return false;
			}

			var tmp = s.ReadBytes(4); // Big-Endian
			var dataLength = (tmp[0] << 24) | (tmp[1] << 16) | (tmp[2] << 8) | tmp[3];

			package = new LvlPackage(new SegmentStream(s, 8, dataLength), filename, context);

			return true;
		}
	}
}

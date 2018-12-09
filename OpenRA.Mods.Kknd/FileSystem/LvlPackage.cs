using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Kknd.FileSystem
{
	public enum Version { KKND1, KKND2, UNKNOWN }

	public class LvlPackageLoader : IPackageLoader
	{
		class NonDisposingSegmentStream : SegmentStream
		{
			public NonDisposingSegmentStream(Stream stream, long offset, long count) : base(stream, offset, count) { }

			// TODO try to get rid of this, but something is disposing the stream!
			protected override void Dispose(bool disposing) { }
		}

		public sealed class LvlPackage : IReadOnlyPackage
		{
			// TODO replace by uint[] for performance
			public struct Entry
			{
				public readonly uint Offset;
				public readonly uint Length;

				public Entry(uint offset, uint length)
				{
					Offset = offset;
					Length = length;
				}
			}

			public string Name { get; private set; }
			public IEnumerable<string> Contents { get { return index.Keys; } }

			readonly Dictionary<string, Entry> index = new Dictionary<string, Entry>();
			readonly Stream s;

			public LvlPackage(Stream sBase, string filename, Dictionary<string, MiniYaml> lvlLookup)
			{
				sBase.ReadASCII(4); // DATA
				var tmp = sBase.ReadBytes(4); // Big-Endian
				var dataLength = (tmp[0] << 24) | (tmp[1] << 16) | (tmp[2] << 8) | tmp[3];
				s = new SegmentStream(sBase, 8, dataLength);
				Name = filename;

				var filetypesOffset = s.ReadUInt32();
				s.Position = filetypesOffset + 4;
				var firstFilesOffset = s.ReadUInt32();
				s.Position = filetypesOffset;

				while (true)
				{
					var filetype = s.ReadASCII(4);
					var filesOffset = s.ReadUInt32();
					var continueTypePosition = s.Position;
					s.Position += 4;
					var nextFilesOffset = s.ReadUInt32();
					s.Position = filesOffset;

					if (nextFilesOffset == 0)
						nextFilesOffset = filetypesOffset;

					for (var i = 0; s.Position < nextFilesOffset; i++)
					{
						var fileOffset = s.ReadUInt32();
						uint fileLength = 0;

						if (fileOffset != 0)
						{
							var continueFilePosition = s.Position;

							while (fileLength == 0)
							{
								if (s.Position == filetypesOffset)
									fileLength = firstFilesOffset - fileOffset;
								else
								{
									var nextFileOffset = s.ReadUInt32();

									if (nextFileOffset != 0)
										fileLength = nextFileOffset - fileOffset;
								}
							}

							s.Position = continueFilePosition;
						}

						if (fileLength <= 0)
							continue;

						var assetFileName = i + "." + (filetype.Equals("SOUN") ? "wav" : filetype.ToLower());

						if (lvlLookup.ContainsKey(filename + "|" + assetFileName))
							assetFileName = lvlLookup[filename + "|" + assetFileName].Value;

						index.Add(assetFileName, new Entry(fileOffset, fileLength));
					}

					s.Position = continueTypePosition;

					if (nextFilesOffset == filetypesOffset)
						break;
				}
			}

			public Stream GetStream(string filename)
			{
				Entry entry;
				
				if (!index.TryGetValue(filename, out entry))
					return null;

				return new NonDisposingSegmentStream(s, entry.Offset, entry.Length);
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
				s.Dispose();
			}
		}

		public bool TryParsePackage(Stream s, string filename, FS context, out IReadOnlyPackage package)
		{
			if (filename.EndsWith(".lpk"))
				s = Crypter.Decrypt(s);

			var signature = s.ReadASCII(4);
			s.Position -= 4;

			if (!signature.Equals("DATA") && !signature.Equals("DAT2"))
			{
				package = null;
				return false;
			}

			Stream lvlLookup;
			context.TryOpen("LvlLookup.yaml", out lvlLookup);

			package = new LvlPackage(s, filename, MiniYaml.FromStream(lvlLookup).ToDictionary(x => x.Key, x => x.Value));

			return true;
		}
	}
}

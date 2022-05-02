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

namespace OpenRA.Mods.OpenKrush.Assets.FileFormats;

using OpenRA.FileSystem;
using Primitives;

// Assets are using container offsets, passing container stream wrapped in a dispose protection to avoid assets disposing the container.  
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

public class Lvl : IReadOnlyPackage
{
	public string Name { get; }

	public IEnumerable<string> Contents => this.index.Keys;

	private readonly Dictionary<string, int[]> index = new();
	private readonly Stream stream;

	public Lvl(Stream stream, string filename, IReadOnlyFileSystem context)
	{
		this.stream = stream;
		this.Name = filename;

		var lvlLookup = new Dictionary<string, string>();
		var lookupPath = $"archives/{Path.GetFileName(filename.ToLower())}.yaml";

		if (context.TryOpen(lookupPath, out var lookupStream))
			lvlLookup = MiniYaml.FromStream(lookupStream).ToDictionary(node => node.Key, node => node.Value.Value);

		var fileTypeListOffset = stream.ReadInt32();
		stream.Position = fileTypeListOffset;

		var firstFileListOffset = 0;

		for (var i = 0; stream.Position < stream.Length; i++)
		{
			stream.Position = fileTypeListOffset + i * 8;

			var fileType = stream.ReadASCII(4);
			var fileListOffset = stream.ReadInt32();

			// List terminator reached.
			if (fileListOffset == 0)
				break;

			// We need this to calculate the last fileLength.
			if (firstFileListOffset == 0)
				firstFileListOffset = fileListOffset;

			// To determine when this list ends, check the next entry
			stream.Position += 4;
			var fileListEndOffset = stream.ReadInt32();

			// List terminator reached, so assume the list goes on till the fileTypeList starts.
			if (fileListEndOffset == 0)
				fileListEndOffset = fileTypeListOffset;

			stream.Position = fileListOffset;

			for (var j = 0; stream.Position < fileListEndOffset; j++)
			{
				var fileOffset = stream.ReadInt32();

				// Removed file, still increments fileId.
				if (fileOffset == 0)
					continue;

				// As the fileLength is nowhere stored, but files always follow in order, calculate the previous fileLength.
				if (this.index.Count > 0)
				{
					var entry = this.index.ElementAt(this.index.Count - 1).Value;
					entry[1] = fileOffset - entry[0];
				}

				var assetFileName = $"{j}.{fileType.ToLower()}";

				// Lookup assumed original filename for better readability in yaml files.
				if (lvlLookup.ContainsKey(assetFileName))
					assetFileName = lvlLookup[assetFileName];
				else
					lvlLookup.Add(assetFileName, assetFileName);

				this.index.Add(assetFileName, new[] { fileOffset, 0 });
			}
		}

		if (this.index.Count <= 0)
			return;

		// Calculate the last fileLength.
		var lastEntry = this.index.ElementAt(this.index.Count - 1).Value;
		lastEntry[1] = firstFileListOffset - lastEntry[0];
	}

	public Stream? GetStream(string filename)
	{
		return !this.index.TryGetValue(filename, out var entry) ? null : new NonDisposingSegmentStream(this.stream, entry[0], entry[1]);
	}

	public IReadOnlyPackage? OpenPackage(string filename, FileSystem context)
	{
		// Not implemented
		return null;
	}

	public bool Contains(string filename)
	{
		return this.index.ContainsKey(filename);
	}

	public void Dispose()
	{
		this.stream.Dispose();
		GC.SuppressFinalize(this);
	}
}

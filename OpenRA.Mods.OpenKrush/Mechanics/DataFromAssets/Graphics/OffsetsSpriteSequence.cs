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

namespace OpenRA.Mods.OpenKrush.Mechanics.DataFromAssets.Graphics
{
	using Common.Graphics;
	using Common.SpriteLoaders;
	using JetBrains.Annotations;
	using OpenRA.Graphics;
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class Offset
	{
		public readonly int Id;
		public readonly int X;
		public readonly int Y;

		public Offset(int id, int x, int y)
		{
			this.Id = id;
			this.X = x;
			this.Y = y;
		}
	}

	public class EmbeddedSpriteOffsets
	{
		public readonly Dictionary<int, Offset[]> FrameOffsets;

		public EmbeddedSpriteOffsets(Dictionary<int, Offset[]> frameOffsets)
		{
			this.FrameOffsets = frameOffsets;
		}
	}

	[UsedImplicitly]
	public class OffsetsSpriteSequenceLoader : DefaultSpriteSequenceLoader
	{
		public OffsetsSpriteSequenceLoader(ModData modData)
			: base(modData)
		{
		}

		public override ISpriteSequence CreateSequence(ModData modData, string tileSet, SpriteCache cache, string sequence, string animation, MiniYaml info)
		{
			return new OffsetsSpriteSequence(modData, tileSet, cache, this, sequence, animation, info);
		}
	}

	public sealed class OffsetsSpriteSequence : DefaultSpriteSequence
	{
		public readonly Dictionary<Sprite, Offset[]> EmbeddedOffsets = new();

		public OffsetsSpriteSequence(
			ModData modData,
			string tileSet,
			SpriteCache cache,
			ISpriteSequenceLoader loader,
			string sequence,
			string animation,
			MiniYaml info
		)
			: base(modData, tileSet, cache, loader, sequence, animation, info)
		{
			if (info.Value.EndsWith(".mobd"))
			{
				var src = this.GetSpriteSrc(modData, tileSet, sequence, animation, info.Value, info.ToDictionary());
				var offsets = cache.FrameMetadata(src).Get<EmbeddedSpriteOffsets>();

				for (var i = 0; i < this.sprites.Length; i++)
				{
					if (this.sprites[i] == null)
						continue;

					if (offsets.FrameOffsets != null && offsets.FrameOffsets.ContainsKey(i))
						this.EmbeddedOffsets.Add(this.sprites[i], offsets.FrameOffsets[i]);
				}
			}
			else if (info.Value.EndsWith(".png"))
			{
				var src = this.GetSpriteSrc(modData, tileSet, sequence, animation, info.Value, info.ToDictionary());
				var metadata = cache.FrameMetadata(src).Get<PngSheetMetadata>();

				for (var i = 0; i < this.sprites.Length; i++)
				{
					if (this.sprites[i] == null || !metadata.Metadata.ContainsKey($"Offsets[{i}]"))
						continue;

					var lines = metadata.Metadata[$"Offsets[{i}]"].Split('|');
					var convertOffsets = new Func<string[], Offset>(data => new(int.Parse(data[0]), int.Parse(data[1]), int.Parse(data[2])));
					this.EmbeddedOffsets.Add(this.sprites[i], lines.Select(t => t.Split(',')).Select(convertOffsets).ToArray());
				}
			}
		}
	}
}

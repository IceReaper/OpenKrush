using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.SpriteLoaders;
using OpenRA.Mods.Kknd.SpriteLoaders;

namespace OpenRA.Mods.Kknd.Graphics
{
	public class Offset
	{
		public int Id;
		public int X;
		public int Y;
	}
	
	public class EmbeddedSpriteOffsets
	{
		public readonly Dictionary<int, Offset[]> FrameOffsets;
		
		public EmbeddedSpriteOffsets(Dictionary<int, Offset[]> frameOffsets = null)
		{
			FrameOffsets = frameOffsets;
		}
	}

	public class OffsetsSpriteSequenceLoader : DefaultSpriteSequenceLoader
	{
		public OffsetsSpriteSequenceLoader(ModData modData) : base(modData) { }

		public override ISpriteSequence CreateSequence(ModData modData, TileSet tileSet, SpriteCache cache, string sequence, string animation, MiniYaml info)
		{
			return new OffsetsSpriteSequence(modData, tileSet, cache, this, sequence, animation, info);
		}
	}

	public class OffsetsSpriteSequence : DefaultSpriteSequence
	{
		public Dictionary<Sprite, Offset[]> EmbeddedOffsets = new Dictionary<Sprite, Offset[]>();

		public OffsetsSpriteSequence(ModData modData, TileSet tileSet, SpriteCache cache, ISpriteSequenceLoader loader, string sequence, string animation, MiniYaml info)
			: base(modData, tileSet, cache, loader, sequence, animation, info)
		{
			if (info.Value.EndsWith(".mobd"))
			{
				var src = GetSpriteSrc(modData, tileSet, sequence, animation, info.Value, info.ToDictionary());
				var offsets = cache.FrameMetadata(src).Get<EmbeddedSpriteOffsets>();
				
				for (var i = 0; i < sprites.Length; i++)
				{
					if (sprites[i] == null)
						continue;

					if (offsets.FrameOffsets != null && offsets.FrameOffsets.ContainsKey(i))
						EmbeddedOffsets.Add(sprites[i], offsets.FrameOffsets[i]);
				}
			}
			else if (info.Value.EndsWith(".png"))
			{
				var src = GetSpriteSrc(modData, tileSet, sequence, animation, info.Value, info.ToDictionary());
				var metadata = cache.FrameMetadata(src).Get<PngSheetMetadata>();
				
				for (var i = 0; i < sprites.Length; i++)
				{
					if (sprites[i] == null || !metadata.Metadata.ContainsKey("Offsets[" + i + "]"))
						continue;

					var lines = metadata.Metadata["Offsets[" + i + "]"].Split('\n');
					EmbeddedOffsets.Add(sprites[i], lines.Select(t => t.Split(',')).Select(data => new Offset
					{
						Id = int.Parse(data[0]),
						X = int.Parse(data[1]),
						Y = int.Parse(data[2])
					}).ToArray());
				}
			}
		}
	}
}

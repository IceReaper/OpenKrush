using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.FileFormats;
using OpenRA.Primitives;
using OpenRA.Traits;
using Version = OpenRA.Mods.Kknd.FileSystem.Version;

namespace OpenRA.Mods.Kknd.Traits.Render
{
	// TODO see PaletteFromEmbeddedSpritePalette
	[Desc("Load palette from KKnD2 MOBD file.")]
	class PaletteFromMobdInfo : ITraitInfo, IProvidesCursorPaletteInfo
	{
		[FieldLoader.Require, PaletteDefinition]
		[Desc("Internal palette name")]
		public readonly string Name = null;

		[FieldLoader.Require]
		[Desc("Filename to load")]
		public readonly string Filename = null;

		public readonly bool AllowModifiers = true;

		public object Create(ActorInitializer init) { return new PaletteFromMobd(init.World, this); }

		string IProvidesCursorPaletteInfo.Palette { get { return Name; } }
		ImmutablePalette IProvidesCursorPaletteInfo.ReadPalette(IReadOnlyFileSystem fileSystem)
		{
			var colors = new uint[Palette.Size];

			var mobd = new Mobd(fileSystem.Open(Filename) as SegmentStream, Version.KKND2);
			var frame = mobd.Animations.FirstOrDefault(a => a.Frames.Length > 0) ?? mobd.HardcodedAnimations.FirstOrDefault(a => a.Frames.Length > 0);
			var palette = frame.Frames.First().RenderFlags.Palette;

			// TODO when we have all kknd2 palettes done, remove this. It will simply extract palettes for easier determination of player indices.
			/*if (Filename != null) {
				var bitmap = new Bitmap(palette.Length / 8, 8);
				for (var i = 1; i < palette.Length; i++)
					bitmap.SetPixel(i % bitmap.Width, i / bitmap.Width, palette[i]);
				bitmap.Save(Filename.Substring(8) + ".png");
				bitmap.Dispose();
			}*/

			for (var i = 1; i < Math.Min(colors.Length, palette.Length); i++)
				colors[i] = palette[i];

			return new ImmutablePalette(colors);
		}
	}

	class PaletteFromMobd : ILoadsPalettes, IProvidesAssetBrowserPalettes
	{
		readonly World world;
		readonly PaletteFromMobdInfo info;

		public PaletteFromMobd(World world, PaletteFromMobdInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			wr.AddPalette(info.Name, ((IProvidesCursorPaletteInfo)info).ReadPalette(world.Map), info.AllowModifiers);
		}

		public IEnumerable<string> PaletteNames { get { yield return info.Name; } }
	}
}

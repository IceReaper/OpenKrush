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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.OpenKrush.FileFormats;
using OpenRA.Mods.OpenKrush.FileSystem;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.OpenKrush.Traits.Render
{
	// TODO see PaletteFromEmbeddedSpritePalette
	[Desc("Load palette from MOBD file.")]
	public class PaletteFromMobdInfo : TraitInfo, IProvidesCursorPaletteInfo
	{
		[FieldLoader.RequireAttribute]
		[PaletteDefinition]
		[Desc("Internal palette name")]
		public readonly string Name = null;

		[FieldLoader.RequireAttribute]
		[Desc("Filename to load")]
		public readonly string Filename = null;

		public readonly bool AllowModifiers = true;

		public override object Create(ActorInitializer init) { return new PaletteFromMobd(init.World, this); }

		string IProvidesCursorPaletteInfo.Palette { get { return Name; } }
		ImmutablePalette IProvidesCursorPaletteInfo.ReadPalette(IReadOnlyFileSystem fileSystem)
		{
			var colors = new uint[Palette.Size];

			var mobd = new Mobd(fileSystem.Open(Filename) as SegmentStream, Generation.Gen2);
			var frame = mobd.RotationalAnimations.FirstOrDefault(a => a.Frames.Length > 0) ?? mobd.SimpleAnimations.FirstOrDefault(a => a.Frames.Length > 0);
			var palette = frame.Frames.First().RenderFlags.Palette;

			for (var i = 1; i < Math.Min(colors.Length, palette.Length); i++)
				colors[i] = palette[i];

			return new ImmutablePalette(colors);
		}
	}

	public class PaletteFromMobd : ILoadsPalettes, IProvidesAssetBrowserPalettes
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

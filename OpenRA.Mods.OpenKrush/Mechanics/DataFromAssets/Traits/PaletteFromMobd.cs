#region Copyright & License Information

/*
 * Copyright 2007-2022 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Mechanics.DataFromAssets.Traits;

using Assets.FileFormats;
using Common.Traits;
using FileSystem;
using JetBrains.Annotations;
using OpenRA.Graphics;
using OpenRA.Traits;
using Primitives;

// TODO see PaletteFromEmbeddedSpritePalette
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("Load palette from MOBD file.")]
public class PaletteFromMobdInfo : TraitInfo, IProvidesCursorPaletteInfo
{
	[FieldLoader.RequireAttribute]
	[PaletteDefinition]
	[Desc("Internal palette name")]
	public readonly string Name = "";

	[FieldLoader.RequireAttribute]
	[Desc("Filename to load")]
	public readonly string Filename = "";

	public readonly bool AllowModifiers = true;

	public override object Create(ActorInitializer init)
	{
		return new PaletteFromMobd(init.World, this);
	}

	string IProvidesCursorPaletteInfo.Palette => this.Name;

	ImmutablePalette IProvidesCursorPaletteInfo.ReadPalette(IReadOnlyFileSystem fileSystem)
	{
		var colors = new uint[Palette.Size];

		if (fileSystem.Open(this.Filename) is not SegmentStream segmentStream)
			return new(colors);

		var mobd = new Mobd(segmentStream);
		var frame = mobd.RotationalAnimations.FirstOrDefault(a => a.Frames.Length > 0) ?? mobd.SimpleAnimations.FirstOrDefault(a => a.Frames.Length > 0);

		if (frame == null)
			return new(colors);

		var palette = frame.Frames.FirstOrDefault()?.RenderFlags.Palette;

		if (palette == null)
			return new(colors);

		for (var i = 1; i < Math.Min(colors.Length, palette.Length); i++)
			colors[i] = palette[i];

		return new(colors);
	}
}

public class PaletteFromMobd : ILoadsPalettes, IProvidesAssetBrowserPalettes
{
	private readonly World world;
	private readonly PaletteFromMobdInfo info;

	public PaletteFromMobd(World world, PaletteFromMobdInfo info)
	{
		this.world = world;
		this.info = info;
	}

	public void LoadPalettes(WorldRenderer wr)
	{
		wr.AddPalette(this.info.Name, ((IProvidesCursorPaletteInfo)this.info).ReadPalette(this.world.Map), this.info.AllowModifiers);
	}

	public IEnumerable<string> PaletteNames
	{
		get
		{
			yield return this.info.Name;
		}
	}
}

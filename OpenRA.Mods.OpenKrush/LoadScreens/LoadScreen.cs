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

namespace OpenRA.Mods.OpenKrush.LoadScreens;

using Common.LoadScreens;
using Graphics;
using JetBrains.Annotations;

[UsedImplicitly]
public class LoadScreen : BlankLoadScreen
{
	private Sheet? sheet;
	private Sprite? logo;

	public override void Init(ModData modData, Dictionary<string, string> info)
	{
		// TODO this is for MAPD only.
		Game.Settings.Graphics.SheetSize = 8192;

		base.Init(modData, info);

		this.sheet = new(SheetType.BGRA, modData.DefaultFileSystem.Open("uibits/splashscreen.png"));
		this.logo = new(this.sheet, new(0, 0, 640, 480), TextureChannel.RGBA);
	}

	public override void StartGame(Arguments args)
	{
		base.StartGame(args);

		this.sheet?.Dispose();

		this.sheet = new(SheetType.BGRA, this.ModData.DefaultFileSystem.Open("uibits/loadscreen.png"));
		this.logo = new(this.sheet, new(0, 0, 640, 480), TextureChannel.RGBA);
	}

	public override void Display()
	{
		var logoPos = new float2(Game.Renderer.Resolution.Width / 2 - 320, Game.Renderer.Resolution.Height / 2 - 240);

		Game.Renderer.BeginUI();
		Game.Renderer.RgbaSpriteRenderer.DrawSprite(this.logo, logoPos);
		Game.Renderer.EndFrame(new NullInputHandler());
	}

	protected override void Dispose(bool disposing)
	{
		this.sheet?.Dispose();

		base.Dispose(disposing);
	}
}

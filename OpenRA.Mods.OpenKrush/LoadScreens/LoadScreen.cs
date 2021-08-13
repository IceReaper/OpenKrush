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

namespace OpenRA.Mods.OpenKrush.LoadScreens
{
	using System.Collections.Generic;
	using Common.LoadScreens;
	using GameProviders;
	using OpenRA.Graphics;
	using Primitives;

	public class LoadScreen : BlankLoadScreen
	{
		private bool gameFound;
		private Renderer renderer;
		private Sheet sheet1;
		private Sheet sheet2;
		private Sheet sheet3;
		private Sprite logo1;
		private Sprite logo2;
		private Sprite logo3;
		private bool started;

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			// TODO this is for MAPD only.
			Game.Settings.Graphics.SheetSize = 8192;

			gameFound = InstallationFinder.FindInstallation(modData, Generation1.AppIdSteam, Generation1.AppIdGog);

			if (!gameFound)
			{
				var manifestType = modData.Manifest.GetType();
				manifestType.GetField(nameof(Manifest.Cursors))?.SetValue(modData.Manifest, new[] { "openkrush|cursors.yaml" });
			}

			base.Init(modData, info);

			renderer = Game.Renderer;

			if (renderer == null)
				return;

			sheet1 = new Sheet(SheetType.BGRA, modData.DefaultFileSystem.Open("uibits/loading_game.png"));
			sheet2 = new Sheet(SheetType.BGRA, modData.DefaultFileSystem.Open("uibits/loading_map.png"));
			sheet3 = new Sheet(SheetType.BGRA, modData.DefaultFileSystem.Open("uibits/missing_installation.png"));
			logo1 = new Sprite(sheet1, new Rectangle(0, 0, 640, 480), TextureChannel.RGBA);
			logo2 = new Sprite(sheet2, new Rectangle(0, 0, 640, 480), TextureChannel.RGBA);
			logo3 = new Sprite(sheet3, new Rectangle(0, 0, 640, 480), TextureChannel.RGBA);
		}

		public override void StartGame(Arguments args)
		{
			if (!gameFound)
			{
				Game.InitializeMod(ModData.Manifest.Id, Arguments.Empty);
				return;
			}

			typeof(Ruleset).GetField("Music")?.SetValue(ModData.DefaultRules, GameProvider.BuildMusicDictionary());
			base.StartGame(args);
			started = true;
		}

		public override void Display()
		{
			if (renderer == null)
				return;

			var logoPos = new float2(renderer.Resolution.Width / 2 - 320, renderer.Resolution.Height / 2 - 240);

			renderer.BeginUI();
			renderer.RgbaSpriteRenderer.DrawSprite(!gameFound ? logo3 : started ? logo2 : logo1, logoPos);
			renderer.EndFrame(new NullInputHandler());
		}

		protected override void Dispose(bool disposing)
		{
			sheet1.Dispose();
			sheet2.Dispose();
			base.Dispose(disposing);
		}
	}
}

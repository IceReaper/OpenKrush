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
	using Common.LoadScreens;
	using Graphics;
	using InstallationFinder;
	using JetBrains.Annotations;

	[UsedImplicitly]
	public class LoadScreen : BlankLoadScreen
	{
		private Installation? gen1;
		private Installation? gen2;
		private Renderer? renderer;
		private Sheet? sheet;
		private Sprite? logo;
		private bool canRun;

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			// TODO this is for MAPD only.
			Game.Settings.Graphics.SheetSize = 8192;

			this.gen1 = InstallationFinder.RegisterInstallation(modData, new Generation1());
			this.gen2 = InstallationFinder.RegisterInstallation(modData, new Generation2());

			var game = modData.Manifest.Id switch
			{
				"openkrush_gen1" => this.gen1,
				"openkrush_gen2" => this.gen2,
				_ => null
			};

			if (game == null)
			{
				var manifestType = modData.Manifest.GetType();
				manifestType.GetField(nameof(Manifest.Cursors))?.SetValue(modData.Manifest, new[] { "openkrush|cursors.yaml" });
			}
			else
				this.canRun = true;

			base.Init(modData, info);

			this.renderer = Game.Renderer;

			if (this.renderer == null)
				return;

			this.sheet = new(SheetType.BGRA, modData.DefaultFileSystem.Open(this.canRun ? "uibits/loading_game.png" : "uibits/missing_installation.png"));
			this.logo = new(this.sheet, new(0, 0, 640, 480), TextureChannel.RGBA);
		}

		public override void StartGame(Arguments args)
		{
			if (!this.canRun)
			{
				Game.InitializeMod(this.ModData.Manifest.Id, Arguments.Empty);

				return;
			}

			typeof(Ruleset).GetField("Music")
				?.SetValue(
					this.ModData.DefaultRules,
					InstallationUtils.BuildMusicDictionary(
						this.ModData.Manifest.Id switch
						{
							"openkrush_gen1" => this.gen1,
							"openkrush_gen2" => this.gen2,
							_ => null
						}
					)
				);

			base.StartGame(args);

			this.sheet?.Dispose();
			this.sheet = new(SheetType.BGRA, this.ModData.DefaultFileSystem.Open("uibits/loading_map.png"));
			this.logo = new(this.sheet, new(0, 0, 640, 480), TextureChannel.RGBA);
		}

		public override void Display()
		{
			if (this.renderer == null)
				return;

			var logoPos = new float2(this.renderer.Resolution.Width / 2 - 320, this.renderer.Resolution.Height / 2 - 240);

			this.renderer.BeginUI();
			this.renderer.RgbaSpriteRenderer.DrawSprite(this.logo, logoPos);
			this.renderer.EndFrame(new NullInputHandler());
		}

		protected override void Dispose(bool disposing)
		{
			this.sheet?.Dispose();
			base.Dispose(disposing);
		}
	}
}

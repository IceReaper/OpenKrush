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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using OpenRA.Graphics;
using OpenRA.Mods.Common.LoadScreens;
using OpenRA.Mods.OpenKrush.GameProviders;
using OpenRA.Primitives;

namespace OpenRA.Mods.OpenKrush.LoadScreens
{
	public class LoadScreen : BlankLoadScreen
	{
		private Renderer renderer;
		private Sheet sheet1;
		private Sheet sheet2;
		private Sprite logo1;
		private Sprite logo2;
		private bool started;

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			Game.Settings.Graphics.SheetSize = 8192;

			if (!FindInstallation(modData))
				throw new Exception("NO GAME INSTALLATION FOUND");

			base.Init(modData, info);

			renderer = Game.Renderer;
			if (renderer == null)
				return;

			sheet1 = new Sheet(SheetType.BGRA, modData.DefaultFileSystem.Open("uibits/loading_game.png"));
			sheet2 = new Sheet(SheetType.BGRA, modData.DefaultFileSystem.Open("uibits/loading_map.png"));
			logo1 = new Sprite(sheet1, new Rectangle(0, 0, 640, 480), TextureChannel.RGBA);
			logo2 = new Sprite(sheet2, new Rectangle(0, 0, 640, 480), TextureChannel.RGBA);
		}

		public override void StartGame(Arguments args)
		{
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
			renderer.RgbaSpriteRenderer.DrawSprite(started ? logo2 : logo1, logoPos);
			renderer.EndFrame(new NullInputHandler());
		}

		protected override void Dispose(bool disposing)
		{
			sheet1.Dispose();
			sheet2.Dispose();
			base.Dispose(disposing);
		}

		private static bool FindInstallation(ModData modData)
		{
			return FindSteamInstallation(modData) || FindGoGInstallation(modData) || FindCdInDrive(modData);
		}

		private static bool FindSteamInstallation(ModData modData)
		{
			foreach (var steamDirectory in SteamDirectory())
			{
				var manifestPath = Path.Combine(steamDirectory, "steamapps", "appmanifest_1292170.acf");

				if (!File.Exists(manifestPath))
					continue;

				var data = ParseKeyValuesManifest(manifestPath);

				if (!data.TryGetValue("StateFlags", out var stateFlags) || stateFlags != "4")
					continue;

				if (!data.TryGetValue("installdir", out var installDir))
					continue;

				installDir = Path.Combine(steamDirectory, "steamapps", "common", installDir);

				Log.Write("debug", $"Steam version candidate: {installDir}");

				if (GameProvider.TryRegister(modData, installDir))
					return true;
			}

			Log.Write("debug", "Steam version not found");

			return false;
		}

		private static bool FindGoGInstallation(ModData modData)
		{
			var prefixes = new[] { "HKEY_LOCAL_MACHINE\\Software\\", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\" };

			foreach (var prefix in prefixes)
			{
				var installDir = Microsoft.Win32.Registry.GetValue($"{prefix}GOG.com\\Games\\1207659107", "path", null) as string;

				if (installDir == null)
					continue;

				Log.Write("debug", $"GoG version candidate: {installDir}");

				if (GameProvider.TryRegister(modData, installDir))
					return true;
			}

			Log.Write("debug", "GoG version not found");

			return false;
		}

		private static bool FindCdInDrive(ModData modData)
		{
			foreach (var driveInfo in DriveInfo.GetDrives())
			{
				if (driveInfo.DriveType != DriveType.CDRom || !driveInfo.IsReady)
					continue;

				var installDir = driveInfo.RootDirectory.FullName;

				Log.Write("debug", $"CD version candidate: {installDir}");

				if (GameProvider.TryRegister(modData, installDir))
					return true;
			}

			Log.Write("debug", "CD version not found");

			return false;
		}

		private static IEnumerable<string> SteamDirectory()
		{
			var candidatePaths = new List<string>();

			if (Platform.CurrentPlatform == PlatformType.Windows)
			{
				var prefixes = new[] { "HKEY_LOCAL_MACHINE\\Software\\", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\" };

				foreach (var prefix in prefixes)
					if (Microsoft.Win32.Registry.GetValue($"{prefix}Valve\\Steam", "InstallPath", null) is string path)
						candidatePaths.Add(path);
			}
			else if (Platform.CurrentPlatform == PlatformType.OSX)
			{
				candidatePaths.Add(Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					"Library", "Application Support", "Steam"));
			}
			else
			{
				candidatePaths.Add(Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
					".steam", "root"));
			}

			foreach (var libraryPath in candidatePaths.Where(Directory.Exists))
			{
				yield return libraryPath;

				var libraryFoldersPath = Path.Combine(libraryPath, "steamapps", "libraryfolders.vdf");

				if (!File.Exists(libraryFoldersPath))
					continue;

				var data = ParseKeyValuesManifest(libraryFoldersPath);

				for (var i = 1; ; i++)
				{
					if (!data.TryGetValue(i.ToString(), out var path))
						break;

					yield return path;
				}
			}
		}

		private static Dictionary<string, string> ParseKeyValuesManifest(string path)
		{
			var regex = new Regex("^\\s*\"(?<key>[^\"]*)\"\\s*\"(?<value>[^\"]*)\"\\s*$");
			var result = new Dictionary<string, string>();

			using (var s = new FileStream(path, FileMode.Open))
			{
				foreach (var line in s.ReadAllLines())
				{
					var match = regex.Match(line);

					if (match.Success)
						result[match.Groups["key"].Value] = match.Groups["value"].Value;
				}
			}

			return result;
		}
	}
}

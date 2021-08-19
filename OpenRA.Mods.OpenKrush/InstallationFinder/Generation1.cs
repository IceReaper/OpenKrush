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

namespace OpenRA.Mods.OpenKrush.InstallationFinder
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public class Generation1 : IGeneration
	{
		public int SteamAppId => 1292170;
		public int GogAppId => 1207659107;

		public Installation? TryRegister(string path)
		{
			var executablePath = path;

			// Xtreme GoG and Steam
			var executable = InstallationUtils.GetFile(executablePath, "kkndgame.exe");

			if (executable == null)
			{
				// Xtreme CD
				executablePath = InstallationUtils.GetDirectory(path, "game");

				if (executablePath == null)
				{
					// Dos CD
					executablePath = InstallationUtils.GetDirectory(path, "kknd");

					if (executablePath == null)
						return null;
				}

				executable = InstallationUtils.GetFile(executablePath, "kknd.exe");

				if (executable == null)
					return null;
			}

			Log.Write("debug", $"Detected installation: {path}");

			var release = CryptoUtil.SHA1Hash(File.OpenRead(executable));
			var isXtreme = true;

			switch (release)
			{
				case "d1f41d7129b6f377869f28b89f92c18f4977a48f":
					Log.Write("debug", "=> Krush, Kill 'N' Destroy Xtreme (Steam/GoG, English)");

					break;

				case "6fb10d85739ef63b28831ada4cdfc159a950c5d2":
					Log.Write("debug", "=> Krush, Kill 'N' Destroy Xtreme (Disc, English)");

					break;

				case "024e96860c504b462b24b9237d49bfe8de6eb8e0":
					Log.Write("debug", "=> Krush, Kill 'N' Destroy (Disc, English)");
					isXtreme = false;
					path = executablePath;

					break;

				default:
					Log.Write("debug", "=> Unsupported game version");

					return null;
			}

			var levelsFolder = InstallationUtils.GetDirectory(path, "levels");

			if (levelsFolder == null)
			{
				Log.Write("debug", "=> Missing folder: levels");

				return null;
			}

			var fmvFolder = InstallationUtils.GetDirectory(path, "fmv");

			if (fmvFolder == null)
			{
				Log.Write("debug", "=> Missing folder: fmv");

				return null;
			}

			var graphicsFolder = InstallationUtils.GetDirectory(levelsFolder, "640");

			if (graphicsFolder == null)
			{
				Log.Write("debug", "=> Missing folder: 640");

				return null;
			}

			// Required files.
			var files =
				new Dictionary<string, string>
				{
					{ "sprites.lvl", graphicsFolder },
					{ "surv.slv", levelsFolder },
					{ "mute.slv", levelsFolder },
					{ "mh_fmv.vbc", fmvFolder },
					{ "intro.vbc", fmvFolder }
				}.Concat(
					isXtreme
						? new()
						{
							{ "surv1.wav", levelsFolder },
							{ "surv2.wav", levelsFolder },
							{ "surv3.wav", levelsFolder },
							{ "surv4.wav", levelsFolder },
							{ "mute1.wav", levelsFolder },
							{ "mute2.wav", levelsFolder },
							{ "mute3.wav", levelsFolder },
							{ "mute4.wav", levelsFolder }
						}
						: new Dictionary<string, string>
						{
							{ "surv1.son", levelsFolder },
							{ "surv2.son", levelsFolder },
							{ "surv3.son", levelsFolder },
							{ "mute1.son", levelsFolder },
							{ "mute2.son", levelsFolder },
							{ "mute3.son", levelsFolder }
						}
				);

			var foundFiles = new Dictionary<string, string>();

			foreach (var (file, folder) in files)
			{
				var resolved = InstallationUtils.GetFile(folder, file);

				if (resolved == null)
				{
					Log.Write("debug", $"=> Missing file: {file}");

					return null;
				}

				foundFiles.Add(file, resolved);
			}

			var game = new Installation("gen1", path);

			game.Music.Add("Survivors 1", game.GetPath(foundFiles[$"surv1.{(isXtreme ? "wav" : "son")}"]));
			game.Music.Add("Survivors 2", game.GetPath(foundFiles[$"surv2.{(isXtreme ? "wav" : "son")}"]));
			game.Music.Add("Survivors 3", game.GetPath(foundFiles[$"surv3.{(isXtreme ? "wav" : "son")}"]));
			game.Music.Add("Evolved 1", game.GetPath(foundFiles[$"mute1.{(isXtreme ? "wav" : "son")}"]));
			game.Music.Add("Evolved 2", game.GetPath(foundFiles[$"mute2.{(isXtreme ? "wav" : "son")}"]));
			game.Music.Add("Evolved 3", game.GetPath(foundFiles[$"mute3.{(isXtreme ? "wav" : "son")}"]));

			if (isXtreme)
			{
				game.Music.Add("Survivors 4", game.GetPath(foundFiles["surv4.wav"]));
				game.Music.Add("Evolved 4", game.GetPath(foundFiles["mute4.wav"]));
			}

			// Any other container for asset browser purpose
			game.Packages.Add("gen1_levels", game.GetPath(levelsFolder));
			game.Packages.Add("gen1_graphics", game.GetPath(graphicsFolder));
			game.Packages.Add("gen1_fmv", game.GetPath(fmvFolder));

			foreach (var file in Directory.GetFiles(levelsFolder)
				.Concat(Directory.GetFiles(graphicsFolder))
				.Where(f => f.EndsWith(".slv", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".lvl", StringComparison.OrdinalIgnoreCase)))
			{
				if (!game.Packages.ContainsValue(file))
					game.Packages.Add(Path.GetFileName(file).ToLower(), game.GetPath(file));
			}

			return game;
		}
	}
}

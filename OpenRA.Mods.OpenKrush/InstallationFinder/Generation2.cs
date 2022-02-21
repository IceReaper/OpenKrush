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

namespace OpenRA.Mods.OpenKrush.InstallationFinder;

public class Generation2 : IGeneration
{
	public int SteamAppId => 1292180;
	public int GogAppId => 1207659196;

	public Installation? TryRegister(string path)
	{
		var executablePath = path;

		// Krossfire GoG and Steam
		var executable = InstallationUtils.GetFile(executablePath, "kknd2.exe");

		// TODO CD version is more tricky
		//      its compressed in a .cab. So we need to use the local installation for the .exe and the base assets.
		//      however briefing videos and music tracks must be loaded from CD
		//      but depending on the faction, you need CD1 or CD2.
		//      this requires us to implement a "Insert Disc X" dialog.
		//      to avoid disk swapping while playing, we should make the dialog copy the data into the installation dir.

		if (executable == null)
			return null;

		Log.Write("debug", $"Detected installation: {path}");

		var release = CryptoUtil.SHA1Hash(File.OpenRead(executable));

		switch (release)
		{
			case "8d81c9183d04eb834aff29195797abe11aedc249":
				Log.Write("debug", "=> Krush, Kill 'N' Destroy 2: Krossfire (Steam/GoG, English)");

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

		// TODO the name of the graphics folder is tied to the language of the executable
		const string language = "english";

		var graphicsFolder = InstallationUtils.GetDirectory(levelsFolder, language);

		if (graphicsFolder == null)
		{
			Log.Write("debug", $"=> Missing folder: {language}");

			return null;
		}

		var multiFolder = InstallationUtils.GetDirectory(levelsFolder, "multi");

		if (multiFolder == null)
		{
			Log.Write("debug", "=> Missing folder: multi");

			return null;
		}

		// Required files.
		var files =
			new Dictionary<string, string>
			{
				{ "gamesprt.lpk", graphicsFolder },
				{ "surv.spk", graphicsFolder },
				{ "mute.spk", graphicsFolder },
				{ "robo.spk", graphicsFolder },
				{ "mh.vbc", fmvFolder },
				{ "intro.vbc", fmvFolder }
			}.Concat(
				new Dictionary<string, string>
				{
					{ "surv_01.wav", levelsFolder },
					{ "surv_02.wav", levelsFolder },
					{ "surv_03.wav", levelsFolder },
					{ "mute_01.wav", levelsFolder },
					{ "mute_02.wav", levelsFolder },
					{ "mute_03.wav", levelsFolder },
					{ "robo_01.wav", levelsFolder },
					{ "robo_02.wav", levelsFolder },
					{ "robo_03.wav", levelsFolder }
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

		var game = new Installation("gen2", path);

		game.Music.Add("Survivors 1", game.GetPath(foundFiles["surv_01.wav"]));
		game.Music.Add("Survivors 2", game.GetPath(foundFiles["surv_02.wav"]));
		game.Music.Add("Survivors 3", game.GetPath(foundFiles["surv_03.wav"]));
		game.Music.Add("Evolved 1", game.GetPath(foundFiles["mute_01.wav"]));
		game.Music.Add("Evolved 2", game.GetPath(foundFiles["mute_02.wav"]));
		game.Music.Add("Evolved 3", game.GetPath(foundFiles["mute_03.wav"]));
		game.Music.Add("Series9 1", game.GetPath(foundFiles["robo_01.wav"]));
		game.Music.Add("Series9 2", game.GetPath(foundFiles["robo_02.wav"]));
		game.Music.Add("Series9 3", game.GetPath(foundFiles["robo_03.wav"]));

		// Any other container for asset browser purpose
		game.Packages.Add("gen2_levels", game.GetPath(levelsFolder));
		game.Packages.Add("gen2_graphics", game.GetPath(graphicsFolder));
		game.Packages.Add("gen2_multi", game.GetPath(multiFolder));
		game.Packages.Add("gen2_fmv", game.GetPath(fmvFolder));

		foreach (var file in Directory.GetFiles(levelsFolder)
			.Concat(Directory.GetFiles(graphicsFolder))
			.Concat(Directory.GetFiles(multiFolder))
			.Where(
				f => f.EndsWith(".lpk", StringComparison.OrdinalIgnoreCase)
					|| f.EndsWith(".bpk", StringComparison.OrdinalIgnoreCase)
					|| f.EndsWith(".spk", StringComparison.OrdinalIgnoreCase)
					|| f.EndsWith(".lps", StringComparison.OrdinalIgnoreCase)
					|| f.EndsWith(".lpm", StringComparison.OrdinalIgnoreCase)
					|| f.EndsWith(".mpk", StringComparison.OrdinalIgnoreCase)
			))
		{
			if (!game.Packages.ContainsValue(file))
				game.Packages.Add(Path.GetFileName(file).ToLower(), game.GetPath(file));
		}

		return game;
	}
}

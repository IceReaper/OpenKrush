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

using Microsoft.Win32;
using System.Text.RegularExpressions;

public static class InstallationFinder
{
	public static Installation? RegisterInstallation(ModData modData, IGeneration generation)
	{
		return InstallationFinder.FindSteamInstallation(modData, generation)
			?? InstallationFinder.FindGoGInstallation(modData, generation) ?? InstallationFinder.FindCdVersion(modData, generation);
	}

	private static Installation? FindSteamInstallation(ModData modData, IGeneration generation)
	{
		foreach (var steamDirectory in InstallationFinder.SteamDirectory())
		{
			var manifestPath = Path.Combine(steamDirectory, "steamapps", $"appmanifest_{generation.SteamAppId}.acf");

			if (!File.Exists(manifestPath))
				continue;

			var data = InstallationFinder.ParseGameManifest(manifestPath);

			if (!data.TryGetValue("StateFlags", out var stateFlags) || stateFlags != "4")
				continue;

			if (!data.TryGetValue("installdir", out var installDir))
				continue;

			installDir = Path.Combine(steamDirectory, "steamapps", "common", installDir);

			Log.Write("debug", $"Steam version candidate: {installDir}");

			var game = InstallationUtils.TryRegister(modData, installDir, generation);

			if (game != null)
				return game;
		}

		Log.Write("debug", "Steam version not found");

		return null;
	}

	private static Installation? FindGoGInstallation(ModData modData, IGeneration generation)
	{
		if (Platform.CurrentPlatform == PlatformType.Windows)
		{
			var prefixes = new[] { "HKEY_LOCAL_MACHINE\\Software\\", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\" };

			foreach (var prefix in prefixes)
			{
#pragma warning disable CA1416
				var installDir = Registry.GetValue($"{prefix}GOG.com\\Games\\{generation.GogAppId}", "path", null) as string;
#pragma warning restore CA1416

				if (installDir == null)
					continue;

				Log.Write("debug", $"GoG version candidate: {installDir}");

				var game = InstallationUtils.TryRegister(modData, installDir, generation);

				if (game != null)
					return game;
			}

			Log.Write("debug", "GoG version not found");
		}
		else
			Log.Write("debug", "GoG version not supported on this platform");

		return null;
	}

	private static Installation? FindCdVersion(ModData modData, IGeneration generation)
	{
		foreach (var driveInfo in DriveInfo.GetDrives())
		{
			if (driveInfo.DriveType != DriveType.CDRom || !driveInfo.IsReady)
				continue;

			var installDir = driveInfo.RootDirectory.FullName;

			Log.Write("debug", $"CD version candidate: {installDir}");

			var game = InstallationUtils.TryRegister(modData, installDir, generation);

			if (game != null)
				return game;
		}

		Log.Write("debug", "CD version not found");

		return null;
	}

	private static IEnumerable<string> SteamDirectory()
	{
		var candidatePaths = new List<string>();

		switch (Platform.CurrentPlatform)
		{
			case PlatformType.Windows:
			{
				var prefixes = new[] { "HKEY_LOCAL_MACHINE\\Software\\", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\" };

				foreach (var prefix in prefixes)
				{
#pragma warning disable CA1416
					if (Registry.GetValue($"{prefix}Valve\\Steam", "InstallPath", null) is string path)
#pragma warning restore CA1416
						candidatePaths.Add(path);
				}

				break;
			}

			case PlatformType.OSX:
				candidatePaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Steam"));

				break;

			case PlatformType.Linux:
				candidatePaths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "root"));

				break;

			case PlatformType.Unknown:
				break;

			default:
				throw new ArgumentOutOfRangeException(Enum.GetName(Platform.CurrentPlatform));
		}

		foreach (var libraryPath in candidatePaths.Where(Directory.Exists))
		{
			yield return libraryPath;

			var libraryFoldersPath = Path.Combine(libraryPath, "steamapps", "libraryfolders.vdf");

			if (!File.Exists(libraryFoldersPath))
				continue;

			foreach (var e in InstallationFinder.ParseLibraryManifest(libraryFoldersPath).Where(e => e.Item1 == "path"))
				yield return e.Item2;
		}
	}

	private static Dictionary<string, string> ParseGameManifest(string path)
	{
		var regex = new Regex("^\\s*\"(?<key>[^\"]*)\"\\s*\"(?<value>[^\"]*)\"\\s*$");
		var result = new Dictionary<string, string>();

		using var s = new FileStream(path, FileMode.Open);

		foreach (var line in s.ReadAllLines())
		{
			var match = regex.Match(line);

			if (match.Success)
				result[match.Groups["key"].Value] = match.Groups["value"].Value;
		}

		return result;
	}

	private static List<Tuple<string, string>> ParseLibraryManifest(string path)
	{
		var regex = new Regex("^\\s*\"(?<key>[^\"]*)\"\\s*\"(?<value>[^\"]*)\"\\s*$");
		var result = new List<Tuple<string, string>>();

		using var s = new FileStream(path, FileMode.Open);

		foreach (var line in s.ReadAllLines())
		{
			var match = regex.Match(line);

			if (match.Success)
				result.Add(new(match.Groups["key"].Value, match.Groups["value"].Value));
		}

		return result;
	}
}

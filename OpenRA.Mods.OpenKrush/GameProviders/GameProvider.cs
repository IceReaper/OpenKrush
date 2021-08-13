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

namespace OpenRA.Mods.OpenKrush.GameProviders
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using GameRules;

	public static class GameProvider
	{
		public static string Installation;
		public static Dictionary<string, string> Packages = new Dictionary<string, string>();
		public static Dictionary<string, string> Music = new Dictionary<string, string>();
		public static Dictionary<string, string> Movies = new Dictionary<string, string>();

		public static bool TryRegister(ModData modData, string path)
		{
			return Generation1.TryRegister(path) && GameProvider.Mount(modData);
		}

		private static bool Mount(ModData modData)
		{
			modData.ModFiles.Mount(GameProvider.Installation, "installation");

			foreach (var (name, explicitName) in GameProvider.Packages)
				modData.ModFiles.Mount(GameProvider.InInstallation(name), explicitName);

			foreach (var key in GameProvider.Music.Keys)
				GameProvider.Music[key] = GameProvider.InInstallation(GameProvider.Music[key]);

			foreach (var key in GameProvider.Movies.Keys)
				GameProvider.Movies[key] = GameProvider.InInstallation(GameProvider.Movies[key]);

			return true;
		}

		public static IReadOnlyDictionary<string, MusicInfo> BuildMusicDictionary()
		{
			var result = new Dictionary<string, MusicInfo>();

			foreach (var (name, path) in GameProvider.Music)
			{
				var extension = Path.GetExtension(path).Substring(1);
				var key = path.Substring(0, path.Length - extension.Length - 1);
				result.Add(key, new MusicInfo(key, new MiniYaml(name, new List<MiniYamlNode> { new MiniYamlNode("Extension", extension) })));
			}

			return result;
		}

		public static string GetFile(string path, string file)
		{
			try
			{
				file = Directory.GetFiles(path).Select(Path.GetFileName).FirstOrDefault(d => d.Equals(file, StringComparison.OrdinalIgnoreCase));
			}
			catch (Exception)
			{
				return null;
			}

			return file == null ? null : Path.Combine(path, file);
		}

		public static string GetDirectory(string path, string directory)
		{
			try
			{
				directory = Directory.GetDirectories(path)
					.Select(Path.GetFileName)
					.FirstOrDefault(d => d.Equals(directory, StringComparison.OrdinalIgnoreCase));
			}
			catch (Exception)
			{
				return null;
			}

			return directory == null ? null : Path.Combine(path, directory);
		}

		private static string InInstallation(string path)
		{
			return "installation|" + Path.GetRelativePath(GameProvider.Installation, path);
		}
	}
}

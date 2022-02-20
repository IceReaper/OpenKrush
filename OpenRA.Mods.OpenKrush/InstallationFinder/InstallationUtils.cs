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
	using GameRules;

	public static class InstallationUtils
	{
		public static Installation? TryRegister(ModData modData, string path, IGeneration generation)
		{
			var installation = generation.TryRegister(path);

			if (installation == null)
				return installation;

			foreach (var (explicitName, name) in installation.Packages)
				modData.ModFiles.Mount(name, explicitName);

			return installation;
		}

		public static IReadOnlyDictionary<string, MusicInfo> BuildMusicDictionary(Installation? installation)
		{
			var result = new Dictionary<string, MusicInfo>();

			if (installation == null)
				return result;

			foreach (var (name, path) in installation.Music)
			{
				var extension = Path.GetExtension(path)[1..];
				var key = path[..(path.Length - extension.Length - 1)];
				result.Add(key, new(key, new(name, new() { new("Extension", extension) })));
			}

			return result;
		}

		public static string? GetFile(string path, string file)
		{
			try
			{
				var match = Directory.GetFiles(path)
					.Select(Path.GetFileName)
					.FirstOrDefault(d => d != null && d.Equals(file, StringComparison.OrdinalIgnoreCase));

				return match == null ? null : Path.Combine(path, match);
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static string? GetDirectory(string path, string directory)
		{
			try
			{
				var match = Directory.GetDirectories(path)
					.Select(Path.GetFileName)
					.FirstOrDefault(d => d != null && d.Equals(directory, StringComparison.OrdinalIgnoreCase));

				return match == null ? null : Path.Combine(path, match);
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}

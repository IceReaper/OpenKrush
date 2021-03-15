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
using OpenRA.GameRules;

namespace OpenRA.Mods.OpenKrush.GameProviders
{
	public static class GameProvider
	{
    	public static string Installation;
    	public static Dictionary<string, string> Packages = new Dictionary<string, string>();
    	public static Dictionary<string, string> Music = new Dictionary<string, string>();
    	public static Dictionary<string, string> Movies = new Dictionary<string, string>();

    	public static bool TryRegister(ModData modData, string path)
    	{
   	 	return Generation1.TryRegister(path) && Mount(modData);
    	}

    	private static bool Mount(ModData modData)
    	{
   	 	modData.ModFiles.Mount(Installation, "installation");

   	 	foreach (var (name, explicitName) in Packages)
  	 	 	modData.ModFiles.Mount(InInstallation(name), explicitName);

   	 	foreach (var key in Music.Keys)
  	 	 	Music[key] = InInstallation(Music[key]);

   	 	foreach (var key in Movies.Keys)
  	 	 	Movies[key] = InInstallation(Movies[key]);

   	 	return true;
    	}

    	public static IReadOnlyDictionary<string, MusicInfo> BuildMusicDictionary()
    	{
   	 	var result = new Dictionary<string, MusicInfo>();

   	 	foreach (var (name, path) in Music)
   	 	{
  	 	 	var extension = Path.GetExtension(path).Substring(1);
  	 	 	var key = path.Substring(0, path.Length - extension.Length - 1);
  	 	 	result.Add(key, new MusicInfo(key, new MiniYaml(name, new List<MiniYamlNode> { new MiniYamlNode("Extension", extension) })));
   	 	}

   	 	return result.AsReadOnly();
    	}

    	public static string GetFile(string path, string file)
    	{
   	 	file = Directory.GetFiles(path).Select(Path.GetFileName).FirstOrDefault(d => d.Equals(file, StringComparison.OrdinalIgnoreCase));

   	 	return file == null ? null : Path.Combine(path, file);
    	}

    	public static string GetDirectory(string path, string directory)
    	{
   	 	directory = Directory.GetDirectories(path).Select(Path.GetFileName).FirstOrDefault(d => d.Equals(directory, StringComparison.OrdinalIgnoreCase));

   	 	return directory == null ? null : Path.Combine(path, directory);
    	}

    	private static string InInstallation(string path)
    	{
   	 	return "installation|" + Path.GetRelativePath(Installation, path);
    	}
	}
}

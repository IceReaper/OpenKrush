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

public class Installation
{
	private readonly string id;
	private readonly string rootPath;
	public readonly Dictionary<string, string> Packages = new();
	public readonly Dictionary<string, string> Music = new();

	public Installation(string id, string rootPath)
	{
		this.id = id;
		this.rootPath = rootPath;

		this.Packages.Add(id, rootPath);
	}

	public string GetPath(string path)
	{
		return $"{this.id}|{Path.GetRelativePath(this.rootPath, path)}";
	}
}

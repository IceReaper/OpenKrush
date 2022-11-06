#region Copyright & License Information

/*
 * Copyright 2007-2022 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Assets.FileFormats;

public class MapdLayer
{
	public readonly int Width;
	public readonly int Height;
	public readonly byte[] Pixels;

	public MapdLayer(int width, int height)
	{
		this.Width = width;
		this.Height = height;
		this.Pixels = new byte[width * height * 4];
	}
}

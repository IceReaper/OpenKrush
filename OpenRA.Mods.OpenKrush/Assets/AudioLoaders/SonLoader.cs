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

namespace OpenRA.Mods.OpenKrush.Assets.AudioLoaders;

using FileFormats;
using JetBrains.Annotations;

[UsedImplicitly]
public class SonLoader : ISoundLoader
{
	bool ISoundLoader.TryParseSound(Stream stream, out ISoundFormat? soundFormat)
	{
		if (stream.Position + 16 <= stream.Length)
		{
			var type = stream.ReadASCII(4);
			stream.Position += 4;
			var format = stream.ReadASCII(8);
			stream.Position -= 16;

			if (type == "SIFF" && format == "SOUNSHDR")
			{
				soundFormat = new Son(stream);

				return true;
			}
		}

		soundFormat = null;

		return false;
	}
}

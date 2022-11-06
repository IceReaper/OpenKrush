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

namespace OpenRA.Mods.OpenKrush.Assets.AudioLoaders;

using FileFormats;
using JetBrains.Annotations;

[UsedImplicitly]
public class SounLoader : ISoundLoader
{
	bool ISoundLoader.TryParseSound(Stream stream, out ISoundFormat? soundFormat)
	{
		if (stream.Position + 72 <= stream.Length)
		{
			stream.Position += 52;
			var filename = stream.ReadASCII(20);
			stream.Position -= 72;

			if (filename.Contains(".smp"))
			{
				soundFormat = new Soun(stream);

				return true;
			}
		}

		soundFormat = null;

		return false;
	}
}

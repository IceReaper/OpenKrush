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

using System.IO;
using OpenRA.Mods.OpenKrush.FileFormats;

namespace OpenRA.Mods.OpenKrush.AudioLoaders
{
	public class SonLoader : ISoundLoader
	{
		bool ISoundLoader.TryParseSound(Stream stream, out ISoundFormat sound)
		{
			if (stream.Position + 16 <= stream.Length)
			{
				var type = stream.ReadASCII(4);
				stream.Position += 4;
				var format = stream.ReadASCII(8);
				stream.Position -= 16;

				if (type == "SIFF" && format == "SOUNSHDR")
				{
					sound = new Son(stream);
					return true;
				}
			}

			sound = null;
			return false;
		}
	}
}

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
		private static bool IsSon(Stream s)
		{
			var start = s.Position;
			var type = s.ReadASCII(4);
			s.Position += 4;
			var format = s.ReadASCII(8);
			s.Position = start;

			return type == "SIFF" && format == "SOUNSHDR";
		}

		bool ISoundLoader.TryParseSound(Stream stream, out ISoundFormat sound)
		{
			try
			{
				if (IsSon(stream))
				{
					sound = new Son(stream);
					return true;
				}
			}
			catch
			{
				// Not a (supported) SON
			}

			sound = null;
			return false;
		}
	}
}

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
	public class SounLoader : ISoundLoader
	{
		private static bool IsSoun(Stream s)
		{
			if (s.Position + 72 > s.Length)
				return false;

			var start = s.Position;
			s.Position += 52;
			var filename = s.ReadASCII(20);
			s.Position = start;

			return filename.Contains(".smp");
		}

		bool ISoundLoader.TryParseSound(Stream stream, out ISoundFormat sound)
		{
			try
			{
				if (IsSoun(stream))
				{
					sound = new Soun(stream);
					return true;
				}
			}
			catch
			{
				// Not a (supported) SOUN
			}

			sound = null;
			return false;
		}
	}
}

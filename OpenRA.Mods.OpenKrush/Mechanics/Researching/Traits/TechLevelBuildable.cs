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

namespace OpenRA.Mods.OpenKrush.Mechanics.Researching.Traits
{
	using Common.Traits;

	[Desc("Adds support for tech level to Buildable.")]
	public class TechLevelBuildableInfo : BuildableInfo
	{
		public const string Prefix = "ACTOR::";

		[Desc("The tech level this actor is on.")]
		public readonly int Level = 0;
	}

	public class TechLevelBuildable : Buildable
	{
	}
}

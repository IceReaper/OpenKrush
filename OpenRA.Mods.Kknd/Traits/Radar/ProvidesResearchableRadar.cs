#region Copyright & License Information
/*
 * Copyright 2016-2018 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Radar
{
	[Desc("This actor enables the radar minimap.")]
	public class ProvidesResearchableRadarInfo : ITraitInfo
	{
		[Desc("The provider level required to enable radar.")]
		public readonly int Level = 1;

		[Desc("The provider level required to show ally units.")]
		public readonly int AllyLevel = 1;

		[Desc("The provider level required to show enemy units.")]
		public readonly int EnemyLevel = 2;

		public object Create(ActorInitializer init) { return new ProvidesResearchableRadar(); }
	}

	public class ProvidesResearchableRadar { }
}

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
	using System.Collections.Generic;
	using Common.Traits;

	[Desc("This actor enables the radar minimap.")]
	public class ProvidesResearchableRadarInfo : ConditionalTraitInfo
	{
		private const string Prefix = "RADAR::";
		public const string Available = ProvidesResearchableRadarInfo.Prefix + "AVAILABLE";
		public const string ShowAllies = ProvidesResearchableRadarInfo.Prefix + "ALLIES";
		public const string ShowEnemies = ProvidesResearchableRadarInfo.Prefix + "ENEMIES";

		[Desc("The tech level required to enable radar.")]
		public readonly int Level = 1;

		[Desc("The tech level required to show ally units.")]
		public readonly int AllyLevel = 1;

		[Desc("The tech level required to show enemy units.")]
		public readonly int EnemyLevel = 2;

		public override object Create(ActorInitializer init)
		{
			return new ProvidesResearchableRadar(this);
		}
	}

	public class ProvidesResearchableRadar : ConditionalTrait<ProvidesResearchableRadarInfo>, IProvidesResearchables
	{
		public ProvidesResearchableRadar(ProvidesResearchableRadarInfo info)
			: base(info)
		{
		}

		public Dictionary<string, int> GetResearchables()
		{
			return new Dictionary<string, int>
			{
				{ ProvidesResearchableRadarInfo.Available, Info.Level },
				{ ProvidesResearchableRadarInfo.ShowAllies, Info.AllyLevel },
				{ ProvidesResearchableRadarInfo.ShowEnemies, Info.EnemyLevel }
			};
		}
	}
}

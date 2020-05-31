#region Copyright & License Information

/*
 * Copyright 2016-2020 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Saboteurs.Traits
{
	[Desc("KKnD Saboteur mechanism, attach to the building.")]
	public class SaboteurConquerableInfo : ConditionalTraitInfo
	{
		[Desc("Starting population.")]
		public readonly int Population = 3;

		[Desc("Maximum population.")]
		public readonly int MaxPopulation = 5;

		[Desc("Voice used when enemy infiltrates a building.")]
		public readonly string NotificationInfiltrated = "EnemyInfiltrated";

		[Desc("Voice used when enemy conquered a building.")]
		public readonly string NotificationConquered = "EnemyConquered";

		public override object Create(ActorInitializer init)
		{
			return new SaboteurConquerable(this);
		}
	}

	public class SaboteurConquerable : ConditionalTrait<SaboteurConquerableInfo>
	{
		public int Population;

		public SaboteurConquerable(SaboteurConquerableInfo info)
			: base(info)
		{
			Population = info.Population;
		}

		public void Enter(Actor self, Actor target)
		{
			if (self.Owner.Stances[target.Owner].HasStance(Stance.Ally))
				Population = Math.Min(Population + 1, Info.MaxPopulation);
			else if (Population > 0)
			{
				Population--;
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.NotificationInfiltrated, self.Owner.Faction.InternalName);
			}
			else
			{
				Population = 1;
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.NotificationConquered, self.Owner.Faction.InternalName);
				self.ChangeOwner(target.Owner);

				// TODO clear production queues!
			}
		}
	}
}

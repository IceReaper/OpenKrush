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

namespace OpenRA.Mods.OpenKrush.Mechanics.Saboteurs.Traits
{
	using System;
	using Common.Traits;
	using LobbyOptions;
	using OpenRA.Traits;

	[Desc("Saboteur mechanism, attach to the building.")]
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
			return new SaboteurConquerable(init, this);
		}
	}

	public class SaboteurConquerable : ConditionalTrait<SaboteurConquerableInfo>
	{
		private readonly SaboteurUsage usage;

		public int Population;

		public SaboteurConquerable(ActorInitializer init, SaboteurConquerableInfo info)
			: base(info)
		{
			Population = info.Population;

			usage = init.Self.World.WorldActor.Trait<SaboteurUsage>();
		}

		public void Enter(Actor self, Actor target)
		{
			if (self.Owner.RelationshipWith(target.Owner).HasRelationship(PlayerRelationship.Ally))
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

				if (usage.Usage == SaboteurUsageType.Conquer || self.Owner.RelationshipWith(target.Owner) == PlayerRelationship.Neutral)
				{
					// TODO clear production queues!
					self.ChangeOwner(target.Owner);
				}
				else
				{
					var worth = self.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
					target.Owner.PlayerActor.Trait<PlayerResources>().GiveCash(worth);
					self.Kill(target);
				}
			}
		}
	}
}

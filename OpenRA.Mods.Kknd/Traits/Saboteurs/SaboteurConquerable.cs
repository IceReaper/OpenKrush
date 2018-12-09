using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Saboteurs
{
	[Desc("KKnD specific saboteur target implementation.")]
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

		public override object Create(ActorInitializer init) { return new SaboteurConquerable(init, this); }
	}

	public class SaboteurConquerable : ConditionalTrait<SaboteurConquerableInfo>
	{
		private readonly SaboteurConquerableInfo info;
		public int Population { get; private set; }

		public SaboteurConquerable(ActorInitializer init, SaboteurConquerableInfo info) : base(info)
		{
			this.info = info;
			Population = info.Population;
		}

		public void Enter(Actor self, Actor saboteur)
		{
			if (saboteur.Owner.Stances[self.Owner].HasStance(Stance.Ally))
				Population = Math.Min(Population + 1, info.MaxPopulation);
			else
			{
				if (Population > 0)
				{
					Population--;
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.NotificationInfiltrated, self.Owner.Faction.InternalName);
				}
				else
				{
					Population = 1;
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.NotificationConquered, self.Owner.Faction.InternalName);
					self.ChangeOwner(saboteur.Owner);
				}
			}
		}
	}
}

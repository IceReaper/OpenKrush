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

namespace OpenRA.Mods.OpenKrush.Mechanics.Production.Traits
{
	using Common.Traits;
	using JetBrains.Annotations;
	using Researching.Traits;

	[UsedImplicitly]
	[Desc("This version of ParallelProductionQueue references prerequisites and techlevel to itself.")]
	public class AdvancedProductionQueueInfo : ParallelProductionQueueInfo
	{
		public override object Create(ActorInitializer init)
		{
			return new AdvancedProductionQueue(init, init.Self.Owner.PlayerActor, this);
		}
	}

	public class AdvancedProductionQueue : ParallelProductionQueue
	{
		private readonly Actor actor;

		public AdvancedProductionQueue(ActorInitializer init, Actor playerActor, AdvancedProductionQueueInfo info)
			: base(init, playerActor, info)
		{
			this.actor = init.Self;
		}

		public override IEnumerable<ActorInfo> BuildableItems()
		{
			if (this.productionTraits.Any() && this.productionTraits.All(p => p.IsTraitDisabled))
				return Enumerable.Empty<ActorInfo>();

			if (!this.Enabled)
				return Enumerable.Empty<ActorInfo>();

			if (this.developerMode.AllTech)
				return this.Producible.Keys;

			return this.Producible.Keys.Where(
				prod =>
				{
					var buildable = prod.TraitInfoOrDefault<BuildableInfo>();

					if (buildable == null)
						return true;

					if (buildable.BuildLimit > 0
						&& buildable.BuildLimit
						<= this.Actor.World.ActorsHavingTrait<Buildable>().Count(a => a.Info.Name == prod.Name && a.Owner == this.Actor.Owner))
						return false;

					return this.ProducerHasRequirements(prod, buildable);
				}
			);
		}

		protected virtual bool ProducerHasRequirements(ActorInfo prod, BuildableInfo buildable)
		{
			if (!this.Actor.Info.TraitInfos<ProvidesPrerequisiteInfo>()
				.Any(providesPrerequisite => buildable.Prerequisites.Contains(providesPrerequisite.Prerequisite)))
				return false;

			if (buildable is not TechLevelBuildableInfo advancedBuildable)
				return true;

			if (advancedBuildable.Level == -1)
				return false;

			var researchable = this.actor.TraitOrDefault<Researchable>();

			return researchable.IsResearched(TechLevelBuildableInfo.Prefix + prod.Name);
		}

		public override int GetBuildTime(ActorInfo unit, BuildableInfo bi)
		{
			return base.GetBuildTime(unit, bi) * (100 + this.actor.Owner.Handicap) / 100;
		}
	}
}

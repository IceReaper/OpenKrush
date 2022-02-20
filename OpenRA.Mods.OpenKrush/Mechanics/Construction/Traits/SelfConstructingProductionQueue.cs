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

namespace OpenRA.Mods.OpenKrush.Mechanics.Construction.Traits
{
	using Common.Traits;
	using JetBrains.Annotations;
	using Production.Traits;
	using Researching.Traits;

	[UsedImplicitly]
	[Desc("This special production queue implements a fake AllQueued, used to instantly place self constructing buildings.")]
	public class SelfConstructingProductionQueueInfo : AdvancedProductionQueueInfo
	{
		public override object Create(ActorInitializer init)
		{
			return new SelfConstructingProductionQueue(init, init.Self, this);
		}
	}

	public class SelfConstructingProductionQueue : AdvancedProductionQueue
	{
		private bool expectFakeProductionItemRequest;

		public SelfConstructingProductionQueue(ActorInitializer init, Actor playerActor, SelfConstructingProductionQueueInfo info)
			: base(init, playerActor, info)
		{
		}

		public override IEnumerable<ProductionItem> AllQueued()
		{
			// Pretend to have items queued, to allow direct placement.
			return this.BuildableItems()
				.Select(
					buildableItem =>
					{
						// Cost == 0 to not consume money at this point.
						var item = new ProductionItem(this, buildableItem.Name, 0, null, null);

						// Required for GetBuildTime, else the production wont be ready after below Tick().
						this.expectFakeProductionItemRequest = true;

						// Tick once, so the item is done.
						item.Tick(this.playerResources);

						return item;
					}
				)
				.ToList();
		}

		public override int GetBuildTime(ActorInfo unit, BuildableInfo bi)
		{
			// Workaround to make above Tick receive a 0 for the production time.
			if (!this.expectFakeProductionItemRequest)
				return base.GetBuildTime(unit, bi);

			this.expectFakeProductionItemRequest = false;

			return 0;
		}

		protected override void Tick(Actor self)
		{
			this.TickInner(self, false);
		}

		public new void BeginProduction(ProductionItem item, bool hasPriority)
		{
			base.BeginProduction(item, hasPriority);
		}

		protected override void TickInner(Actor self, bool allProductionPaused)
		{
			this.Queue.RemoveAll(
				item => item is not SelfConstructingProductionItem selfConstructingItem
					|| selfConstructingItem.Actor.IsDead
					|| !selfConstructingItem.Actor.IsInWorld
			);

			if (this.Queue.Count > 0)
			{
				var first = this.Queue[0];
				var before = first.RemainingTime;
				first.Tick(this.playerResources);

				if (first.RemainingTime != before)
				{
					this.Queue.Remove(first);

					if (!first.Done)
						this.Queue.Add(first);
					else
						Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", this.Info.ReadyAudio, self.Owner.Faction.InternalName);
				}
			}

			// Auto finish done items, as they are already in the world in our case!
			this.Queue.FindAll(i => i.Done).ForEach(i => this.Queue.Remove(i));
		}

		protected override bool ProducerHasRequirements(ActorInfo prod, BuildableInfo buildable)
		{
			var producers = this.Actor.World.ActorsWithTrait<Production>()
				.Where(x => !x.Trait.IsTraitDisabled && x.Actor.Owner == this.Actor.Owner)
				.Select(x => x.Actor);

			return producers.Any(
				producer =>
				{
					if (!producer.Info.TraitInfos<ProvidesPrerequisiteInfo>()
						.Any(providesPrerequisite => buildable.Prerequisites.Contains(providesPrerequisite.Prerequisite)))
						return false;

					if (buildable is not TechLevelBuildableInfo)
						return true;

					var researchable = producer.TraitOrDefault<Researchable>();

					return researchable.IsResearched(TechLevelBuildableInfo.Prefix + prod.Name);
				}
			);
		}
	}

	public class SelfConstructingProductionItem : ProductionItem
	{
		public readonly Actor Actor;

		public SelfConstructingProductionItem(ProductionQueue queue, Actor actor, int cost, PowerManager? pm, Action? onComplete)
			: base(queue, actor.Info.Name, cost, pm, onComplete)
		{
			this.Actor = actor;
		}
	}
}

#region Copyright & License Information
/*
 * Copyright 2007-2021 The KKnD Developers (see AUTHORS)
 * This file is part of KKnD, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Mechanics.Researching.Traits;

namespace OpenRA.Mods.Kknd.Traits.Production
{
	[Desc("This special production queue implements a fake AllQueued, used to instantly place self constructing buildings.")]
	public class SelfConstructingProductionQueueInfo : AdvancedProductionQueueInfo
	{
		public override object Create(ActorInitializer init) { return new SelfConstructingProductionQueue(init, init.Self, this); }
	}

	public class SelfConstructingProductionQueue : AdvancedProductionQueue
	{
		private bool expectFakeProductionItemRequest;

		public SelfConstructingProductionQueue(ActorInitializer init, Actor playerActor, SelfConstructingProductionQueueInfo info)
			: base(init, playerActor, info) { }

		public override IEnumerable<ProductionItem> AllQueued()
		{
			// Pretend to have items queued, to allow direct placement.
			return BuildableItems().Select(buildableItem =>
			{
				// Cost == 0 to not consume money at this point.
				var item = new ProductionItem(this, buildableItem.Name, 0, null, null);

				// Required for GetBuildTime, else the production wont be ready after below Tick().
				expectFakeProductionItemRequest = true;

				// Tick once, so the item is done.
				item.Tick(playerResources);
				return item;
			}).ToList();
		}

		public override int GetBuildTime(ActorInfo unit, BuildableInfo bi)
		{
			// Workaround to make above Tick receive a 0 for the production time.
			if (expectFakeProductionItemRequest)
			{
				expectFakeProductionItemRequest = false;
				return 0;
			}

			return base.GetBuildTime(unit, bi);
		}

		protected override void Tick(Actor self)
		{
			TickInner(self, false);
		}

		public new void BeginProduction(ProductionItem item, bool hasPriority)
		{
			base.BeginProduction(item, hasPriority);
		}

		protected override void TickInner(Actor self, bool allProductionPaused)
		{
			Queue.RemoveAll(item =>
			{
				var selfConstructingItem = item as SelfConstructingProductionItem;
				return selfConstructingItem == null || selfConstructingItem.Actor.IsDead || !selfConstructingItem.Actor.IsInWorld;
			});

			if (Queue.Count > 0)
			{
				var first = Queue[0];
				var before = first.RemainingTime;
				first.Tick(playerResources);

				if (first.RemainingTime != before)
				{
					Queue.Remove(first);

					if (!first.Done)
						Queue.Add(first);
					else
						Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", Info.ReadyAudio, self.Owner.Faction.InternalName);
				}
			}

			// Auto finish done items, as they are already in the world in our case!
			Queue.FindAll(i => i.Done).ForEach(i => Queue.Remove(i));
		}

		protected override bool ProducerHasRequirements(BuildableInfo buildable)
		{
			var producers = Actor.World.ActorsWithTrait<Common.Traits.Production>().Where(x => !x.Trait.IsTraitDisabled && x.Actor.Owner == Actor.Owner).Select(x => x.Actor);

			return producers.Any(producer =>
			{
				if (!producer.Info.TraitInfos<ProvidesPrerequisiteInfo>().Any(providesPrerequisite => buildable.Prerequisites.Contains(providesPrerequisite.Prerequisite)))
					return false;

				var advancedBuildable = buildable as AdvancedBuildableInfo;

				if (advancedBuildable == null)
					return true;

				if (advancedBuildable.Level == -1)
					return false;

				return advancedBuildable.Level == 0 || advancedBuildable.Level <= producer.Trait<Researchable>().Level;
			});
		}
	}

	public class SelfConstructingProductionItem : ProductionItem
	{
		public readonly Actor Actor;

		public SelfConstructingProductionItem(ProductionQueue queue, Actor actor, int cost, PowerManager pm, Action onComplete)
			: base(queue, actor.Info.Name, cost, pm, onComplete)
		{
			Actor = actor;
		}
	}
}

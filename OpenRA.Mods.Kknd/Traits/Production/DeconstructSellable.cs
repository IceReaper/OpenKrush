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

using System;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Kknd.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Production
{
	public class DeconstructSellableInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("How long selling will take, percentual to build time.")]
		public readonly int SellTimePercent = 50;

		[Desc("The percentual amount of money to refund.")]
		public readonly int RefundPercent = 25;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while the make animation is playing.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init) { return new DeconstructSellable(init, this); }
	}

	public class DeconstructSellable : ConditionalTrait<DeconstructSellableInfo>, ITick, IResolveOrder, INotifyCreated
	{
		private readonly DeconstructSellableInfo info;

		private readonly DeveloperMode developerMode;
		private readonly WithSpriteBody wsb;

		private SelfConstructingInfo selfConstructing;

		private ConditionManager conditionManager;
		private int token = ConditionManager.InvalidConditionToken;

		private int sellTimer;
		private int sellTimerTotal;
		private int refundAmount;

		public DeconstructSellable(ActorInitializer init, DeconstructSellableInfo info) : base(info)
		{
			this.info = info;
			developerMode = init.Self.Owner.PlayerActor.Trait<DeveloperMode>();
			wsb = init.Self.Trait<WithSpriteBody>();
		}

		void INotifyCreated.Created(Actor self)
		{
			selfConstructing = self.Info.TraitInfo<SelfConstructingInfo>();
			conditionManager = self.TraitOrDefault<ConditionManager>();
		}

		void ITick.Tick(Actor self)
		{
			if (self.IsDead)
				return;

			if (token != ConditionManager.InvalidConditionToken)
			{
				sellTimer = developerMode.FastBuild ? 0 : sellTimer - 1;

				if (sellTimer <= 0)
				{
					foreach (var notifySold in self.TraitsImplementing<INotifySold>())
						notifySold.Sold(self);

					var pr = self.Owner.PlayerActor.Trait<PlayerResources>();
					var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();

					if (valued != null)
						pr.GiveCash(refundAmount * info.RefundPercent / 100);

					self.Dispose();
				}
				else
					wsb.PlayCustomAnimationRepeating(self, selfConstructing.Sequence + Math.Min(sellTimer * selfConstructing.Steps / sellTimerTotal, selfConstructing.Steps - 1));
			}
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != SellOrderGenerator.Id)
				return;

			if (conditionManager != null && !string.IsNullOrEmpty(info.Condition) && token == ConditionManager.InvalidConditionToken)
				token = conditionManager.GrantCondition(self, info.Condition);

			var productionItem = self.Trait<SelfConstructing>().TryAbort(self);
			var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
			
			if (productionItem != null)
				refundAmount = productionItem.TotalCost - productionItem.RemainingCost;
			else if (valued != null)
				refundAmount = valued.Cost;

			sellTimer = sellTimerTotal = self.Info.TraitInfoOrDefault<BuildableInfo>().BuildDuration * info.SellTimePercent / 100;
			
			if (developerMode.FastBuild)
				sellTimer = 0;
			else if (productionItem != null)
				sellTimer = (productionItem.TotalTime - productionItem.RemainingTime) * info.SellTimePercent / 100;

			foreach (var notifySold in self.TraitsImplementing<INotifySold>())
				notifySold.Selling(self);
		}
	}
}

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
	using System;
	using Common.Traits;
	using Common.Traits.Render;
	using OpenRA.Traits;
	using Orders;

	public class DeconstructSellableInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("How long selling will take, percentual to build time.")]
		public readonly int SellTimePercent = 50;

		[Desc("The percentual amount of money to refund.")]
		public readonly int RefundPercent = 25;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while the make animation is playing.")]
		public readonly string Condition = null;

		public override object Create(ActorInitializer init)
		{
			return new DeconstructSellable(init, this);
		}
	}

	public class DeconstructSellable : ConditionalTrait<DeconstructSellableInfo>, ITick, IResolveOrder, INotifyCreated
	{
		private readonly DeconstructSellableInfo info;

		private readonly DeveloperMode developerMode;
		private readonly WithSpriteBody wsb;

		private SelfConstructing selfConstructing;

		private int token = Actor.InvalidConditionToken;

		private int sellTimer;
		private int sellTimerTotal;
		private int refundAmount;

		public bool IsSelling => token != Actor.InvalidConditionToken;

		public DeconstructSellable(ActorInitializer init, DeconstructSellableInfo info)
			: base(info)
		{
			this.info = info;
			developerMode = init.Self.Owner.PlayerActor.Trait<DeveloperMode>();
			wsb = init.Self.Trait<WithSpriteBody>();
		}

		protected override void Created(Actor self)
		{
			selfConstructing = self.Trait<SelfConstructing>();
		}

		void ITick.Tick(Actor self)
		{
			if (self.IsDead)
				return;

			if (token == Actor.InvalidConditionToken)
				return;

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
				wsb.PlayCustomAnimationRepeating(
					self,
					selfConstructing.Info.Sequence.Substring(0, selfConstructing.Info.Sequence.Length - 1)
					+ Math.Min(sellTimer * selfConstructing.Steps / sellTimerTotal, selfConstructing.Steps - 1));
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != SellOrderGenerator.Id)
				return;

			if (!string.IsNullOrEmpty(info.Condition) && token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);

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

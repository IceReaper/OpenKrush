#region Copyright & License Information

/*
 * Copyright 2007-2022 The OpenKrush Developers (see AUTHORS)
 * This file is part of OpenKrush, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */

#endregion

namespace OpenRA.Mods.OpenKrush.Mechanics.Construction.Traits;

using Common.Traits;
using Common.Traits.Render;
using JetBrains.Annotations;
using OpenRA.Traits;
using Orders;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class DeconstructSellableInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
{
	[Desc("How long selling will take, percentual to build time.")]
	public readonly int SellTimePercent = 50;

	[Desc("The percentual amount of money to refund.")]
	public readonly int RefundPercent = 25;

	[GrantedConditionReference]
	[Desc("The condition to grant to self while the make animation is playing.")]
	public readonly string? Condition;

	public override object Create(ActorInitializer init)
	{
		return new DeconstructSellable(init, this);
	}
}

public class DeconstructSellable : ConditionalTrait<DeconstructSellableInfo>, ITick, IResolveOrder
{
	private readonly DeconstructSellableInfo info;

	private readonly DeveloperMode developerMode;
	private readonly WithSpriteBody wsb;

	private SelfConstructing? selfConstructing;

	private int token = Actor.InvalidConditionToken;

	private int sellTimer;
	private int sellTimerTotal;
	private int refundAmount;

	public DeconstructSellable(ActorInitializer init, DeconstructSellableInfo info)
		: base(info)
	{
		this.info = info;
		this.developerMode = init.Self.Owner.PlayerActor.TraitOrDefault<DeveloperMode>();
		this.wsb = init.Self.TraitOrDefault<WithSpriteBody>();
	}

	protected override void Created(Actor self)
	{
		this.selfConstructing = self.TraitOrDefault<SelfConstructing>();
	}

	void ITick.Tick(Actor self)
	{
		if (self.IsDead)
			return;

		if (this.token == Actor.InvalidConditionToken)
			return;

		this.sellTimer = this.developerMode.FastBuild ? 0 : this.sellTimer - 1;

		if (this.sellTimer <= 0)
		{
			foreach (var notifySold in self.TraitsImplementing<INotifySold>())
				notifySold.Sold(self);

			var pr = self.Owner.PlayerActor.TraitOrDefault<PlayerResources>();
			var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();

			if (valued != null)
				pr.GiveCash(this.refundAmount * this.info.RefundPercent / 100);

			self.Dispose();
		}
		else if (this.selfConstructing != null)
		{
			this.wsb.PlayCustomAnimationRepeating(
				self,
				this.selfConstructing.Info.Sequence[..^1]
				+ Math.Min(this.sellTimer * this.selfConstructing.Steps / this.sellTimerTotal, this.selfConstructing.Steps - 1)
			);
		}
	}

	void IResolveOrder.ResolveOrder(Actor self, Order order)
	{
		if (order.OrderString != SellOrderGenerator.Id)
			return;

		if (!string.IsNullOrEmpty(this.info.Condition) && this.token == Actor.InvalidConditionToken)
			this.token = self.GrantCondition(this.info.Condition);

		var productionItem = self.TraitOrDefault<SelfConstructing>().TryAbort(self);
		var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();

		if (productionItem != null)
			this.refundAmount = productionItem.TotalCost - productionItem.RemainingCost;
		else if (valued != null)
			this.refundAmount = valued.Cost;

		this.sellTimer = this.sellTimerTotal = self.Info.TraitInfoOrDefault<BuildableInfo>().BuildDuration * this.info.SellTimePercent / 100;

		if (this.developerMode.FastBuild)
			this.sellTimer = 0;
		else if (productionItem != null)
			this.sellTimer = (productionItem.TotalTime - productionItem.RemainingTime) * this.info.SellTimePercent / 100;

		foreach (var notifySold in self.TraitsImplementing<INotifySold>())
			notifySold.Selling(self);
	}
}

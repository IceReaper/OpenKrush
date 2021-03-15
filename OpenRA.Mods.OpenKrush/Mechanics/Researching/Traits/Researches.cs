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
	using System;
	using System.Collections.Generic;
	using Common.Traits;
	using OpenRA.Traits;
	using Orders;

	[Desc("Research mechanism, attach to the actor which can research.")]
	public class ResearchesInfo : ConditionalTraitInfo, Requires<ResearchableInfo>
	{
		[Desc("Cursor used when target actor is researchable.")]
		public readonly string Cursor = "research";

		[Desc("Cursor used when target actor not not researchable.")]
		public readonly string BlockedCursor = "research-blocked";

		[Desc("Notification when research starts.")]
		public readonly string StartNotification = "ResearchStarted";

		[Desc("Notification when research canceled.")]
		public readonly string CancelNotification = "ResearchCanceled";

		[Desc("Notification when research finishes.")]
		public readonly string CompleteNotification = "ResearchCompleted";

		[Desc("Percentual research duration and cost per tech level.")]
		public readonly int[] ResearchRates = { 100, 90, 80, 70, 60, 50 };

		public override object Create(ActorInitializer init)
		{
			return new Researches(init, this);
		}
	}

	public class Researches : ConditionalTrait<ResearchesInfo>, IIssueOrder, IResolveOrder, INotifyKilled, ITick
	{
		private readonly ResearchesInfo info;
		private readonly Actor self;

		private readonly Researchable researchable;
		private readonly DeveloperMode developerMode;
		private readonly PlayerResources playerResources;

		public Actor Researching;

		private int totalCost;
		private int totalTime;
		private int remainingCost;
		private int remainingTime;

		public Researches(ActorInitializer init, ResearchesInfo info)
			: base(info)
		{
			this.info = info;
			self = init.Self;
			researchable = init.Self.Trait<Researchable>();
			developerMode = init.Self.Owner.PlayerActor.Trait<DeveloperMode>();
			playerResources = init.Self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new ResearchOrderTargeter(info.Cursor, info.BlockedCursor);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			return order.OrderID == ResearchOrderTargeter.Id ? new Order(order.OrderID, self, target, queued) : null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != ResearchOrderTargeter.Id)
				return;

			if (order.Target.Type != TargetType.Actor || order.Target.Actor == null)
				return;

			var action = ResearchUtils.GetAction(self, order.Target.Actor);

			if (action == ResearchAction.Start)
				StartResearch(order.Target.Actor);
			else if (action == ResearchAction.Stop)
				order.Target.Actor.Trait<Researchable>().ResearchedBy.StopResearch(true);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo attackInfo)
		{
			StopResearch(true);
		}

		void ITick.Tick(Actor self)
		{
			if (Researching == null)
				return;

			var action = ResearchUtils.GetAction(self, Researching);

			if (action == ResearchAction.None)
			{
				StopResearch(true);

				return;
			}

			var expectedRemainingCost = remainingTime == 1 ? 0 : totalCost * remainingTime / Math.Max(1, totalTime);
			var costThisFrame = remainingCost - expectedRemainingCost;

			if (costThisFrame != 0 && !playerResources.TakeCash(costThisFrame, true))
				return;

			remainingCost -= costThisFrame;
			remainingTime -= 1;

			var researchable = Researching.Trait<Researchable>();

			if (remainingTime == 0)
			{
				StopResearch(false);
				researchable.Level++;
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.CompleteNotification, self.Owner.Faction.InternalName);
			}
			else
				researchable.SetProgress((totalTime - remainingTime) / (float)totalTime);
		}

		private void StartResearch(Actor target)
		{
			if (Researching != null && Researching.Equals(target))
				return;

			var researchable = target.Trait<Researchable>();

			if (researchable.ResearchedBy != null)
				return;

			if (Researching != null)
				StopResearch(true);

			Researching = target;
			remainingCost = totalCost = researchable.Info.ResearchCost[researchable.Level] * info.ResearchRates[this.researchable.Level] / 100;

			if (developerMode.FastBuild)
				remainingTime = totalTime = 1;
			else
				remainingTime = totalTime = researchable.Info.ResearchTime[researchable.Level] * info.ResearchRates[this.researchable.Level] / 100;

			researchable.ResearchedBy = this;
			researchable.SetProgress(0);

			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.StartNotification, self.Owner.Faction.InternalName);
		}

		private void StopResearch(bool isCanceled)
		{
			if (Researching == null)
				return;

			if (!Researching.Disposed)
				Researching.Trait<Researchable>().ResearchedBy = null;

			Researching = null;

			if (isCanceled)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.CancelNotification, self.Owner.Faction.InternalName);
		}

		public ResarchState GetState()
		{
			if (IsTraitDisabled)
				return ResarchState.Unavailable;

			if (Researching != null)
				return ResarchState.Researching;

			return ResarchState.Available;
		}
	}
}

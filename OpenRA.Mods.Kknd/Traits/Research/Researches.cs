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
using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Orders;
using OpenRA.Mods.Kknd.Traits.Production;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Research
{
	[Desc("Allows an actor to research actors.")]
	public class ResearchesInfo : ConditionalTraitInfo, Requires<ResearchableInfo>
	{
		[Desc("Cursor used when target actor is researchable.")]
		public readonly string Cursor = "research";

		[Desc("Cursor used when target actor not not researchable.")]
		public readonly string BlockedCursor = "research-blocked";

		[Desc("Notification when research starts.")]
		public readonly string StartNotification = "ResearchStarted";

		[Desc("Notification when research finishes.")]
		public readonly string FinishNotification = "ResearchCompleted";

		[Desc("Percentual research duration per level-up.")]
		public readonly int[] ResearchRates = { 100, 90, 80, 70, 60, 50 };

		public readonly int ResearchSteps = 6;

		public override object Create(ActorInitializer init) { return new Researches(init, this); }
	}

	public class Researches : ConditionalTrait<ResearchesInfo>, ITick, IIssueOrder, IResolveOrder, INotifyKilled
	{
		private readonly ResearchesInfo info;
		private Researchable researchable;
		private DeveloperMode developerMode;
		private PlayerResources playerResources;
		private TechLevel techLevel;

		private int totalCost;
		private int totalTime;
		private int remainingCost;
		private int remainingTime;

		private Actor currentTarget;

		public bool IsResearching { get { return currentTarget != null; } }

		public Researches(ActorInitializer init, ResearchesInfo info) : base(info)
		{
			this.info = info;
			researchable = init.Self.Trait<Researchable>();
			developerMode = init.Self.Owner.PlayerActor.Trait<DeveloperMode>();
			playerResources = init.Self.Owner.PlayerActor.Trait<PlayerResources>();
			techLevel = init.World.WorldActor.Trait<TechLevel>();
		}

		public IEnumerable<IOrderTargeter> Orders { get { yield return new ResearchOrderTargeter(info.Cursor, info.BlockedCursor); } }

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			return order.OrderID == ResearchOrderTargeter.Id ? new Order(order.OrderID, self, target, queued) : null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != ResearchOrderTargeter.Id)
				return;

			if (order.Target.Type != TargetType.Actor || order.Target.Actor == null)
				return;

			if (!IsValidTarget(self, order.Target.Actor))
				return;

			var researchable = order.Target.Actor.Trait<Researchable>();

			if (researchable.Researches != null)
				researchable.Researches.StopResearch();
			else
				StartResearch(self, order.Target.Actor);
		}

		private void StartResearch(Actor self, Actor target)
		{
			if (!IsValidTarget(self, target))
				return;

			var researchable = target.Trait<Researchable>();

			if (researchable.Researches != null)
				return;

			remainingCost = totalCost = researchable.Info.ResearchCost[researchable.Level] * info.ResearchRates[this.researchable.Level] / 100;

			if (developerMode.FastBuild)
				remainingTime = totalTime = 1;
			else
				remainingTime = totalTime = researchable.Info.ResearchTime[researchable.Level] * info.ResearchRates[this.researchable.Level] / 100;

			currentTarget = target;
			researchable.Researches = this;
			researchable.SetProgress(0);

			Game.Sound.PlayNotification(target.World.Map.Rules, target.Owner, "Speech", info.StartNotification, target.Owner.Faction.InternalName);
		}

		private void StopResearch()
		{
			if (currentTarget == null)
				return;

			if (!currentTarget.IsDead && currentTarget.IsInWorld)
				currentTarget.Trait<Researchable>().Researches = null;

			currentTarget = null;
		}

		void ITick.Tick(Actor self)
		{
			if (currentTarget == null)
				return;

			if (!IsValidTarget(self, currentTarget))
			{
				StopResearch();
				return;
			}

			var researchable = currentTarget.Trait<Researchable>();

			var expectedRemainingCost = remainingTime == 1 ? 0 : totalCost * remainingTime / Math.Max(1, totalTime);
			var costThisFrame = remainingCost - expectedRemainingCost;

			if (costThisFrame != 0 && !playerResources.TakeCash(costThisFrame, true))
				return;

			remainingCost -= costThisFrame;
			remainingTime -= 1;

			researchable.SetProgress(Math.Min(info.ResearchSteps * (totalTime - remainingTime) / Math.Max(1, totalTime), info.ResearchSteps - 1));

			if (remainingTime == 0)
			{
				StopResearch();
				researchable.Level++;
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.FinishNotification, self.Owner.Faction.InternalName);
			}
		}

		public bool IsValidTarget(Actor self, Actor target)
		{
			if (target == null || target.IsDead || !target.IsInWorld)
				return false;

			if (target.Owner != self.Owner)
				return false;

			if (IsTraitDisabled)
				return false;

			var researchable = target.TraitOrDefault<Researchable>();

			if (researchable == null || researchable.IsTraitDisabled)
				return false;

			if (researchable.Level >= researchable.Info.MaxLevel)
				return false;

			if (researchable.Level >= techLevel.TechLevels)
				return false;

			if (currentTarget != null && researchable.Researches == null)
				return false;

			return true;
		}

		public void Killed(Actor self, AttackInfo attackInfo)
		{
			StopResearch();
		}
	}
}

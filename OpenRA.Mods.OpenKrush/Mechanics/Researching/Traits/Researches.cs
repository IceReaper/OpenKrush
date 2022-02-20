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
	using Common.Traits;
	using JetBrains.Annotations;
	using LobbyOptions;
	using OpenRA.Traits;
	using Orders;

	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[Desc("Research mechanism, attach to the actor which can research.")]
	public class ResearchesInfo : ConditionalTraitInfo, Requires<ResearchableInfo>
	{
		public const string Prefix = "RESEARCH::";

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

	public class Researches : ConditionalTrait<ResearchesInfo>, IIssueOrder, IResolveOrder, INotifyKilled, ITick, IProvidesResearchables
	{
		private readonly ResearchesInfo info;

		private readonly Researchable researchable;
		private readonly DeveloperMode developerMode;
		private readonly PlayerResources playerResources;
		private readonly int timeFactor;

		private Actor? researchingActor;

		private int totalCost;
		private int totalTime;
		private int remainingCost;
		private int remainingTime;

		public Researches(ActorInitializer init, ResearchesInfo info)
			: base(info)
		{
			this.info = info;
			this.researchable = init.Self.TraitOrDefault<Researchable>();
			this.developerMode = init.Self.Owner.PlayerActor.TraitOrDefault<DeveloperMode>();
			this.playerResources = init.Self.Owner.PlayerActor.TraitOrDefault<PlayerResources>();
			this.timeFactor = init.Self.World.WorldActor.TraitOrDefault<ResearchDuration>().Duration;
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new ResearchOrderTargeter(this.info.Cursor, this.info.BlockedCursor);
			}
		}

		Order? IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
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

			switch (action)
			{
				case ResearchAction.Start:
					this.StartResearch(self, order.Target.Actor);

					break;

				case ResearchAction.Stop:
					var targetResearchable = order.Target.Actor.TraitOrDefault<Researchable>();

					if (targetResearchable.ResearchedBy != null)
						targetResearchable.ResearchedByResearches?.StopResearch(targetResearchable.ResearchedBy, true);

					break;

				case ResearchAction.None:
					break;

				default:
					throw new ArgumentOutOfRangeException(Enum.GetName(action));
			}
		}

		void INotifyKilled.Killed(Actor self, AttackInfo attackInfo)
		{
			this.StopResearch(self, true);
		}

		void ITick.Tick(Actor self)
		{
			if (this.researchingActor == null)
				return;

			var action = ResearchUtils.GetAction(self, this.researchingActor);

			if (action == ResearchAction.None)
			{
				this.StopResearch(self, true);

				return;
			}

			var expectedRemainingCost = this.remainingTime == 1 ? 0 : this.totalCost * this.remainingTime / Math.Max(1, this.totalTime);
			var costThisFrame = this.remainingCost - expectedRemainingCost;

			if (costThisFrame != 0 && !this.playerResources.TakeCash(costThisFrame, true))
				return;

			this.remainingCost -= costThisFrame;
			this.remainingTime -= 1;

			var targetActor = this.researchingActor;
			var targetResearchable = targetActor.TraitOrDefault<Researchable>();

			if (this.remainingTime == 0)
			{
				this.StopResearch(self, false);
				targetResearchable.Researched(targetActor);
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", this.info.CompleteNotification, self.Owner.Faction.InternalName);
			}
			else
				targetResearchable.SetProgress((this.totalTime - this.remainingTime) / (float)this.totalTime);
		}

		private void StartResearch(Actor self, Actor target)
		{
			if (this.researchingActor != null && this.researchingActor.Equals(target))
				return;

			var targetResearchable = target.TraitOrDefault<Researchable>();

			if (targetResearchable.ResearchedBy != null)
				return;

			if (this.researchingActor != null)
				this.StopResearch(self, true);

			this.researchingActor = target;

			this.remainingCost = this.totalCost = targetResearchable.Info.ResearchCostBase
				+ targetResearchable.Info.ResearchCostTechLevel * targetResearchable.NextTechLevel() * this.info.ResearchRates[this.researchable.Level] / 100;

			if (this.developerMode.FastBuild)
				this.remainingTime = this.totalTime = 1;
			else
			{
				this.remainingTime = this.totalTime = targetResearchable.Info.ResearchTimeBase
					+ targetResearchable.Info.ResearchTimeTechLevel
					* targetResearchable.NextTechLevel()
					* this.info.ResearchRates[this.researchable.Level]
					/ 100
					* this.timeFactor;
			}

			targetResearchable.ResearchedBy = self;
			targetResearchable.ResearchedByResearches = this;
			targetResearchable.SetProgress(0);

			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", this.info.StartNotification, self.Owner.Faction.InternalName);
		}

		private void StopResearch(Actor self, bool isCanceled)
		{
			if (this.researchingActor == null)
				return;

			if (!this.researchingActor.Disposed)
				this.researchingActor.TraitOrDefault<Researchable>().ResearchedBy = null;

			this.researchingActor = null;

			if (isCanceled)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", this.info.CancelNotification, self.Owner.Faction.InternalName);
		}

		public ResarchState GetState()
		{
			if (this.IsTraitDisabled)
				return ResarchState.Unavailable;

			return this.researchingActor != null ? ResarchState.Researching : ResarchState.Available;
		}

		public Dictionary<string, int> GetResearchables(Actor self)
		{
			var technologies = new Dictionary<string, int>();

			for (var i = 0; i < this.info.ResearchRates.Length; i++)
				technologies.Add(ResearchesInfo.Prefix + i, i);

			return technologies;
		}
	}
}

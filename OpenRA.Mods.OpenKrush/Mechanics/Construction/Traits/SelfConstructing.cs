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
	using Common.Traits.Render;
	using JetBrains.Annotations;
	using OpenRA.Traits;
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public enum SpawnType
	{
		PlaceBuilding,
		Deploy,
		Other
	}

	[UsedImplicitly]
	public class SelfConstructingInfo : WithMakeAnimationInfo, Requires<IHealthInfo>
	{
		public override object Create(ActorInitializer init)
		{
			return new SelfConstructing(init, this);
		}
	}

	public class SelfConstructing : WithMakeAnimation, ITick, INotifyRemovedFromWorld, INotifyCreated, INotifyDamageStateChanged, INotifyKilled
	{
		public readonly SelfConstructingInfo Info;
		public readonly int Steps;

		private readonly WithSpriteBody wsb;

		private int token = Actor.InvalidConditionToken;

		private ProductionItem? productionItem;

		private List<int> healthSteps = new();
		private Health? health;
		private int step;
		private readonly SpawnType spawnType;

		public bool IsConstructing => this.token != Actor.InvalidConditionToken;

		public SelfConstructing(ActorInitializer init, SelfConstructingInfo info)
			: base(init, info)
		{
			this.Info = info;
			this.wsb = init.Self.TraitOrDefault<WithSpriteBody>();

			if (!string.IsNullOrEmpty(this.Info.Condition) && this.token == Actor.InvalidConditionToken)
				this.token = init.Self.GrantCondition(this.Info.Condition);

			this.spawnType = init.Contains<PlaceBuildingInit>(null) ? SpawnType.PlaceBuilding :
				init.Contains<SpawnedByMapInit>() ? SpawnType.Other : SpawnType.Deploy;

			for (this.Steps = 0;; this.Steps++)
			{
				if (!this.wsb.DefaultAnimation.HasSequence(this.Info.Sequence[..^1] + this.Steps))
					break;
			}
		}

		void INotifyCreated.Created(Actor self)
		{
			switch (this.spawnType)
			{
				case SpawnType.PlaceBuilding:
				{
					var productionQueue = self.Owner.PlayerActor.TraitsImplementing<SelfConstructingProductionQueue>()
						.FirstOrDefault(q => q.AllItems().Contains(self.Info));

					if (productionQueue == null)
						return;

					var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();

					this.productionItem = new SelfConstructingProductionItem(productionQueue, self, valued?.Cost ?? 0, null, null);
					productionQueue.BeginProduction(this.productionItem, false);

					this.health = self.TraitOrDefault<Health>();

					this.healthSteps = new();

					for (var i = 0; i <= this.Steps; i++)
						this.healthSteps.Add(this.health.MaxHP * (i + 1) / (this.Steps + 1));

					self.World.AddFrameEndTask(_ => this.health.InflictDamage(self, self, new(this.health.MaxHP - this.healthSteps[0]), true));

					this.wsb.CancelCustomAnimation(self);
					this.wsb.PlayCustomAnimationRepeating(self, this.Info.Sequence[..^1] + 0);

					break;
				}

				case SpawnType.Deploy:
					this.wsb.CancelCustomAnimation(self);
					this.wsb.PlayCustomAnimation(self, "deploy", () => this.OnComplete(self));

					break;

				case SpawnType.Other:
					this.OnComplete(self);

					break;

				default:
					throw new ArgumentOutOfRangeException(Enum.GetName(this.spawnType));
			}
		}

		private void OnComplete(Actor self)
		{
			if (this.token != Actor.InvalidConditionToken)
				this.token = self.RevokeCondition(this.token);
		}

		void ITick.Tick(Actor self)
		{
			if (this.productionItem == null)
				return;

			if (this.productionItem.Done)
			{
				this.productionItem.Queue.EndProduction(this.productionItem);
				this.productionItem = null;
				this.wsb.CancelCustomAnimation(self);

				for (; this.step < this.Steps; this.step++)
					this.health?.InflictDamage(self, self, new(this.healthSteps[this.step] - this.healthSteps[this.step + 1]), true);

				this.OnComplete(self);

				return;
			}

			var progress = Math.Max(
				0,
				Math.Min(
					this.Steps * (this.productionItem.TotalTime - this.productionItem.RemainingTime) / Math.Max(1, this.productionItem.TotalTime),
					this.Steps - 1
				)
			);

			if (progress == this.step)
				return;

			for (; this.step < progress; this.step++)
				this.health?.InflictDamage(self, self, new(this.healthSteps[this.step] - this.healthSteps[this.step + 1]), true);

			this.wsb.PlayCustomAnimationRepeating(self, this.Info.Sequence[..^1] + this.step);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			this.productionItem?.Queue.EndProduction(this.productionItem);
		}

		public ProductionItem? TryAbort(Actor self)
		{
			if (this.productionItem == null)
				return null;

			var item = this.productionItem;

			this.productionItem.Queue.EndProduction(this.productionItem);
			this.productionItem = null;
			this.OnComplete(self);

			return item;
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			if (this.productionItem == null)
				return;

			this.wsb.PlayCustomAnimationRepeating(self, this.Info.Sequence[..^1] + this.step);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			this.productionItem?.Queue.EndProduction(this.productionItem);
		}
	}
}

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
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Kknd.Traits.Production;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Mechanics.Construction.Traits
{
	public enum SpawnType { PlaceBuilding, Deploy, Other }

	public class SelfConstructingInfo : WithMakeAnimationInfo, Requires<IHealthInfo>
	{
		public override object Create(ActorInitializer init) { return new SelfConstructing(init, this); }
	}

	public class SelfConstructing : WithMakeAnimation, ITick, INotifyRemovedFromWorld, INotifyCreated, INotifyDamageStateChanged, INotifyKilled
	{
		public readonly SelfConstructingInfo Info;
		public readonly int Steps = 0;

		private readonly WithSpriteBody wsb;

		private int token = Actor.InvalidConditionToken;

		private ProductionItem productionItem;

		private List<int> healthSteps;
		private Health health;
		private int step;
		private SpawnType spawnType;

		public SelfConstructing(ActorInitializer init, SelfConstructingInfo info)
			: base(init, info)
		{
			Info = info;
			wsb = init.Self.Trait<WithSpriteBody>();

			if (!string.IsNullOrEmpty(Info.Condition) && token == Actor.InvalidConditionToken)
				token = init.Self.GrantCondition(Info.Condition);

			spawnType = init.Contains<PlaceBuildingInit>(null) ? SpawnType.PlaceBuilding : init.Contains<SpawnedByMapInit>() ? SpawnType.Other : SpawnType.Deploy;

			for (Steps = 0; ; Steps++)
			{
				if (!wsb.DefaultAnimation.HasSequence(Info.Sequence.Substring(0, Info.Sequence.Length - 1) + Steps))
					break;
			}
		}

		void INotifyCreated.Created(Actor self)
		{
			if (spawnType == SpawnType.PlaceBuilding)
			{
				var productionQueue = self.Owner.PlayerActor.TraitsImplementing<SelfConstructingProductionQueue>().First(q => q.AllItems().Contains(self.Info));
				var valued = self.Info.TraitInfoOrDefault<ValuedInfo>();
				productionItem = new SelfConstructingProductionItem(productionQueue, self, valued == null ? 0 : valued.Cost, null, null);
				productionQueue.BeginProduction(productionItem, false);

				health = self.Trait<Health>();

				healthSteps = new List<int>();
				for (var i = 0; i <= Steps; i++)
					healthSteps.Add(health.MaxHP * (i + 1) / (Steps + 1));

				self.World.AddFrameEndTask(world => health.InflictDamage(self, self, new Damage(health.MaxHP - healthSteps[0]), true));

				wsb.CancelCustomAnimation(self);
				wsb.PlayCustomAnimationRepeating(self, Info.Sequence.Substring(0, Info.Sequence.Length - 1) + 0);
			}
			else if (spawnType == SpawnType.Deploy)
			{
				wsb.CancelCustomAnimation(self);
				wsb.PlayCustomAnimation(self, "deploy", () => OnComplete(self));
			}
			else
				OnComplete(self);
		}

		private void OnComplete(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		void ITick.Tick(Actor self)
		{
			if (productionItem == null)
				return;

			if (productionItem.Done)
			{
				productionItem.Queue.EndProduction(productionItem);
				productionItem = null;
				wsb.CancelCustomAnimation(self);

				while (step < Steps)
					health.InflictDamage(self, self, new Damage(healthSteps[step] - healthSteps[++step]), true);

				OnComplete(self);
				return;
			}

			var progress = Math.Max(0, Math.Min(Steps * (productionItem.TotalTime - productionItem.RemainingTime) / Math.Max(1, productionItem.TotalTime), Steps - 1));

			if (progress != step)
			{
				while (step < progress)
					health.InflictDamage(self, self, new Damage(healthSteps[step] - healthSteps[++step]), true);

				wsb.PlayCustomAnimationRepeating(self, Info.Sequence.Substring(0, Info.Sequence.Length - 1) + step);
			}
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (productionItem != null)
				productionItem.Queue.EndProduction(productionItem);
		}

		public ProductionItem TryAbort(Actor self)
		{
			if (productionItem == null)
				return null;

			var item = productionItem;

			productionItem.Queue.EndProduction(productionItem);
			productionItem = null;
			OnComplete(self);

			return item;
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			if (productionItem == null)
				return;

			wsb.PlayCustomAnimationRepeating(self, Info.Sequence.Substring(0, Info.Sequence.Length - 1) + step);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (productionItem != null)
				productionItem.Queue.EndProduction(productionItem);
		}
	}
}

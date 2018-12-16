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

using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Docking;
using OpenRA.Mods.Kknd.Traits.Research;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Traits.Repairbay
{
	// TODO implement cost per repair
	[Desc("KKnD specific repair bay implementation.")]
	class RepairbayInfo : DockActionInfo, Requires<ResearchableInfo>
	{
		[Desc("How many HP per tick should be repaired per tech level.")]
		public readonly int[] Rates = {10, 20, 30, 40};

		[Desc("Delay between repair ticks.")]
		public readonly int Delay = 3;

		[GrantedConditionReference]
		[Desc("Condition to grant while repairing.")]
		public readonly string Condition = null;

		[Desc("Repair using these damagetypes.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		public override object Create(ActorInitializer init) { return new Repairbay(init, this); }
	}

	class Repairbay : DockAction, ITick
	{
		private readonly RepairbayInfo info;
		private readonly Actor self;
		private readonly Researchable researchable;
		private ConditionManager conditionManager;

		private int lastRepairTick;
		private int token = ConditionManager.InvalidConditionToken;

		public Repairbay(ActorInitializer init, RepairbayInfo info) : base(info)
		{
			this.info = info;
			self = init.Self;
			researchable = init.Self.Trait<Researchable>();
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			conditionManager = self.Trait<ConditionManager>();
		}

		public override bool CanDock(Actor target, Dockable dockable)
		{
			if (dockable == null && !target.Info.HasTraitInfo<RepairableVehicleInfo>())
				return false;

			if (dockable != null && !(dockable is RepairableVehicle))
				return false;

			return target.Owner == self.Owner && target.Trait<Health>().DamageState != DamageState.Undamaged;
		}

		public override bool Process(Actor target)
		{
			if (!string.IsNullOrEmpty(info.Condition) && token == ConditionManager.InvalidConditionToken)
				token = conditionManager.GrantCondition(self, info.Condition);

			lastRepairTick = target.World.WorldTick;
			var health = target.Trait<Health>();

			if (target.World.WorldTick % info.Delay == 0)
				health.InflictDamage(self, self, new Damage(-info.Rates[researchable.Level], info.DamageTypes), true);

			return health.HP == health.MaxHP;
		}

		void ITick.Tick(Actor self)
		{
			if (token != ConditionManager.InvalidConditionToken && self.World.WorldTick - 1 > lastRepairTick)
				token = conditionManager.RevokeCondition(self, token);
		}
	}
}

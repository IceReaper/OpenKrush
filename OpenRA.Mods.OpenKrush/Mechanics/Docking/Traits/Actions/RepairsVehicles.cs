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

namespace OpenRA.Mods.OpenKrush.Mechanics.Docking.Traits.Actions
{
	using Common.Traits;
	using Dockables;
	using OpenRA.Traits;
	using Primitives;
	using Researching.Traits;

	// TODO implement cost per repair
	[Desc("RepairBay implementation.")]
	public class RepairsVehiclesInfo : DockActionInfo, Requires<ResearchableInfo>
	{
		[Desc("How many HP per tick should be repaired per tech level.")]
		public readonly int[] Rates = { 10, 20, 30, 40 };

		[Desc("Delay between repair ticks.")]
		public readonly int Delay = 3;

		[GrantedConditionReference]
		[Desc("Condition to grant while repairing.")]
		public readonly string Condition = null;

		[Desc("Repair using these damagetypes.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		public override object Create(ActorInitializer init)
		{
			return new RepairsVehicles(init, this);
		}
	}

	public class RepairsVehicles : DockAction, ITick
	{
		private readonly RepairsVehiclesInfo info;
		private readonly Actor self;
		private readonly Researchable researchable;

		private int lastRepairTick;
		private int token = Actor.InvalidConditionToken;

		public RepairsVehicles(ActorInitializer init, RepairsVehiclesInfo info)
			: base(info)
		{
			this.info = info;
			self = init.Self;
			researchable = init.Self.Trait<Researchable>();
		}

		public override bool CanDock(Actor target)
		{
			if (!target.Info.HasTraitInfo<RepairableVehicleInfo>())
				return false;

			// Only yourself, so you cannot block friendly repairbays when being broke.
			if (target.Owner != self.Owner)
				return false;

			return target.Trait<Health>().DamageState != DamageState.Undamaged;
		}

		public override bool Process(Actor target)
		{
			if (!target.Info.HasTraitInfo<RepairableVehicleInfo>())
				return true;

			if (!string.IsNullOrEmpty(info.Condition) && token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);

			lastRepairTick = target.World.WorldTick;
			var health = target.Trait<Health>();

			if (target.World.WorldTick % info.Delay == 0)
				health.InflictDamage(self, self, new Damage(-info.Rates[researchable.Level], info.DamageTypes), true);

			return health.HP == health.MaxHP;
		}

		void ITick.Tick(Actor self)
		{
			if (token != Actor.InvalidConditionToken && self.World.WorldTick - 1 > lastRepairTick)
				token = self.RevokeCondition(token);
		}
	}
}

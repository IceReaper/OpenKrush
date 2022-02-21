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

namespace OpenRA.Mods.OpenKrush.Mechanics.Repairbays.Traits;

using Common.Traits;
using Docking.Traits;
using JetBrains.Annotations;
using OpenRA.Traits;
using Primitives;
using Researching;
using Researching.Traits;

// TODO implement cost per repair
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("RepairBay implementation.")]
public class RepairsVehiclesInfo : DockActionInfo, Requires<ResearchableInfo>
{
	public const string Prefix = "REPAIRSVEHICLES::";

	[Desc("How many HP per tick should be repaired per tech level.")]
	public readonly int[] Rates = { 10, 20, 30, 40 };

	[Desc("Delay between repair ticks.")]
	public readonly int Delay = 3;

	[GrantedConditionReference]
	[Desc("Condition to grant while repairing.")]
	public readonly string? Condition;

	[Desc("Repair using these damagetypes.")]
	public readonly BitSet<DamageType> DamageTypes;

	public override object Create(ActorInitializer init)
	{
		return new RepairsVehicles(init, this);
	}
}

public class RepairsVehicles : DockAction, ITick, IProvidesResearchables
{
	private readonly RepairsVehiclesInfo info;
	private readonly Researchable researchable;

	private int lastRepairTick;
	private int token = Actor.InvalidConditionToken;

	public RepairsVehicles(ActorInitializer init, RepairsVehiclesInfo info)
		: base(info)
	{
		this.info = info;
		this.researchable = init.Self.TraitOrDefault<Researchable>();
	}

	public override bool CanDock(Actor self, Actor target)
	{
		if (!target.Info.HasTraitInfo<RepairableVehicleInfo>())
			return false;

		// Only yourself, so you cannot block friendly repairbays when being broke.
		if (target.Owner != self.Owner)
			return false;

		return target.TraitOrDefault<Health>().DamageState != DamageState.Undamaged;
	}

	public override bool Process(Actor self, Actor target)
	{
		if (!target.Info.HasTraitInfo<RepairableVehicleInfo>())
			return true;

		if (!string.IsNullOrEmpty(this.info.Condition) && this.token == Actor.InvalidConditionToken)
			this.token = self.GrantCondition(this.info.Condition);

		this.lastRepairTick = target.World.WorldTick;
		var health = target.TraitOrDefault<Health>();

		if (target.World.WorldTick % this.info.Delay == 0)
			health.InflictDamage(self, self, new(-this.info.Rates[this.researchable.Level], this.info.DamageTypes), true);

		return health.HP == health.MaxHP;
	}

	void ITick.Tick(Actor self)
	{
		if (this.token != Actor.InvalidConditionToken && self.World.WorldTick - 1 > this.lastRepairTick)
			this.token = self.RevokeCondition(this.token);
	}

	public Dictionary<string, int> GetResearchables(Actor self)
	{
		var technologies = new Dictionary<string, int>();

		for (var i = 0; i < this.info.Rates.Length; i++)
			technologies.Add(RepairsVehiclesInfo.Prefix + i, i);

		return technologies;
	}
}

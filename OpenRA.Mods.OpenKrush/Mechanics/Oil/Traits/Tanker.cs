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

namespace OpenRA.Mods.OpenKrush.Mechanics.Oil.Traits;

using Activities;
using Docking.Activities;
using Docking.Traits;
using JetBrains.Annotations;
using OpenRA.Activities;
using OpenRA.Traits;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
[Desc("Tanker implementation.")]
public class TankerInfo : DockableInfo
{
	[Desc("Maximum oil a tanker can hold.")]
	public readonly int Capacity = 500;

	public override object Create(ActorInitializer init)
	{
		return new Tanker(this);
	}
}

public class Tanker : Dockable, IHaveOil, INotifyCreated, ITick
{
	private readonly TankerInfo info;

	public Actor? PreferedDrillrig;
	public Actor? PreferedPowerStation;

	public Tanker(TankerInfo info)
		: base(info)
	{
		this.info = info;
	}

	void INotifyCreated.Created(Actor self)
	{
		self.World.AddFrameEndTask(_ => self.QueueActivity(new TankerCycle(self, this)));
	}

	public int Current { get; private set; }
	public int Maximum => this.info.Capacity;

	public int Pull(int amount)
	{
		var pullAmount = Math.Min(amount, this.Current);
		this.Current -= pullAmount;

		return pullAmount;
	}

	public int Push(int amount)
	{
		var pushAmount = Math.Min(amount, this.Maximum - this.Current);
		this.Current += pushAmount;

		return amount - pushAmount;
	}

	protected override Activity GetDockingActivity(Actor self, Actor target, Dock dock)
	{
		if (target.TraitOrDefault<Drillrig>() != null)
		{
			this.PreferedDrillrig = target;
			var tankerCycleDrillrig = new TankerCycle(self, this);
			tankerCycleDrillrig.QueueChild(new Docking(self, this.PreferedDrillrig, this.PreferedDrillrig.TraitOrDefault<Dock>()));

			return tankerCycleDrillrig;
		}

		if (target.TraitOrDefault<PowerStation>() == null)
			return base.GetDockingActivity(self, target, dock);

		this.PreferedPowerStation = target;
		var tankerCyclePowerStation = new TankerCycle(self, this);
		tankerCyclePowerStation.QueueChild(new Docking(self, this.PreferedPowerStation, this.PreferedPowerStation.TraitOrDefault<Dock>()));

		return tankerCyclePowerStation;
	}

	void ITick.Tick(Actor self)
	{
		if (this.PreferedDrillrig != null && !OilUtils.IsUsable(this.PreferedDrillrig, this.PreferedDrillrig.TraitOrDefault<Drillrig>()))
		{
			// When releasing the drillrig, we should also release the powerstation as another one might be the better pick.
			this.PreferedDrillrig = null;
			this.PreferedPowerStation = null;
		}

		if (this.PreferedPowerStation != null && this.PreferedPowerStation.Owner.RelationshipWith(self.Owner) != PlayerRelationship.Ally)
			this.PreferedPowerStation = null;

		if (this.PreferedPowerStation != null && !OilUtils.IsUsable(this.PreferedPowerStation, this.PreferedPowerStation.TraitOrDefault<PowerStation>()))
			this.PreferedPowerStation = null;
	}
}

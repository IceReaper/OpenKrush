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

namespace OpenRA.Mods.OpenKrush.Mechanics.Oil.Activities;

using Docking;
using Docking.Activities;
using Docking.Traits;
using OpenRA.Activities;
using Traits;

public class TankerCycle : Activity, IDockingActivity
{
	private readonly Actor actor;
	private readonly Tanker tanker;

	public Dock? Dock => (this.ChildActivity as Docking)?.Dock;
	public Actor? DockActor => (this.ChildActivity as Docking)?.DockActor;
	public DockingState DockingState => (this.ChildActivity as Docking)?.DockingState ?? DockingState.None;

	public void StartDocking()
	{
		(this.ChildActivity as Docking)?.StartDocking();
	}

	public void StartUndocking()
	{
		(this.ChildActivity as Docking)?.StartUndocking();
	}

	public TankerCycle(Actor actor, Tanker tanker)
	{
		this.actor = actor;
		this.tanker = tanker;
	}

	public override bool Tick(Actor self)
	{
		if (this.IsCanceling)
			return true;

		if (this.ChildActivity != null)
			return false;

		if (this.tanker.Current < this.tanker.Maximum)
		{
			this.tanker.PreferedDrillrig ??= OilUtils.GetMostUnderutilizedDrillrig(self.Owner, self.CenterPosition);

			if (this.tanker.PreferedDrillrig != null)
			{
				this.QueueChild(new Docking(this.actor, this.tanker.PreferedDrillrig, this.tanker.PreferedDrillrig.TraitOrDefault<Dock>()));

				return false;
			}
		}

		if (this.tanker.Current <= 0)
			return false;

		if (this.tanker.PreferedPowerStation == null && this.tanker.PreferedDrillrig != null)
			this.tanker.PreferedPowerStation = OilUtils.GetNearestPowerStation(self.Owner, this.tanker.PreferedDrillrig.CenterPosition);

		var target = this.tanker.PreferedPowerStation ?? OilUtils.GetNearestPowerStation(self.Owner, self.CenterPosition);

		if (target == null)
			return false;

		this.QueueChild(new Docking(this.actor, target, target.TraitOrDefault<Dock>()));

		return false;
	}
}

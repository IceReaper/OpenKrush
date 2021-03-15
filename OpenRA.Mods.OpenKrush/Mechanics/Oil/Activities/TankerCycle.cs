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

using OpenRA.Activities;
using OpenRA.Mods.OpenKrush.Mechanics.Docking;
using OpenRA.Mods.OpenKrush.Mechanics.Docking.Activities;
using OpenRA.Mods.OpenKrush.Mechanics.Docking.Traits;
using OpenRA.Mods.OpenKrush.Mechanics.Oil.Traits;

namespace OpenRA.Mods.OpenKrush.Mechanics.Oil.Activities
{
	public class TankerCycle : Activity, IDockingActivity
	{
		private readonly Actor actor;
		private readonly Tanker tanker;

		public Dock Dock => (ChildActivity as Docking.Activities.Docking)?.Dock;
		public Actor DockActor => (ChildActivity as Docking.Activities.Docking)?.DockActor;
		public DockingState DockingState => (ChildActivity as Docking.Activities.Docking)?.DockingState ?? DockingState.None;
		public void StartDocking() => (ChildActivity as Docking.Activities.Docking)?.StartDocking();
		public void StartUndocking() => (ChildActivity as Docking.Activities.Docking)?.StartUndocking();

		public TankerCycle(Actor actor, Tanker tanker)
		{
			this.actor = actor;
			this.tanker = tanker;
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			if (ChildActivity != null)
				return false;

			if (tanker.Current < tanker.Maximum)
			{
				if (tanker.PreferedDrillrig == null)
					tanker.PreferedDrillrig = OilUtils.GetMostUnderutilizedDrillrig(self.Owner, self.CenterPosition);

				if (tanker.PreferedDrillrig != null)
				{
					QueueChild(new Docking.Activities.Docking(actor, tanker.PreferedDrillrig, tanker.PreferedDrillrig.Trait<Dock>()));
					return false;
				}
			}

			if (tanker.Current > 0)
			{
				if (tanker.PreferedPowerStation == null && tanker.PreferedDrillrig != null)
					tanker.PreferedPowerStation = OilUtils.GetNearestPowerStation(self.Owner, tanker.PreferedDrillrig.CenterPosition);

				var target = tanker.PreferedPowerStation ?? OilUtils.GetNearestPowerStation(self.Owner, self.CenterPosition);

				if (target != null)
				{
					QueueChild(new Docking.Activities.Docking(actor, target, target.Trait<Dock>()));
					return false;
				}
			}

			return false;
		}
	}
}

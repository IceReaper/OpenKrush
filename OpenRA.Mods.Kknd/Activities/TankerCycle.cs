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

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities.Docking;
using OpenRA.Mods.Common.Traits.Docking;
using OpenRA.Mods.Kknd.Traits.Resources;

namespace OpenRA.Mods.Kknd.Activities
{
	class TankerCycle : Docking
	{
		private bool abortByCancel;

		[ObjectCreator.UseCtor]
		public TankerCycle(Actor dockableActor, Dockable dockable, Actor dockActor, Dock dock) : base(dockableActor, dockable, dockActor, dock)
		{
			var tanker = (Tanker)dockable;

			if (dockActor == null)
			{
				DockingState = DockingState.Undocked;
				return;
			}

			if (dockActor.Info.HasTraitInfo<DrillrigInfo>())
				tanker.Drillrig = dockActor;
			else
				tanker.PowerStation = dockActor;
		}

		public override Activity Tick(Actor self)
		{
			var result = base.Tick(self);

			if (!abortByCancel && shouldCancel)
				shouldCancel = false;

			if (result == this || shouldCancel)
				return result;

			var tanker = (Tanker)Dockable;

			if (tanker.Current == tanker.Maximum)
			{
				if (!tanker.IsValidPowerStation(tanker.PowerStation))
					tanker.AssignNearestPowerStation();

				if (tanker.PowerStation == null)
					return this;

				DockActor = tanker.PowerStation;
				Dock = DockActor.Trait<Dock>();
				DockingState = DockingState.Approaching;
			}
			else
			{
				if (!tanker.IsValidDrillrig(tanker.Drillrig))
					tanker.AssignNearestDrillrig();

				if (tanker.Drillrig == null)
					return this;

				DockActor = tanker.Drillrig;
				Dock = DockActor.Trait<Dock>();
				DockingState = DockingState.Approaching;
			}

			return this;

		}

		public override bool Cancel(Actor self, bool keepQueue = false)
		{
			abortByCancel = true;
			return base.Cancel(self, keepQueue);
		}
	}
}

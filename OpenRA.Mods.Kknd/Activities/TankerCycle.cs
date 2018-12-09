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

			if (!abortByCancel && ShouldCancel)
				ShouldCancel = false;

			if (result == this || ShouldCancel)
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

#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Traits.Docking;

namespace OpenRA.Mods.Kknd.Activities
{
	public class BackwardsDockingSequence : DockingSequence
	{
		private Actor dockableActor;
		private Dock dock;

		private WPos dockTarget;
		private CPos dockEntry;

		private int speed;
		private int distance;

		private bool isDocking;
		private bool shouldCancel;

		private void Setup(Actor dockableActor, Dockable dockable, Actor dockActor, Dock dock)
		{
			this.dockableActor = dockableActor;
			this.dock = dock;

			dockTarget = dockActor.CenterPosition + dock.Info.Position;
			dockEntry = dockableActor.World.Map.CellContaining(dockTarget + new WVec(0, -1024, 0).Rotate(WRot.FromFacing(dock.Info.Facing)));

			speed = dockableActor.Trait<Mobile>().Info.Speed;
			distance = (dockTarget - dockableActor.World.Map.CenterOfCell(dockEntry)).Length;
		}

		public override void Dock(Actor dockableActor, Dockable dockable, Actor dockActor, Dock dock)
		{
			isDocking = true;
			Setup(dockableActor, dockable, dockActor, dock);
			QueueChild(new Move(dockableActor, dockEntry, WDist.Zero));
		}

		public override void Undock(Actor dockableActor, Dockable dockable, Actor dockActor, Dock dock)
		{
			Setup(dockableActor, dockable, dockActor, dock);
			QueueChild(new Drag(dockableActor, dockTarget, dockableActor.World.Map.CenterOfCell(dockEntry), distance / speed));
		}

		public override bool Tick(Actor self)
		{
			var lastActivity = ChildActivity;
			var isComplete = ChildActivity.Tick(self);

			if (!isComplete)
				return false;

			if (shouldCancel)
				return true;

			if (lastActivity is Move)
			{
				if ((dockableActor.World.Map.CellContaining(dockableActor.CenterPosition) - dockEntry).Length != 0)
					QueueChild(new Move(dockableActor, dockEntry, WDist.Zero));
				else
					QueueChild(new Turn(dockableActor, dock.Info.Facing));
			}

			if (lastActivity is Turn)
				QueueChild(new Drag(dockableActor, dockableActor.World.Map.CenterOfCell(dockEntry), dockTarget, distance / speed));

			return false;
		}

		public new bool Cancel(Actor self, bool keepQueue = false)
		{
			if (!shouldCancel)
			{
				shouldCancel = true;

				if (ChildActivity is Move || ChildActivity is Turn)
					ChildActivity.Cancel(self);
				else if (ChildActivity is Drag && isDocking)
					QueueChild(new Drag(dockableActor, dockTarget, dockableActor.World.Map.CenterOfCell(dockEntry), distance / speed));
			}

			return false;
		}
	}
}

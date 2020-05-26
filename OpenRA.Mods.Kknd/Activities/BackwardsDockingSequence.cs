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
	internal enum SequenceState
	{
		Move,
		Turn,
		Drag,
		Done
	}

	public class BackwardsDockingSequence : DockingSequence
	{
		private Actor dockableActor;
		private Dock dock;

		private WPos dockTarget;
		private CPos dockEntry;

		private int speed;
		private int distance;

		private bool isDocking;
		private SequenceState state = SequenceState.Done;
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
			state = SequenceState.Move;
		}

		public override void Undock(Actor dockableActor, Dockable dockable, Actor dockActor, Dock dock)
		{
			Setup(dockableActor, dockable, dockActor, dock);
			QueueChild(new Drag(dockableActor, dockTarget, dockableActor.World.Map.CenterOfCell(dockEntry), distance / speed));
			state = SequenceState.Drag;
		}

		public override bool Tick(Actor self)
		{
			if (state == SequenceState.Move)
			{
				if (shouldCancel)
					return true;

				if ((dockableActor.World.Map.CellContaining(dockableActor.CenterPosition) - dockEntry).Length != 0)
					QueueChild(new Move(dockableActor, dockEntry, WDist.Zero));
				else
				{
					QueueChild(new Turn(dockableActor, dock.Info.Facing));
					state = SequenceState.Turn;
				}

				return false;
			}

			if (state == SequenceState.Turn)
			{
				if (shouldCancel)
					return true;

				QueueChild(new Drag(dockableActor, dockableActor.World.Map.CenterOfCell(dockEntry), dockTarget, distance / speed));
				state = SequenceState.Drag;

				return false;
			}

			if (state == SequenceState.Drag)
			{
				state = SequenceState.Done;
				return false;
			}

			return true;
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			if (!shouldCancel)
			{
				shouldCancel = true;

				if (ChildActivity is Move || ChildActivity is Turn)
					ChildActivity.Cancel(self);
				else if (ChildActivity is Drag && isDocking)
					QueueChild(new Drag(dockableActor, dockTarget, dockableActor.World.Map.CenterOfCell(dockEntry), distance / speed));
			}
		}
	}
}

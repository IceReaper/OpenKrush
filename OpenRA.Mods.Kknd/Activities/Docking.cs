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

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Kknd.Traits.Docking;
using OpenRA.Traits;

namespace OpenRA.Mods.Kknd.Activities
{
	public enum DockingState { Undocked, Approaching, Waiting, Docking, Docked, Undocking }

	public class Docking : Activity
	{
		public Actor DockableActor { get; protected set; }
		public Dockable Dockable { get; protected set; }

		public Actor DockActor { get; protected set; }
		public Dock Dock { get; protected set; }

		public DockingState DockingState { get; protected set; }
		protected bool shouldCancel;

		[ObjectCreator.UseCtor]
		public Docking(Actor dockableActor, Dockable dockable, Actor dockActor, Dock dock)
		{
			DockableActor = dockableActor;
			Dockable = dockable;
			DockActor = dockActor;
			Dock = dock;

			DockingState = DockingState.Approaching;
		}

		public override bool Tick(Actor self)
		{
			if (!shouldCancel && (DockActor == null || DockActor.IsDead || !DockActor.IsInWorld || Dock.GetDockAction(DockableActor) == null))
				shouldCancel = true;

			var isChildActivityRunning = ChildActivity != null;

			if (isChildActivityRunning)
			{
				if (shouldCancel)
					ChildActivity.Cancel(self);
				else
					isChildActivityRunning = !ChildActivity.Tick(self);
			}

			switch (DockingState)
			{
				case DockingState.Approaching:
					if (shouldCancel)
					{
						DockingState = DockingState.Undocked;
						break;
					}

					// TODO does not null when target reached...?
					if (isChildActivityRunning)
						break;

					var distance = (DockableActor.CenterPosition - DockActor.CenterPosition).Length;

					if (distance > WDist.FromCells(Dock.Info.QueueDistance).Length)
						QueueChild(new Move(DockableActor, Target.FromActor(DockActor), WDist.FromCells(Dock.Info.QueueDistance)));
					else
					{
						DockingState = DockingState.Waiting;
						Dock.Add(DockableActor);
					}

					break;

				case DockingState.Waiting:
					if (shouldCancel)
					{
						DockingState = DockingState.Undocked;
						Dock.Remove(DockableActor);
					}

					break;

				case DockingState.Docking:
					if (!isChildActivityRunning)
					{
						if (shouldCancel)
						{
							DockingState = DockingState.Undocked;
							Dock.Remove(DockableActor);
						}
						else
						{
							DockingState = DockingState.Docked;
							Dock.OnDock();
						}
					}

					break;

				case DockingState.Docked:
					if (shouldCancel)
						StartUndocking();

					break;

				case DockingState.Undocking:
					if (!isChildActivityRunning)
					{
						DockingState = DockingState.Undocked;
						Dock.Remove(DockableActor);

						if (!DockActor.IsDead && DockActor.IsInWorld)
						{
							var rallyPoint = DockActor.TraitOrDefault<RallyPoint>();
							if (rallyPoint != null)
								DockableActor.QueueActivity(new Move(DockableActor, rallyPoint.Path.First()));
						}
					}

					break;

				case DockingState.Undocked:
					break;
			}

			return DockingState == DockingState.Undocked && !isChildActivityRunning;
		}

		public void StartDocking()
		{
			DockingState = DockingState.Docking;
			var dockingSequence = Game.ModData.ObjectCreator.CreateObject<DockingSequence>(Dockable.Info.DockingSequenceActivity + "DockingSequence");
			dockingSequence.Dock(DockableActor, Dockable, DockActor, Dock);
			QueueChild(dockingSequence);
		}

		public void StartUndocking()
		{
			DockingState = DockingState.Undocking;
			var dockingSequence = Game.ModData.ObjectCreator.CreateObject<DockingSequence>(Dockable.Info.DockingSequenceActivity + "DockingSequence");
			dockingSequence.Undock(DockableActor, Dockable, DockActor, Dock);
			QueueChild(dockingSequence);
		}

		public new bool Cancel(Actor self, bool keepQueue = false)
		{
			shouldCancel = true;
			return false;
		}
	}
}

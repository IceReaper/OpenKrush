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

namespace OpenRA.Mods.OpenKrush.Mechanics.Docking.Activities
{
	using System.Linq;
	using Common.Activities;
	using Common.Traits;
	using OpenRA.Activities;
	using OpenRA.Traits;
	using Traits;

	public class Docking : Activity, IDockingActivity
	{
		private readonly Actor dockableActor;

		private WPos initialPosition;

		public Actor DockActor { get; }
		public Dock Dock { get; }
		public DockingState DockingState { get; private set; }

		public Docking(Actor dockableActor, Actor dockActor, Dock dock)
		{
			this.dockableActor = dockableActor;
			DockActor = dockActor;
			Dock = dock;

			DockingState = DockingState.Approaching;
		}

		public override bool Tick(Actor self)
		{
			if ((DockActor.IsDead || !DockActor.IsInWorld || Dock.IsTraitDisabled) && !IsCanceling)
				Cancel(self, true);

			switch (DockingState)
			{
				case DockingState.Approaching:
					if (State == ActivityState.Canceling)
						return true;

					if (ChildActivity != null)
						break;

					var distance = WDist.FromCells(Dock.Info.QueueDistance);

					if ((dockableActor.CenterPosition - DockActor.CenterPosition).Length > distance.Length)
						QueueChild(new Move(dockableActor, Target.FromActor(DockActor), distance));
					else
					{
						DockingState = DockingState.Waiting;
						Dock.Add(dockableActor);
					}

					break;

				case DockingState.Waiting:
					if (State == ActivityState.Canceling)
					{
						Dock.Remove(dockableActor);

						return true;
					}

					break;

				case DockingState.PrepareDocking:
					if (State == ActivityState.Canceling)
					{
						Dock.Remove(dockableActor);

						return true;
					}

					if (ChildActivity != null)
						break;

					var target = DockActor.World.Map.CellContaining(DockActor.CenterPosition + Dock.Info.Position + Dock.Info.DragOffset);

					if (dockableActor.Location != target)
						QueueChild(new Move(dockableActor, target));
					else
					{
						DockingState = DockingState.Docking;

						QueueChild(new Turn(dockableActor, Dock.Info.Angle));
						initialPosition = dockableActor.CenterPosition;
						QueueChild(new Drag(dockableActor, dockableActor.CenterPosition, DockActor.CenterPosition + Dock.Info.Position, Dock.Info.DragLength));
					}

					break;

				case DockingState.Docking:
					if (State == ActivityState.Canceling)
					{
						StartUndocking();

						return false;
					}

					if (ChildActivity == null)
					{
						DockingState = DockingState.Docked;
						Dock.OnDock();
					}

					break;

				case DockingState.Docked:
					if (State == ActivityState.Canceling)
					{
						StartUndocking();

						return false;
					}

					break;

				case DockingState.Undocking:
					if (ChildActivity == null)
					{
						DockingState = DockingState.None;
						Dock.Remove(dockableActor);

						if (!DockActor.IsDead && DockActor.IsInWorld)
						{
							var rallyPoint = DockActor.TraitOrDefault<RallyPoint>();

							if (rallyPoint != null && rallyPoint.Path.Any())
								foreach (var cell in rallyPoint.Path)
									dockableActor.QueueActivity(new Move(dockableActor, cell));
						}
					}

					break;

				case DockingState.None:
					return true;
			}

			return false;
		}

		public void StartDocking()
		{
			DockingState = DockingState.PrepareDocking;
		}

		public void StartUndocking()
		{
			DockingState = DockingState.Undocking;
			QueueChild(new Drag(dockableActor, DockActor.CenterPosition + Dock.Info.Position, initialPosition, Dock.Info.DragLength));
		}
	}
}

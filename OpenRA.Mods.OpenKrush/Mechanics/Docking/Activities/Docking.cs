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
	using Common.Activities;
	using Common.Traits;
	using OpenRA.Activities;
	using OpenRA.Traits;
	using System;
	using System.Linq;
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
			this.DockActor = dockActor;
			this.Dock = dock;

			this.DockingState = DockingState.Approaching;
		}

		public override bool Tick(Actor self)
		{
			if ((this.DockActor.IsDead || !this.DockActor.IsInWorld || this.Dock.IsTraitDisabled) && !this.IsCanceling)
				this.Cancel(self, true);

			switch (this.DockingState)
			{
				case DockingState.Approaching:
					if (this.State == ActivityState.Canceling)
						return true;

					if (this.ChildActivity != null)
						break;

					var distance = WDist.FromCells(this.Dock.Info.QueueDistance);

					if ((this.dockableActor.CenterPosition - this.DockActor.CenterPosition).Length > distance.Length)
						this.QueueChild(new Move(this.dockableActor, this.DockActor.Location, distance));
					else
					{
						this.DockingState = DockingState.Waiting;
						this.Dock.Add(this.dockableActor);
					}

					break;

				case DockingState.Waiting:
					if (this.State == ActivityState.Canceling)
					{
						this.Dock.Remove(this.dockableActor);

						return true;
					}

					break;

				case DockingState.PrepareDocking:
					if (this.State == ActivityState.Canceling)
					{
						this.Dock.Remove(this.dockableActor);

						return true;
					}

					if (this.ChildActivity != null)
						break;

					var target = this.DockActor.World.Map.CellContaining(this.DockActor.CenterPosition + this.Dock.Info.Position + this.Dock.Info.DragOffset);

					if (this.dockableActor.Location != target)
						this.QueueChild(new Move(this.dockableActor, target));
					else
					{
						this.DockingState = DockingState.Docking;

						this.QueueChild(new Turn(this.dockableActor, this.Dock.Info.Angle));
						this.initialPosition = this.dockableActor.CenterPosition;

						this.QueueChild(
							new Drag(
								this.dockableActor,
								this.dockableActor.CenterPosition,
								this.DockActor.CenterPosition + this.Dock.Info.Position,
								this.Dock.Info.DragLength
							)
						);
					}

					break;

				case DockingState.Docking:
					if (this.State == ActivityState.Canceling)
					{
						this.StartUndocking();

						return false;
					}

					if (this.ChildActivity == null)
					{
						this.DockingState = DockingState.Docked;
						this.Dock.OnDock(this.DockActor);
					}

					break;

				case DockingState.Docked:
					if (this.State == ActivityState.Canceling)
					{
						this.StartUndocking();

						return false;
					}

					break;

				case DockingState.Undocking:
					if (this.ChildActivity == null)
					{
						this.DockingState = DockingState.None;
						this.Dock.OnUndock();
						this.Dock.Remove(this.dockableActor);

						if (!this.DockActor.IsDead && this.DockActor.IsInWorld)
						{
							var rallyPoint = this.DockActor.TraitOrDefault<RallyPoint>();

							if (rallyPoint != null && rallyPoint.Path.Any())
							{
								foreach (var cell in rallyPoint.Path)
									this.dockableActor.QueueActivity(new Move(this.dockableActor, cell));
							}
						}
					}

					break;

				case DockingState.None:
					return true;

				default:
					throw new ArgumentOutOfRangeException(Enum.GetName(this.DockingState));
			}

			return false;
		}

		public void StartDocking()
		{
			this.DockingState = DockingState.PrepareDocking;
		}

		public void StartUndocking()
		{
			this.DockingState = DockingState.Undocking;

			this.QueueChild(
				new Drag(this.dockableActor, this.DockActor.CenterPosition + this.Dock.Info.Position, this.initialPosition, this.Dock.Info.DragLength)
			);
		}
	}
}
